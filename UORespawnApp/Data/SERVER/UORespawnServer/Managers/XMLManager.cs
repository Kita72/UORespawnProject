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

                        default:
                            failed++;
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[Unknown command: {command}]");
                            break;
                    }
                }

                // Delete command file after processing to prevent re-execution
                File.Delete(CommandFilePath);

                UOR_Utility.SendMsg(ConsoleColor.Green, $"XMLCMD-[Processed: {deleted} deleted, {added} added, {failed} failed]");
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

                    UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[DELETE: Removed XmlSpawner {serialValue}]");
                    return true;
                }

                // Handle standard Spawner (has RemoveSpawned method)
                if (item is Spawner spawner)
                {
                    spawner.RemoveSpawned();
                    spawner.Delete();

                    UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[DELETE: Removed Spawner {serialValue}]");
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
        /// Format: ADD|MapId|X|Y|Z|HomeRange|Creature1:Count1|Creature2:Count2|...
        /// </summary>
        private static bool ProcessAddCommand(string[] parts)
        {
            // Minimum: ADD|MapId|X|Y|Z|HomeRange|Creature:Count = 7 parts
            if (parts.Length < 7)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[ADD: Insufficient parameters]");
                return false;
            }

            // Parse location parameters
            if (!int.TryParse(parts[1], out int mapId) ||
                !int.TryParse(parts[2], out int x) ||
                !int.TryParse(parts[3], out int y) ||
                !int.TryParse(parts[4], out int z) ||
                !int.TryParse(parts[5], out int homeRange))
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[ADD: Invalid location/range parameters]");
                return false;
            }

            // Validate map
            Map map = Map.Maps[mapId];
            if (map == null || map == Map.Internal)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[ADD: Invalid map ID: {mapId}]");
                return false;
            }

            // Parse creature list (parts 6 onwards)
            var creatures = new List<(string typeName, int count)>();
            int totalCreatureCount = 0;

            for (int i = 6; i < parts.Length; i++)
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
                totalCreatureCount += count;
            }

            if (creatures.Count == 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[ADD: No valid creatures specified]");
                return false;
            }

            try
            {
                // Calculate MaxCount: base count at HomeRange 10, +10% per additional 10 range
                double rangeMultiplier = 1.0 + ((homeRange - 10) / 10.0) * 0.1;
                rangeMultiplier = Math.Max(1.0, rangeMultiplier); // Minimum multiplier of 1.0
                int maxCount = (int)Math.Ceiling(totalCreatureCount * rangeMultiplier);

                // Create XmlSpawner
                XmlSpawner spawner = new XmlSpawner
                {
                    Name = "XmlSpawner",
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

                UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[ADD: Created spawner at {location} on {map.Name} with {creatures.Count} creature types, MaxCount={maxCount}]");
                return true;
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[ADD: Error creating spawner: {ex.Message}]");
                return false;
            }
        }
    }
}
