using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Engines.Doom;
using Server.Custom.UORespawnServer.Mobiles;
using Server.Custom.UORespawnServer.Helpers;
using Server.Custom.UORespawnServer.Enums;

namespace Server.Custom.UORespawnServer.Managers
{
    // Manages our Game Data : Lists of Required Data for Editor!
    internal static class GameManager
    {
        internal static void InitializeData()
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, "DATA-[Initializing...");

            GenMapList();
            GenBestiaryList();
            GenRegionList();
            GenTileList();
            GenSpawnerList();
            GenVendorData();

            UOR_Utility.SendMsg(ConsoleColor.Green, "DATA-[Initialized]");
        }

        /// <summary>
        /// Checks for and applies any pending edit commands.
        /// Called after normal spawn data load to apply server-side edits that haven't been consumed by editor.
        /// </summary>
        internal static void CheckAndApplyPendingCommands()
        {
            if (!CommandManager.HasAnyPendingCommands())
            {
                UOR_Utility.SendMsg(ConsoleColor.Green, "COMMANDS-[No pending commands]");
                return;
            }

            UOR_Utility.SendMsg(ConsoleColor.Cyan, "COMMANDS-[Checking pending commands...]");

            int totalApplied = 0;
            int totalFailed = 0;

            // Process settings commands
            if (CommandManager.HasPendingCommands(CommandTarget.Settings))
            {
                var result = ApplySettingsCommands();
                totalApplied += result.applied;
                totalFailed += result.failed;
            }

            // Process spawn commands (Box, Region, Tile, Vendor)
            CommandTarget[] spawnTargets = { CommandTarget.Box, CommandTarget.Region, CommandTarget.Tile, CommandTarget.Vendor };

            foreach (var target in spawnTargets)
            {
                if (CommandManager.HasPendingCommands(target))
                {
                    var result = ApplySpawnCommands(target);
                    totalApplied += result.applied;
                    totalFailed += result.failed;
                }
            }

            if (totalApplied > 0 || totalFailed > 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Green, $"COMMANDS-[Applied: {totalApplied}, Failed: {totalFailed}]");
            }
        }

        /// <summary>
        /// Applies pending settings commands.
        /// </summary>
        private static (int applied, int failed) ApplySettingsCommands()
        {
            int applied = 0;
            int failed = 0;

            var commands = CommandManager.ProcessAndConsumeCommands(CommandTarget.Settings);

            foreach (var command in commands)
            {
                if (!CommandManager.ValidateCommand(command))
                {
                    failed++;
                    continue;
                }

                // Settings commands use SpawnName as key, ExtraData as value
                if (UOR_Settings.ApplySettingCommand(command.SpawnName, command.ExtraData))
                {
                    applied++;
                    UOR_Utility.SendMsg(ConsoleColor.Cyan, $"SETTING APPLIED-[{command.SpawnName}={command.ExtraData}]");
                }
                else
                {
                    failed++;
                }
            }

            return (applied, failed);
        }

        /// <summary>
        /// Applies pending spawn commands for a specific target type.
        /// </summary>
        private static (int applied, int failed) ApplySpawnCommands(CommandTarget target)
        {
            int applied = 0;
            int failed = 0;

            var commands = CommandManager.ProcessAndConsumeCommands(target);

            foreach (var command in commands)
            {
                if (!CommandManager.ValidateCommand(command))
                {
                    failed++;
                    continue;
                }

                if (SpawnManager.ApplySpawnCommand(command))
                {
                    applied++;
                    UOR_Utility.SendMsg(ConsoleColor.Cyan, $"SPAWN APPLIED-[{command.Action} {command.SpawnName} to {target}/{command.Section}]");
                }
                else
                {
                    failed++;
                }
            }

            return (applied, failed);
        }

        private static void GenMapList()
        {
            List<string> mapList = new List<string>();

            for (int i = 0; i < Map.Maps.Length; i++)
            {
                if (Map.Maps[i] != null && Map.Maps[i] != Map.Internal)
                {
                    if (!mapList.Contains($"{(Map.Maps[i].MapID, Map.Maps[i].Name)}"))
                    {
                        mapList.Add($"{(Map.Maps[i].MapID, Map.Maps[i].Name)}");
                    }
                }
            }

            if (mapList.Count > 0)
            {
                File.WriteAllLines(UOR_DIR.MAP_LIST_FILE, mapList);

                UOR_Utility.SendMsg(ConsoleColor.Green, "MAPS-[Generated]");
            }
            else
            {
                UOR_Utility.SendMsg(ConsoleColor.Green, "MAPS-[Empty]");
            }
        }

        private static void GenBestiaryList()
        {
            try
            {
                var allTypes = Assembly.GetExecutingAssembly().GetTypes();

                var types = allTypes.Where(t => IsValidSpawn(t)).Select(t => t.Name).ToList();

                types.Sort();

                if (types.Count > 0)
                {
                    File.WriteAllLines(UOR_DIR.BESTIARY_LIST_FILE, types);
                }

                UOR_Utility.SendMsg(ConsoleColor.Green, $"BESTIARY-[Generated]");
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"BESTIARY: {ex.Message}");

                return;
            }
        }

        private static bool IsValidSpawn(Type t)
        {
            if (t.Name == nameof(RiftMob) || t.Name == nameof(PlaceHolder)) { return false; }

            if (t.Name.EndsWith("EffectNPC") || t.Name == nameof(AmbushNPC)) { return true; }

            if (t.Name == nameof(GameMaster) || t.Name.StartsWith("Summoned")) { return false; }

            if (t.Name.StartsWith("Base")) { return false; }

            if (t.IsClass)
            {
                if (IsValidBase(t))
                {
                    if (t.GetConstructors().Any(c => c.GetParameters().Length == 0))
                    {
                        return true;
                    }
                    else
                    {
                        if (UOR_Settings.ENABLE_DEBUG)
                        {
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"BESTIARY EXCLUDED-[{t.Name}]");
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsValidBase(Type type)
        {
            if (type.BaseType == typeof(BaseCreature)) return true;
            if (type.BaseType == typeof(BaseMount)) return true;
            if (type.BaseType == typeof(BaseRenowned)) return true;
            if (type.BaseType == typeof(BaseEodonTribesman)) return true;
            if (type.IsAbstract) return false;

            return false;
        }

        private static void GenRegionList()
        {
            List<string> regionList = new List<string>();

            try
            {
                string name;
                Map map;
                Rectangle3D[] area;

                string[] areaParts;
                string location;

                int lastMap = 0;

                for (int i = 0; i < Region.Regions.Count; i++)
                {
                    name = Region.Regions[i].Name;
                    map = Region.Regions[i].Map;
                    area = Region.Regions[i].Area;

                    // Clean off bad regions from end of regions.xml
                    if (map.MapID != lastMap)
                    {
                        if (map.MapID > lastMap) lastMap++;

                        if (map.MapID < lastMap) continue;
                    }

                    if (!string.IsNullOrEmpty(name) && UOR_Utility.IsValidRegion(name))
                    {
                        for (int j = 0; j < area.Length; j++)
                        {
                            areaParts = area[j].ToString().Split(',');

                            location = $"{areaParts[0]},{areaParts[1].TrimStart()},{area[j].Width},{area[j].Height})";

                            regionList.Add($"{map.MapID}:{name}:{location}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"REGIONS: {ex.Message}");
            }

            if (regionList.Count > 0)
            {
                File.WriteAllLines(UOR_DIR.REGIONS_LIST_FILE, regionList);

                UOR_Utility.SendMsg(ConsoleColor.Green, "REGIONS-[Generated]");
            }
            else
            {
                UOR_Utility.SendMsg(ConsoleColor.Green, "REGIONS-[Empty]");
            }
        }

        private static void GenTileList()
        {
            List<string> tileNames = new List<string>();

            for (int i = 0; i < TileData.MaxLandValue; i++)
            {
                var name = TileHelper.GetTileName(i, Map.Felucca, Point3D.Zero);

                if (!tileNames.Contains(name) && IsValidTile(name))
                {
                    tileNames.Add(name);
                }
            }

            if (tileNames.Count > 0)
            {
                tileNames.Sort();

                File.WriteAllLines(UOR_DIR.TILE_LIST_FILE, tileNames);

                UOR_Utility.SendMsg(ConsoleColor.Green, $"TILES-[Generated]");
            }
            else
            {
                UOR_Utility.SendMsg(ConsoleColor.Green, "TILES-[Empty]");
            }
        }

        private static bool IsValidTile(string name)
        {
            return !TileHelper.InvalidTileNames.Contains(name);
        }

        private static void GenSpawnerList()
        {
            List<string> allSpawners = new List<string>();

            var spawnerList = World.Items.Values.Where(s => s is ISpawner);

            foreach (var spawner in spawnerList)
            {
                if (spawner is Spawner s && s is ISpawner spwnr)
                {
                    // Get spawn names from the spawner's spawn objects
                    string spawnNames = string.Empty;

                    if (s.SpawnObjects != null && s.SpawnObjects.Count > 0)
                    {
                        spawnNames = string.Join("|", s.SpawnObjects.Select(so => so.SpawnName));
                    }

                    // Format: Serial:MapId:X:Y:HomeRange:MaxCount:SpawnNames
                    allSpawners.Add($"{s.Serial.Value}:{s.Map}:{s.X}:{s.Y}:{spwnr.HomeRange}:{s.MaxCount}:{spawnNames}");
                }

                if (spawner is XmlSpawner xml && xml is ISpawner xspwnr)
                {
                    // Get spawn names from the XmlSpawner's spawn objects
                    string spawnNames = string.Empty;

                    if (xml.SpawnObjects != null && xml.SpawnObjects.Length > 0)
                    {
                        spawnNames = string.Join("|", xml.SpawnObjects.Select(so => so.TypeName));
                    }

                    // Format: Serial:MapId:X:Y:HomeRange:MaxCount:SpawnNames
                    allSpawners.Add($"{xml.Serial.Value}:{xml.Map.MapID}:{xml.X}:{xml.Y}:{xspwnr.HomeRange}:{xml.MaxCount}:{spawnNames}");
                }
            }

            if (allSpawners.Count > 0)
            {
                File.WriteAllLines(UOR_DIR.SPAWNERS_LIST_FILE, allSpawners);

                UOR_Utility.SendMsg(ConsoleColor.Green, "SPAWNERS-[Generated]");
            }
            else
            {
                UOR_Utility.SendMsg(ConsoleColor.Green, "SPAWNERS-[Empty]");
            }
        }

        private static void GenVendorData()
        {
            VendorManager.VendorDataInitialize();
        }
    }
}
