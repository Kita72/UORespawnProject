using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Server;
using Server.Mobiles;
using Server.Engines.XmlSpawner2;

namespace Server.Custom.UORespawnServer.Core
{
    /// <summary>
    /// Processes XML spawner commands from the editor.
    /// Commands are written to INPUT/UOR_XmlCommands.txt
    /// Format: DELETE|Serial or ADD|MapId|X|Y|Z|HomeRange|Creature1:Count1|Creature2:Count2|...
    /// </summary>
    internal static class XmlCommandProcessor
    {
        /// <summary>
        /// Checks for and processes any pending XML spawner commands.
        /// Called during server startup after data initialization.
        /// </summary>
        internal static void ProcessCommands()
        {
            if (!File.Exists(UOR_DIR.XML_COMMANDS_FILE))
            {
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(UOR_DIR.XML_COMMANDS_FILE);
                int deleted = 0;
                int added = 0;
                int failed = 0;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split('|');
                    if (parts.Length < 2)
                    {
                        failed++;
                        continue;
                    }

                    string action = parts[0].ToUpper();

                    switch (action)
                    {
                        case "DELETE":
                            if (ProcessDeleteCommand(parts))
                                deleted++;
                            else
                                failed++;
                            break;

                        case "ADD":
                            if (ProcessAddCommand(parts))
                                added++;
                            else
                                failed++;
                            break;

                        default:
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XML-CMD-[Unknown action: {action}]");
                            failed++;
                            break;
                    }
                }

                // Delete the command file after processing
                File.Delete(UOR_DIR.XML_COMMANDS_FILE);

                if (deleted > 0 || added > 0 || failed > 0)
                {
                    UOR_Utility.SendMsg(ConsoleColor.Green, $"XML-CMD-[Deleted: {deleted}, Added: {added}, Failed: {failed}]");
                }
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XML-CMD ERROR-[{ex.Message}]");
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
                UOR_Utility.SendMsg(ConsoleColor.Yellow, "XML-DELETE-[Missing serial]");
                return false;
            }

            string serialStr = parts[1];

            // Parse serial (plain int from Serial.Value)
            if (!int.TryParse(serialStr, out int serialInt))
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XML-DELETE-[Invalid serial: {serialStr}]");
                return false;
            }

            Serial serial = (Serial)serialInt;

            // Find the spawner
            Item item = World.FindItem(serial);
            if (item == null)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XML-DELETE-[Spawner not found: {serialStr}]");
                return false;
            }

            if (!(item is ISpawner))
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XML-DELETE-[Item is not a spawner: {serialStr}]");
                return false;
            }

            // Delete all spawned creatures first
            if (item is ISpawner spawner)
            {
                spawner.RemoveSpawned();
            }

            // Delete the spawner
            item.Delete();

            UOR_Utility.SendMsg(ConsoleColor.Green, $"XML-DELETE-[Deleted spawner: {serialStr}]");
            return true;
        }

        /// <summary>
        /// Processes an ADD command.
        /// Format: ADD|MapId|X|Y|Z|HomeRange|Creature1:Count1|Creature2:Count2|...
        /// </summary>
        private static bool ProcessAddCommand(string[] parts)
        {
            // Minimum: ADD|MapId|X|Y|Z|HomeRange|Creature:Count
            if (parts.Length < 7)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, "XML-ADD-[Insufficient parameters]");
                return false;
            }

            // Parse location parameters
            if (!int.TryParse(parts[1], out int mapId) ||
                !int.TryParse(parts[2], out int x) ||
                !int.TryParse(parts[3], out int y) ||
                !int.TryParse(parts[4], out int z) ||
                !int.TryParse(parts[5], out int homeRange))
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, "XML-ADD-[Invalid location parameters]");
                return false;
            }

            // Validate map
            Map map = Map.Maps[mapId];
            if (map == null || map == Map.Internal)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XML-ADD-[Invalid map: {mapId}]");
                return false;
            }

            // Parse creature list (starting at index 6)
            var creatures = new List<(string Name, int Count)>();
            for (int i = 6; i < parts.Length; i++)
            {
                string creaturePart = parts[i];
                if (string.IsNullOrWhiteSpace(creaturePart))
                    continue;

                // Format: CreatureName:Count
                string[] creatureSplit = creaturePart.Split(':');
                string creatureName = creatureSplit[0];
                int count = 1;

                if (creatureSplit.Length > 1 && int.TryParse(creatureSplit[1], out int parsedCount))
                {
                    count = parsedCount;
                }

                creatures.Add((creatureName, count));
            }

            if (creatures.Count == 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, "XML-ADD-[No creatures specified]");
                return false;
            }

            // Calculate total spawn count and max count based on home range
            int totalCreatures = creatures.Sum(c => c.Count);
            int maxCount = CalculateMaxSpawnFromHomeRange(homeRange);

            // Adjust Z to land height
            z = map.GetAverageZ(x, y);

            // Create XmlSpawner
            try
            {
                XmlSpawner spawner = new XmlSpawner();
                spawner.Name = "UOR_XmlSpawner";
                spawner.HomeRange = homeRange;
                spawner.SpawnRange = homeRange;
                spawner.MaxCount = maxCount;
                spawner.MinDelay = TimeSpan.FromMinutes(1);
                spawner.MaxDelay = TimeSpan.FromMinutes(5);

                // Build spawn objects
                var spawnObjects = new List<XmlSpawner.SpawnObject>();
                foreach (var (name, count) in creatures)
                {
                    var spawnObj = new XmlSpawner.SpawnObject(null, 1);
                    spawnObj.TypeName = name;
                    spawnObj.MaxCount = count;
                    spawnObjects.Add(spawnObj);
                }
                spawner.SpawnObjects = spawnObjects.ToArray();

                // Place the spawner
                spawner.MoveToWorld(new Point3D(x, y, z), map);
                spawner.Running = true;
                spawner.Respawn();

                UOR_Utility.SendMsg(ConsoleColor.Green, $"XML-ADD-[Created spawner at Map{mapId} ({x},{y},{z}) with {creatures.Count} creature types]");
                return true;
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"XML-ADD ERROR-[{ex.Message}]");
                return false;
            }
        }

        /// <summary>
        /// Calculates a reasonable max spawn count based on home range.
        /// Larger areas get more spawns.
        /// </summary>
        private static int CalculateMaxSpawnFromHomeRange(int homeRange)
        {
            if (homeRange <= 0) return 1;

            // Area-based scaling with diminishing returns
            double area = Math.PI * homeRange * homeRange;
            int baseCount = (int)Math.Ceiling(Math.Sqrt(area) / 8.0);

            return Math.Max(1, Math.Min(baseCount, 50)); // Cap at 50
        }
    }
}
