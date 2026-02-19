using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

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
        /// Load tile spawn data using BinaryReader (matches App format)
        /// Format: Version(int), VersionString, MapCount, then per map: MapId, MapName, TileCount,
        ///         then per tile: Id, Name, MapId, WeatherSpawn(int), TimedSpawn(int), 6 spawn lists
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

                int totalTiles = 0;
                int mapCount = 0;

                using (BinaryReader reader = new BinaryReader(File.Open(TileSpawnFile, FileMode.Open, FileAccess.Read)))
                {
                    int fileVersion = reader.ReadInt32();
                    string versionString = reader.ReadString();

                    if (string.IsNullOrWhiteSpace(versionString))
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: TileSpawn binary has no version info");
                    }

                    mapCount = reader.ReadInt32();

                    for (int m = 0; m < mapCount; m++)
                    {
                        int mapId = reader.ReadInt32();
                        string mapName = reader.ReadString();
                        int tileCount = reader.ReadInt32();

                        // Validate map ID
                        if (mapId < 0 || mapId >= Map.Maps.Length)
                        {
                            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                                $"WARNING: Invalid map ID {mapId} in TileSpawn binary - Skipping {tileCount} tiles");
                            // Skip tiles for this invalid map
                            for (int t = 0; t < tileCount; t++)
                                SkipTileEntity(reader);
                            continue;
                        }

                        Map map = Map.Maps[mapId];
                        if (map == null)
                        {
                            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                                $"WARNING: Map ID {mapId} is null in TileSpawn binary - Skipping");
                            for (int t = 0; t < tileCount; t++)
                                SkipTileEntity(reader);
                            continue;
                        }

                        // Initialize list for this map
                        if (!TileSpawns.ContainsKey(map))
                        {
                            TileSpawns[map] = new List<TileEntity>();
                        }

                        for (int t = 0; t < tileCount; t++)
                        {
                            TileEntity entity = ReadTileEntity(reader);
                            if (entity != null && !string.IsNullOrWhiteSpace(entity.Name))
                            {
                                TileSpawns[map].Add(entity);
                                totalTiles++;
                            }
                        }
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

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green,
                    $"Tile Spawn: Loaded {totalTiles} tile(s) across {mapCount} map(s)");
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                    $"ERROR: Failed to load TileSpawn binary - {ex.Message}");
            }
        }

        private static TileEntity ReadTileEntity(BinaryReader reader)
        {
            int id = reader.ReadInt32();
            string name = reader.ReadString();
            int mapId = reader.ReadInt32();
            Enums.WeatherTypes weather = (Enums.WeatherTypes)reader.ReadInt32();
            Enums.TimeNames time = (Enums.TimeNames)reader.ReadInt32();

            TileEntity entity = new TileEntity(name, weather, time)
            {
                WaterSpawnList = new System.Collections.ArrayList(ReadStringList(reader)),
                WeatherSpawnList = new System.Collections.ArrayList(ReadStringList(reader)),
                TimedSpawnList = new System.Collections.ArrayList(ReadStringList(reader)),
                CommonSpawnList = new System.Collections.ArrayList(ReadStringList(reader)),
                UnCommonSpawnList = new System.Collections.ArrayList(ReadStringList(reader)),
                RareSpawnList = new System.Collections.ArrayList(ReadStringList(reader))
            };

            return entity;
        }

        private static void SkipTileEntity(BinaryReader reader)
        {
            reader.ReadInt32(); // Id
            reader.ReadString(); // Name
            reader.ReadInt32(); // MapId
            reader.ReadInt32(); // WeatherSpawn
            reader.ReadInt32(); // TimedSpawn
            SkipStringList(reader); // WaterSpawns
            SkipStringList(reader); // WeatherSpawns
            SkipStringList(reader); // TimedSpawns
            SkipStringList(reader); // CommonSpawns
            SkipStringList(reader); // UncommonSpawns
            SkipStringList(reader); // RareSpawns
        }

        /// <summary>
        /// Load region spawn data using BinaryReader (matches App format)
        /// Format: Version(int), VersionString, MapCount, then per map: MapId, MapName, RegionCount,
        ///         then per region: Id, Name, MapId, WeatherSpawn(int), TimedSpawn(int), 6 spawn lists
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

                int totalRegions = 0;
                int mapCount = 0;

                using (BinaryReader reader = new BinaryReader(File.Open(RegionSpawnFile, FileMode.Open, FileAccess.Read)))
                {
                    int fileVersion = reader.ReadInt32();
                    string versionString = reader.ReadString();

                    if (string.IsNullOrWhiteSpace(versionString))
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: RegionSpawn binary has no version info");
                    }

                    mapCount = reader.ReadInt32();

                    for (int m = 0; m < mapCount; m++)
                    {
                        int mapId = reader.ReadInt32();
                        string mapName = reader.ReadString();
                        int regionCount = reader.ReadInt32();

                        // Validate map ID
                        if (mapId < 0 || mapId >= Map.Maps.Length)
                        {
                            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                                $"WARNING: Invalid map ID {mapId} in RegionSpawn binary - Skipping {regionCount} regions");
                            for (int r = 0; r < regionCount; r++)
                                SkipRegionEntity(reader);
                            continue;
                        }

                        Map map = Map.Maps[mapId];
                        if (map == null)
                        {
                            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                                $"WARNING: Map ID {mapId} is null in RegionSpawn binary - Skipping");
                            for (int r = 0; r < regionCount; r++)
                                SkipRegionEntity(reader);
                            continue;
                        }

                        // Initialize list for this map
                        if (!RegionSpawns.ContainsKey(map))
                        {
                            RegionSpawns[map] = new List<RegionEntity>();
                        }

                        for (int r = 0; r < regionCount; r++)
                        {
                            RegionEntity entity = ReadRegionEntity(reader, map);
                            if (entity != null)
                            {
                                RegionSpawns[map].Add(entity);
                                totalRegions++;
                            }
                        }
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

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green,
                    $"Region Spawn: Loaded {totalRegions} region(s) across {mapCount} map(s)");
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                    $"ERROR: Failed to load RegionSpawn binary - {ex.Message}");
            }
        }

        private static RegionEntity ReadRegionEntity(BinaryReader reader, Map map)
        {
            int id = reader.ReadInt32();
            string name = reader.ReadString();
            int mapId = reader.ReadInt32();
            Enums.WeatherTypes weather = (Enums.WeatherTypes)reader.ReadInt32();
            Enums.TimeNames time = (Enums.TimeNames)reader.ReadInt32();

            List<string> waterSpawns = ReadStringList(reader);
            List<string> weatherSpawns = ReadStringList(reader);
            List<string> timedSpawns = ReadStringList(reader);
            List<string> commonSpawns = ReadStringList(reader);
            List<string> uncommonSpawns = ReadStringList(reader);
            List<string> rareSpawns = ReadStringList(reader);

            if (string.IsNullOrWhiteSpace(name))
                return null;

            // CRITICAL: Lookup Region by name on the map
            if (!map.Regions.TryGetValue(name, out Region regionHandle))
            {
                // Fallback: Case-insensitive search if exact match fails
                regionHandle = map.Regions.Values.FirstOrDefault(r =>
                    r != null &&
                    !string.IsNullOrEmpty(r.Name) &&
                    r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            if (regionHandle == null)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                    $"WARNING: Region '{name}' not found on {map.Name} - Skipping");
                return null;
            }

            RegionEntity entity = new RegionEntity(name, regionHandle, weather, time)
            {
                WaterSpawnList = new System.Collections.ArrayList(waterSpawns),
                WeatherSpawnList = new System.Collections.ArrayList(weatherSpawns),
                TimedSpawnList = new System.Collections.ArrayList(timedSpawns),
                CommonSpawnList = new System.Collections.ArrayList(commonSpawns),
                UnCommonSpawnList = new System.Collections.ArrayList(uncommonSpawns),
                RareSpawnList = new System.Collections.ArrayList(rareSpawns)
            };

            return entity;
        }

        private static void SkipRegionEntity(BinaryReader reader)
        {
            reader.ReadInt32(); // Id
            reader.ReadString(); // Name
            reader.ReadInt32(); // MapId
            reader.ReadInt32(); // WeatherSpawn
            reader.ReadInt32(); // TimedSpawn
            SkipStringList(reader); // WaterSpawns
            SkipStringList(reader); // WeatherSpawns
            SkipStringList(reader); // TimedSpawns
            SkipStringList(reader); // CommonSpawns
            SkipStringList(reader); // UncommonSpawns
            SkipStringList(reader); // RareSpawns
        }

        /// <summary>
        /// Load box spawn data using BinaryReader (matches App format)
        /// Format: Version(int), VersionString, MapCount, then per map: MapId, MapName, BoxCount,
        ///         then per box: Position, Priority, MapId, X, Y, Width, Height, WeatherSpawn(int), TimedSpawn(int), 6 spawn lists
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

                int totalBoxes = 0;
                int mapCount = 0;

                using (BinaryReader reader = new BinaryReader(File.Open(BoxSpawnFile, FileMode.Open, FileAccess.Read)))
                {
                    int fileVersion = reader.ReadInt32();
                    string versionString = reader.ReadString();

                    if (string.IsNullOrWhiteSpace(versionString))
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: BoxSpawn binary has no version info");
                    }

                    mapCount = reader.ReadInt32();

                    for (int m = 0; m < mapCount; m++)
                    {
                        int mapId = reader.ReadInt32();
                        string mapName = reader.ReadString();
                        int boxCount = reader.ReadInt32();

                        // Validate map ID
                        if (mapId < 0 || mapId >= Map.Maps.Length)
                        {
                            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                                $"WARNING: Invalid map ID {mapId} in BoxSpawn binary - Skipping {boxCount} boxes");
                            for (int b = 0; b < boxCount; b++)
                                SkipBoxEntity(reader);
                            continue;
                        }

                        Map map = Map.Maps[mapId];
                        if (map == null)
                        {
                            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                                $"WARNING: Map ID {mapId} is null in BoxSpawn binary - Skipping");
                            for (int b = 0; b < boxCount; b++)
                                SkipBoxEntity(reader);
                            continue;
                        }

                        // Initialize list for this map
                        if (!BoxSpawns.ContainsKey(map))
                        {
                            BoxSpawns[map] = new List<BoxEntity>();
                        }

                        for (int b = 0; b < boxCount; b++)
                        {
                            BoxEntity entity = ReadBoxEntity(reader);
                            if (entity != null)
                            {
                                BoxSpawns[map].Add(entity);
                                totalBoxes++;
                            }
                        }
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

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green,
                    $"Box Spawn: Loaded {totalBoxes} box(es) across {mapCount} map(s)");
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                    $"ERROR: Failed to load BoxSpawn binary - {ex.Message}");
            }
        }

        private static BoxEntity ReadBoxEntity(BinaryReader reader)
        {
            int position = reader.ReadInt32();
            int priority = reader.ReadInt32();
            int mapId = reader.ReadInt32();

            // SpawnBox rect
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();

            Enums.WeatherTypes weather = (Enums.WeatherTypes)reader.ReadInt32();
            Enums.TimeNames time = (Enums.TimeNames)reader.ReadInt32();

            Rectangle2D spawnBox = new Rectangle2D(x, y, width, height);

            BoxEntity entity = new BoxEntity(position, priority, spawnBox, weather, time)
            {
                WaterSpawnList = new System.Collections.ArrayList(ReadStringList(reader)),
                WeatherSpawnList = new System.Collections.ArrayList(ReadStringList(reader)),
                TimedSpawnList = new System.Collections.ArrayList(ReadStringList(reader)),
                CommonSpawnList = new System.Collections.ArrayList(ReadStringList(reader)),
                UnCommonSpawnList = new System.Collections.ArrayList(ReadStringList(reader)),
                RareSpawnList = new System.Collections.ArrayList(ReadStringList(reader))
            };

            return entity;
        }

        private static void SkipBoxEntity(BinaryReader reader)
        {
            reader.ReadInt32(); // Position
            reader.ReadInt32(); // Priority
            reader.ReadInt32(); // MapId
            reader.ReadInt32(); // X
            reader.ReadInt32(); // Y
            reader.ReadInt32(); // Width
            reader.ReadInt32(); // Height
            reader.ReadInt32(); // WeatherSpawn
            reader.ReadInt32(); // TimedSpawn
            SkipStringList(reader); // WaterSpawns
            SkipStringList(reader); // WeatherSpawns
            SkipStringList(reader); // TimedSpawns
            SkipStringList(reader); // CommonSpawns
            SkipStringList(reader); // UncommonSpawns
            SkipStringList(reader); // RareSpawns
        }

        #endregion

        #region Binary Reader Helper Methods

        /// <summary>
        /// Read a list of strings from binary (matches App format)
        /// </summary>
        private static List<string> ReadStringList(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            var list = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadString());
            }
            return list;
        }

        /// <summary>
        /// Skip a string list when skipping invalid data
        /// </summary>
        private static void SkipStringList(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                reader.ReadString();
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
