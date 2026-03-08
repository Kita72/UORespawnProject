using System;
using System.IO;
using System.Collections.Generic;

using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Managers
{
    /// <summary>
    /// Processes XML spawner commands from the UORespawn editor.
    /// Commands are written to a text file that the server processes on startup.
    /// Supports DELETE and ADD operations for XmlSpawner management.
    /// </summary>
    internal static class XMLManager
    {
        private static readonly string CommandFilePath = Path.Combine(UOR_DIR.INPUT_DIR, "UOR_XmlCommands.txt");

        // Command types
        private const string CMD_DELETE = "DELETE";
        private const string CMD_ADD = "ADD";
        private const string CMD_EDIT = "EDIT";

        /// <summary>
        /// Processes all pending XML spawner commands from the command file.
        /// Called during server startup after world load.
        /// </summary>
        internal static void ProcessCommands()
        {
            if (!File.Exists(CommandFilePath))
            {
                return;
            }

            UOR_Utility.SendMsg(ConsoleColor.Cyan, "XMLCMD-[Processing commands...]");

            int deleted = 0;
            int added = 0;
            int edited = 0;
            int failed = 0;

            try
            {
                string[] lines = File.ReadAllLines(CommandFilePath);

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split('|');

                    if (parts.Length < 2)
                    {
                        failed++;
                        UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[Invalid command format: {line}]");
                        continue;
                    }

                    string command = parts[0].ToUpper();

                    switch (command)
                    {
                        case CMD_DELETE:
                            if (ProcessDeleteCommand(parts))
                                deleted++;
                            else
                                failed++;
                            break;

                        case CMD_ADD:
                            if (ProcessAddCommand(parts))
                                added++;
                            else
                                failed++;
                            break;

                        case CMD_EDIT:
                            if (ProcessEditCommand(parts))
                                edited++;
                            else
                                failed++;
                            break;

                        default:
                            failed++;
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[Unknown command: {command}]");
                            break;
                    }
                }

                // Delete command file after processing to prevent re-execution
                File.Delete(CommandFilePath);

                UOR_Utility.SendMsg(ConsoleColor.Green, $"XMLCMD-[Processed: {deleted} deleted, {added} added, {edited} edited, {failed} failed]");
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[Error: {ex.Message}]");
            }
        }

        /// <summary>
        /// Processes a DELETE command.
        /// Format: DELETE|Serial
        /// </summary>
        private static bool ProcessDeleteCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[DELETE: Missing serial]");
                return false;
            }

            if (!int.TryParse(parts[1], out int serialValue))
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[DELETE: Invalid serial format: {parts[1]}]");
                return false;
            }

            Item item = World.FindItem((Serial)serialValue);

            if (item == null)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[DELETE: Spawner not found: {serialValue}]");
                return false;
            }

            try
            {
                // Handle XmlSpawner (has RemoveSpawnObjects method)
                if (item is XmlSpawner xmlSpawner)
                {
                    xmlSpawner.RemoveSpawnObjects();
                    xmlSpawner.Delete();

                    UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[DELETE: XmlSpawner({serialValue})]");
                    return true;
                }

                // Handle standard Spawner (has RemoveSpawned method)
                if (item is Spawner spawner)
                {
                    spawner.RemoveSpawned();
                    spawner.Delete();

                    UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[DELETE: Spawner({serialValue})]");
                    return true;
                }

                // Item exists but is not a spawner
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[DELETE: Item is not a spawner: {serialValue}]");
                return false;
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[DELETE: Error deleting {serialValue}: {ex.Message}]");
                return false;
            }
        }

        /// <summary>
        /// Processes an ADD command.
        /// Format: ADD|MapId|X|Y|Z|HomeRange|MaxCount|Creature1:Count1|Creature2:Count2|...
        /// </summary>
        private static bool ProcessAddCommand(string[] parts)
        {
            // Minimum: ADD|MapId|X|Y|Z|HomeRange|MaxCount|Creature:Count = 8 parts
            if (parts.Length < 8)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[ADD: Insufficient parameters]");
                return false;
            }

            // Parse location parameters
            if (!int.TryParse(parts[1], out int mapId) ||
                !int.TryParse(parts[2], out int x) ||
                !int.TryParse(parts[3], out int y) ||
                !int.TryParse(parts[4], out int z) ||
                !int.TryParse(parts[5], out int homeRange) ||
                !int.TryParse(parts[6], out int maxCount))
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[ADD: Invalid location/range/maxcount parameters]");
                return false;
            }

            // Validate map
            Map map = Map.Maps[mapId];
            if (map == null || map == Map.Internal)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[ADD: Invalid map ID: {mapId}]");
                return false;
            }

            // Parse creature list (parts 7 onwards)
            var creatures = new List<(string typeName, int count)>();

            for (int i = 7; i < parts.Length; i++)
            {
                string[] creatureParts = parts[i].Split(':');

                if (creatureParts.Length != 2)
                {
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[ADD: Invalid creature format: {parts[i]}]");
                    continue;
                }

                string typeName = creatureParts[0];

                if (!int.TryParse(creatureParts[1], out int count) || count <= 0)
                {
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[ADD: Invalid count for {typeName}]");
                    continue;
                }

                creatures.Add((typeName, count));
            }

            if (creatures.Count == 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[ADD: No valid creatures specified]");
                return false;
            }

            try
            {
                // Create XmlSpawner
                XmlSpawner spawner = new XmlSpawner
                {
                    Name = "XmlRespawner",
                    HomeRange = homeRange
                };

                // Create SpawnObjects array
                var spawnObjects = new List<XmlSpawner.SpawnObject>();

                foreach (var (typeName, count) in creatures)
                {
                    var spawnObj = new XmlSpawner.SpawnObject(typeName, count);
                    spawnObjects.Add(spawnObj);
                }

                spawner.SpawnObjects = spawnObjects.ToArray();

                // Place spawner in world
                Point3D location = new Point3D(x, y, z);
                spawner.MoveToWorld(location, map);

                // Start spawning
                spawner.DoRespawn = true;

                UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[ADD: {map.Name}|{location}|Spawn({creatures.Count})|Max({maxCount})|Range({homeRange})");
                return true;
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[ADD: Error creating spawner: {ex.Message}]");
                return false;
            }
        }

        /// <summary>
        /// Processes an EDIT command.
        /// Format: EDIT|Serial|HomeRange|MaxCount|Creature1:Count1|Creature2:Count2|...
        /// Preserves XmlSpawner special syntax (e.g., /Cantwalk/true) for existing creatures.
        /// </summary>
        private static bool ProcessEditCommand(string[] parts)
        {
            // Minimum: EDIT|Serial|HomeRange|MaxCount|Creature:Count = 5 parts
            if (parts.Length < 5)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[EDIT: Insufficient parameters]");
                return false;
            }

            // Parse serial
            if (!int.TryParse(parts[1], out int serialValue))
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[EDIT: Invalid serial format: {parts[1]}]");
                return false;
            }

            // Parse HomeRange and MaxCount
            if (!int.TryParse(parts[2], out int homeRange) ||
                !int.TryParse(parts[3], out int maxCount))
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[EDIT: Invalid HomeRange/MaxCount parameters]");
                return false;
            }

            // Find the spawner
            Item item = World.FindItem((Serial)serialValue);

            if (item == null)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[EDIT: Spawner not found: {serialValue}]");
                return false;
            }

            // Parse creature list (parts 4 onwards)
            var creatures = new List<(string typeName, int count)>();

            for (int i = 4; i < parts.Length; i++)
            {
                string[] creatureParts = parts[i].Split(':');

                if (creatureParts.Length != 2)
                {
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[EDIT: Invalid creature format: {parts[i]}]");
                    continue;
                }

                string typeName = creatureParts[0];

                if (!int.TryParse(creatureParts[1], out int count) || count <= 0)
                {
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[EDIT: Invalid count for {typeName}]");
                    continue;
                }

                creatures.Add((typeName, count));
            }

            if (creatures.Count == 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[EDIT: No valid creatures specified]");
                return false;
            }

            try
            {
                // Handle XmlSpawner
                if (item is XmlSpawner xmlSpawner)
                {
                    return EditXmlSpawner(xmlSpawner, homeRange, maxCount, creatures);
                }

                // Handle standard Spawner
                if (item is Spawner spawner)
                {
                    return EditStandardSpawner(spawner, homeRange, maxCount, creatures);
                }

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[EDIT: Item is not a spawner: {serialValue}]");
                return false;
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[EDIT: Error editing spawner: {ex.Message}]");
                return false;
            }
        }

        /// <summary>
        /// Edits an XmlSpawner, preserving special syntax for existing creatures.
        /// </summary>
        private static bool EditXmlSpawner(XmlSpawner spawner, int homeRange, int maxCount, List<(string typeName, int count)> creatures)
        {
            // Build lookup of existing spawn objects: clean name -> original full TypeName
            // This preserves special XmlSpawner syntax like /Cantwalk/true or ,param
            var existingByCleanName = new Dictionary<string, XmlSpawner.SpawnObject>(StringComparer.OrdinalIgnoreCase);

            if (spawner.SpawnObjects != null)
            {
                foreach (var spawnObj in spawner.SpawnObjects)
                {
                    if (!string.IsNullOrEmpty(spawnObj.TypeName))
                    {
                        string cleanName = ExtractCleanTypeName(spawnObj.TypeName);

                        // Only store first occurrence (in case of duplicates)
                        if (!existingByCleanName.ContainsKey(cleanName))
                        {
                            existingByCleanName[cleanName] = spawnObj;
                        }
                    }
                }
            }

            // Remove existing spawned creatures
            spawner.RemoveSpawnObjects();

            // Build new spawn objects list
            var newSpawnObjects = new List<XmlSpawner.SpawnObject>();

            foreach (var (typeName, count) in creatures)
            {
                // Check if this creature existed with special syntax
                if (existingByCleanName.TryGetValue(typeName, out var existingObj) && 
                    HasSpecialSyntax(existingObj.TypeName))
                {
                    // Preserve original TypeName with special syntax, but update count
                    var spawnObj = new XmlSpawner.SpawnObject(existingObj.TypeName, count);
                    newSpawnObjects.Add(spawnObj);
                }
                else
                {
                    // Use the clean name from edit command
                    var spawnObj = new XmlSpawner.SpawnObject(typeName, count);
                    newSpawnObjects.Add(spawnObj);
                }
            }

            // Apply changes
            spawner.SpawnObjects = newSpawnObjects.ToArray();
            spawner.HomeRange = homeRange;
            spawner.MaxCount = maxCount;

            // Trigger respawn
            spawner.DoRespawn = true;

            UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[EDIT: XmlSpawner({spawner.Serial.Value})|Spawn({creatures.Count})|Max({maxCount})|Range({homeRange})]");
            return true;
        }

        /// <summary>
        /// Edits a standard Spawner.
        /// </summary>
        private static bool EditStandardSpawner(Spawner spawner, int homeRange, int maxCount, List<(string typeName, int count)> creatures)
        {
            // Remove existing spawned creatures
            spawner.RemoveSpawned();

            // Clear existing spawn entries
            spawner.SpawnObjects.Clear();

            // Add new spawn entries
            foreach (var (typeName, count) in creatures)
            {
                var spawnObj = new SpawnObject(typeName, count);
                spawner.SpawnObjects.Add(spawnObj);
            }

            // Apply settings
            spawner.HomeRange = homeRange;
            spawner.MaxCount = maxCount;

            // Trigger respawn
            spawner.Respawn();

            UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[EDIT: Spawner({spawner.Serial.Value})|Spawn({creatures.Count})|Max({maxCount})|Range({homeRange})]");
            return true;
        }

        /// <summary>
        /// Checks if a TypeName contains XmlSpawner special syntax (commas or slashes).
        /// </summary>
        private static bool HasSpecialSyntax(string typeName)
        {
            return typeName.Contains(",") || typeName.Contains("/");
        }

        /// <summary>
        /// Extracts the clean creature type name from XmlSpawner syntax.
        /// Handles formats like "CreatureName,param" or "CreatureName/prop/value".
        /// </summary>
        private static string ExtractCleanTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return typeName;

            string name = typeName.Trim();

            // Handle comma-separated parameters (e.g., "tribewarrior,Kurak")
            int commaIndex = name.IndexOf(',');
            if (commaIndex > 0)
            {
                name = name.Substring(0, commaIndex).Trim();
            }

            // Handle slash-separated properties (e.g., "MyrmidexQueen/Cantwalk/true")
            int slashIndex = name.IndexOf('/');
            if (slashIndex > 0)
            {
                name = name.Substring(0, slashIndex).Trim();
            }

            return name;
        }
    }
}
