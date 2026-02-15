using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

using Server.Custom.UORespawnSystem.Entities.BinaryModels;
using Server.Custom.UORespawnSystem.Entities;

namespace Server.Custom.UORespawnSystem.SpawnUtility
{
    internal static class UORespawnDataBase
    {
        // Binary File Paths (Editor creates, Server loads)
        private static readonly string BoxSpawnFile = Path.Combine(UORespawnSettings.UOR_DATA, "UOR_BoxSpawn.bin");
        private static readonly string RegionSpawnFile = Path.Combine(UORespawnSettings.UOR_DATA, "UOR_RegionSpawn.bin");
        private static readonly string TileSpawnFile = Path.Combine(UORespawnSettings.UOR_DATA, "UOR_TileSpawn.bin");

        internal static Dictionary<Map, List<BoxEntity>> BoxSpawns { get; private set; }
        internal static Dictionary<Map, List<RegionEntity>> RegionSpawns { get; private set; }
        internal static Dictionary<Map, List<TileEntity>> TileSpawns { get; private set; }

        internal static void LoadSpawns(string message = "Loading")
        {
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Cyan, $"{message} spawn data...");

            BoxSpawns = new Dictionary<Map, List<BoxEntity>>();
            RegionSpawns = new Dictionary<Map, List<RegionEntity>>();
            TileSpawns = new Dictionary<Map, List<TileEntity>>();

            LoadSettingsData();
            LoadBoxSpawnData();
            LoadRegionSpawnData();
            LoadTileSpawnData();

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Spawn Loaded...");
        }

        internal static void ReLoadSpawns()
        {
            LoadSpawns("Reloading");
        }

        #region Binary Loading Methods

        /// <summary>
        /// Load settings data from Binary format (Editor creates, Server loads)
        /// </summary>
        private static void LoadSettingsData()
        {
            UORespawnSettings.LoadSpawnSettings();
        }

        /// <summary>
        /// Load tile spawn data from Binary format (Editor creates, Server loads)
        /// </summary>
        internal static void LoadTileSpawnData()
        {
            try
            {
                if (!File.Exists(TileSpawnFile))
                {
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                        "ERROR: TileSpawn binary not found - Use Editor to create UOR_TileSpawn.bin");
                    return;
                }

                BinaryFormatter formatter = new BinaryFormatter();
                TileContainer container;

                using (FileStream stream = new FileStream(TileSpawnFile, FileMode.Open, FileAccess.Read))
                {
                    container = (TileContainer)formatter.Deserialize(stream);
                }

                // Version validation (optional)
                if (string.IsNullOrWhiteSpace(container.Version))
                {
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: TileSpawn binary has no version info");
                }

                // Convert DTOs to Entities
                foreach (MapTileData mapData in container.TileData)
                {
                    // Validate map ID
                    if (mapData.MapId < 0 || mapData.MapId >= Map.Maps.Length)
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                            $"WARNING: Invalid map ID {mapData.MapId} in TileSpawn binary - Skipping");
                        continue;
                    }

                    Map map = Map.Maps[mapData.MapId];
                    if (map == null)
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                            $"WARNING: Map ID {mapData.MapId} is null in TileSpawn binary - Skipping");
                        continue;
                    }

                    // Initialize list for this map
                    if (!TileSpawns.ContainsKey(map))
                    {
                        TileSpawns[map] = new List<TileEntity>();
                    }

                    // Convert each TileModel to TileEntity
                    foreach (TileModel tileModel in mapData.TileSpawns)
                    {
                        if (string.IsNullOrWhiteSpace(tileModel.Name))
                            continue;

                        TileEntity entity = new TileEntity(
                            tileModel.Name,
                            tileModel.WeatherSpawn,
                            tileModel.TimedSpawn
                        )
                        {
                            // Convert List<string> to ArrayList
                            WaterSpawnList = new System.Collections.ArrayList(tileModel.WaterSpawns),
                            WeatherSpawnList = new System.Collections.ArrayList(tileModel.WeatherSpawns),
                            TimedSpawnList = new System.Collections.ArrayList(tileModel.TimedSpawns),
                            CommonSpawnList = new System.Collections.ArrayList(tileModel.CommonSpawns),
                            UnCommonSpawnList = new System.Collections.ArrayList(tileModel.UncommonSpawns),
                            RareSpawnList = new System.Collections.ArrayList(tileModel.RareSpawns)
                        };

                        TileSpawns[map].Add(entity);
                    }
                }

                // Ensure all maps have entries (even if empty)
                foreach (Map map in Map.Maps)
                {
                    if (map != null && !TileSpawns.ContainsKey(map))
                    {
                        TileSpawns[map] = new List<TileEntity>();
                    }
                }

                int totalTiles = TileSpawns.Values.Sum(list => list.Count);
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green,
                    $"Tile Spawn: Loaded {totalTiles} tile(s) across {container.TileData.Count} map(s)");
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                    $"ERROR: Failed to load TileSpawn binary - {ex.Message}");
            }
        }

        /// <summary>
        /// Load region spawn data from Binary format (Editor creates, Server loads)
        /// </summary>
        internal static void LoadRegionSpawnData()
        {
            try
            {
                if (!File.Exists(RegionSpawnFile))
                {
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                        "ERROR: RegionSpawn binary not found - Use Editor to create UOR_RegionSpawn.bin");
                    return;
                }

                BinaryFormatter formatter = new BinaryFormatter();
                RegionContainer container;

                using (FileStream stream = new FileStream(RegionSpawnFile, FileMode.Open, FileAccess.Read))
                {
                    container = (RegionContainer)formatter.Deserialize(stream);
                }

                // Version validation (optional)
                if (string.IsNullOrWhiteSpace(container.Version))
                {
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: RegionSpawn binary has no version info");
                }

                // Convert DTOs to Entities
                foreach (MapRegionData mapData in container.RegionData)
                {
                    // Validate map ID
                    if (mapData.MapId < 0 || mapData.MapId >= Map.Maps.Length)
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                            $"WARNING: Invalid map ID {mapData.MapId} in RegionSpawn binary - Skipping");
                        continue;
                    }

                    Map map = Map.Maps[mapData.MapId];
                    if (map == null)
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                            $"WARNING: Map ID {mapData.MapId} is null in RegionSpawn binary - Skipping");
                        continue;
                    }

                    // Initialize list for this map
                    if (!RegionSpawns.ContainsKey(map))
                    {
                        RegionSpawns[map] = new List<RegionEntity>();
                    }

                    // Convert each RegionModel to RegionEntity
                    foreach (RegionModel regionModel in mapData.RegionSpawns)
                    {
                        if (string.IsNullOrWhiteSpace(regionModel.Name))
                            continue;

                        // CRITICAL: Lookup Region by name on the map
                        // map.Regions is a Dictionary<string, Region> where the key is the region name
                        // Since the Editor saves region names from [GenRegionList], we can do a direct lookup

                        // Direct dictionary lookup by region name (most efficient)
                        if (!map.Regions.TryGetValue(regionModel.Name, out Region regionHandle))
                        {
                            // Fallback: Case-insensitive search if exact match fails
                            regionHandle = map.Regions.Values.FirstOrDefault(r =>
                                r != null &&
                                !string.IsNullOrEmpty(r.Name) &&
                                r.Name.Equals(regionModel.Name, StringComparison.OrdinalIgnoreCase));
                        }

                        if (regionHandle == null)
                        {
                            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                                $"WARNING: Region '{regionModel.Name}' not found on {map.Name} - Skipping");
                            continue;
                        }

                        RegionEntity entity = new RegionEntity(
                            regionModel.Name,
                            regionHandle,
                            regionModel.WeatherSpawn,
                            regionModel.TimedSpawn
                        )
                        {
                            // Convert List<string> to ArrayList
                            WaterSpawnList = new System.Collections.ArrayList(regionModel.WaterSpawns),
                            WeatherSpawnList = new System.Collections.ArrayList(regionModel.WeatherSpawns),
                            TimedSpawnList = new System.Collections.ArrayList(regionModel.TimedSpawns),
                            CommonSpawnList = new System.Collections.ArrayList(regionModel.CommonSpawns),
                            UnCommonSpawnList = new System.Collections.ArrayList(regionModel.UncommonSpawns),
                            RareSpawnList = new System.Collections.ArrayList(regionModel.RareSpawns)
                        };

                        RegionSpawns[map].Add(entity);
                    }
                }

                // Ensure all maps have entries (even if empty)
                foreach (Map map in Map.Maps)
                {
                    if (map != null && !RegionSpawns.ContainsKey(map))
                    {
                        RegionSpawns[map] = new List<RegionEntity>();
                    }
                }

                int totalRegions = RegionSpawns.Values.Sum(list => list.Count);
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green,
                    $"Region Spawn: Loaded {totalRegions} region(s) across {container.RegionData.Count} map(s)");
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                    $"ERROR: Failed to load RegionSpawn binary - {ex.Message}");
            }
        }

        /// <summary>
        /// Load box spawn data from Binary format (Editor creates, Server loads)
        /// </summary>
        internal static void LoadBoxSpawnData()
        {
            try
            {
                if (!File.Exists(BoxSpawnFile))
                {
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                        "ERROR: BoxSpawn binary not found - Use Editor to create UOR_BoxSpawn.bin");
                    return;
                }

                BinaryFormatter formatter = new BinaryFormatter();
                BoxContainer container;

                using (FileStream stream = new FileStream(BoxSpawnFile, FileMode.Open, FileAccess.Read))
                {
                    container = (BoxContainer)formatter.Deserialize(stream);
                }

                // Version validation (optional)
                if (string.IsNullOrWhiteSpace(container.Version))
                {
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: BoxSpawn binary has no version info");
                }

                // Convert DTOs to Entities
                foreach (MapBoxData mapData in container.BoxData)
                {
                    // Validate map ID
                    if (mapData.MapId < 0 || mapData.MapId >= Map.Maps.Length)
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                            $"WARNING: Invalid map ID {mapData.MapId} in BoxSpawn binary - Skipping");
                        continue;
                    }

                    Map map = Map.Maps[mapData.MapId];
                    if (map == null)
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                            $"WARNING: Map ID {mapData.MapId} is null in BoxSpawn binary - Skipping");
                        continue;
                    }

                    // Initialize list for this map
                    if (!BoxSpawns.ContainsKey(map))
                    {
                        BoxSpawns[map] = new List<BoxEntity>();
                    }

                    // Convert each BoxModel to BoxEntity
                    foreach (BoxModel boxModel in mapData.BoxSpawns)
                    {
                        Rectangle2D spawnBox = new Rectangle2D(
                            boxModel.X,
                            boxModel.Y,
                            boxModel.Width,
                            boxModel.Height
                        );

                        BoxEntity entity = new BoxEntity(
                            boxModel.Id,
                            boxModel.SpawnPriority,
                            spawnBox,
                            boxModel.WeatherSpawn,
                            boxModel.TimedSpawn
                        )
                        {
                            // Convert List<string> to ArrayList
                            WaterSpawnList = new System.Collections.ArrayList(boxModel.WaterSpawns),
                            WeatherSpawnList = new System.Collections.ArrayList(boxModel.WeatherSpawns),
                            TimedSpawnList = new System.Collections.ArrayList(boxModel.TimedSpawns),
                            CommonSpawnList = new System.Collections.ArrayList(boxModel.CommonSpawns),
                            UnCommonSpawnList = new System.Collections.ArrayList(boxModel.UncommonSpawns),
                            RareSpawnList = new System.Collections.ArrayList(boxModel.RareSpawns)
                        };

                        BoxSpawns[map].Add(entity);
                    }
                }

                // Ensure all maps have entries (even if empty)
                foreach (Map map in Map.Maps)
                {
                    if (map != null && !BoxSpawns.ContainsKey(map))
                    {
                        BoxSpawns[map] = new List<BoxEntity>();
                    }
                }

                int totalBoxes = BoxSpawns.Values.Sum(list => list.Count);
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green,
                    $"Box Spawn: Loaded {totalBoxes} box(es) across {container.BoxData.Count} map(s)");
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                    $"ERROR: Failed to load BoxSpawn binary - {ex.Message}");
            }
        }

        #endregion

        #region Binary Save Methods (Placeholders - Editor handles saving)

        /// <summary>
        /// Placeholder: Binary save should be done from Editor
        /// TODO: Future feature for in-game editing
        /// </summary>
        internal static void SaveTileSpawnData()
        {
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: Not implemented!");
            // TODO: Implement when in-game editing feature is added
        }

        /// <summary>
        /// Placeholder: Binary save should be done from Editor
        /// TODO: Future feature for in-game editing
        /// </summary>
        internal static void SaveRegionSpawnData()
        {
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: Not implemented!");
            // TODO: Implement when in-game editing feature is added
        }

        /// <summary>
        /// Placeholder: Binary save should be done from Editor
        /// TODO: Future feature for in-game editing
        /// </summary>
        internal static void SaveBoxSpawnData()
        {
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: Not implemented!");
            // TODO: Implement when in-game editing feature is added
        }

        #endregion
    }
}
