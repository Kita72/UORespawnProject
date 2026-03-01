using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Server.Custom.UORespawnServer.Entities;
using Server.Items;

namespace Server.Custom.UORespawnServer.Managers
{
    // Manages Spawn Data : Files and Dirctories!
    internal static class SpawnManager
    {
        // Binary File Paths (Editor creates, Server loads)
        private static readonly string BoxSpawnFile = UOR_DIR.BOX_DATA_FILE;
        private static readonly string RegionSpawnFile = UOR_DIR.REGION_DATA_FILE;
        private static readonly string TileSpawnFile = UOR_DIR.TILE_DATA_FILE;
        private static readonly string VendorSpawnFile = UOR_DIR.VENDOR_DATA_FILE;

        internal static Dictionary<Map, List<BoxEntity>> BoxSpawns { get; private set; }
        internal static Dictionary<Map, List<RegionEntity>> RegionSpawns { get; private set; }
        internal static Dictionary<Map, List<TileEntity>> TileSpawns { get; private set; }
        internal static Dictionary<Map, List<VendorEntity>> VendorSpawns { get; private set; }

        // O(1) Lookup Dictionaries (built after loading)
        internal static Dictionary<Map, Dictionary<Region, RegionEntity>> RegionLookup { get; private set; }
        internal static Dictionary<Map, Dictionary<string, TileEntity>> TileLookup { get; private set; }

        internal static void LoadSpawns(string message = "Loading")
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, $"SPAWN-[{message}...");

            BoxSpawns = new Dictionary<Map, List<BoxEntity>>();
            RegionSpawns = new Dictionary<Map, List<RegionEntity>>();
            TileSpawns = new Dictionary<Map, List<TileEntity>>();
            VendorSpawns = new Dictionary<Map, List<VendorEntity>>();

            LoadSettingsData();
            LoadBoxSpawnData();
            LoadRegionSpawnData();
            LoadTileSpawnData();
            LoadVendorSpawnData();

            // Build O(1) lookup structures
            SpatialGridManager.Initialize(BoxSpawns);
            BuildRegionLookup();
            BuildTileLookup();

            UOR_Utility.SendMsg(ConsoleColor.Green, "SPAWN-[Loaded]");
        }

        internal static void ReLoadSpawns()
        {
            // Clear spatial grid before reload
            SpatialGridManager.Clear();

            // Reload all spawn data (including fresh vendor data)
            LoadSpawns("Reloading");

            UOR_Utility.SendMsg(ConsoleColor.Green, "SPAWN-[Reloaded]");
        }

        /// <summary>
        /// Load settings data from Binary format (Editor creates, Server loads)
        /// </summary>
        private static void LoadSettingsData()
        {
            UOR_Settings.LoadSpawnSettings();
        }

        /// <summary>
        /// Load box spawn data using BinaryReader (matches App format)
        /// Format: Version(int), VersionString, MapCount, then per map: MapId, MapName, BoxCount,
        ///         then per box: Position, Priority, MapId, X, Y, Width, Height, WeatherSpawn(int), TimedSpawn(int), 6 spawn lists
        /// </summary>
        private static void LoadBoxSpawnData()
        {
            try
            {
                if (!File.Exists(BoxSpawnFile))
                {
                    UOR_Utility.SendMsg(ConsoleColor.Red, "BoxSpawn binary not found - Use Editor to create UOR_BoxSpawn.bin");

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
                        UOR_Utility.SendMsg(ConsoleColor.Yellow, "BoxSpawn binary has no version info");
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
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Invalid map ID {mapId} in BoxSpawn binary - Skipping {boxCount} boxes");

                            for (int b = 0; b < boxCount; b++)
                                SkipBoxEntity(reader);
                            continue;
                        }

                        Map map = Map.Maps[mapId];

                        if (map == null)
                        {
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Map ID {mapId} is null in BoxSpawn binary - Skipping");

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
                    if (map != null && map != Map.Internal && !BoxSpawns.ContainsKey(map))
                    {
                        BoxSpawns[map] = new List<BoxEntity>();
                    }
                }

                UOR_Utility.SendMsg(ConsoleColor.Green, $"BOX SPAWN-[Loaded {totalBoxes} boxes across {mapCount} maps]");
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"Failed to load BoxSpawn binary - {ex.Message}");
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
            Enums.TimeTypes time = (Enums.TimeTypes)reader.ReadInt32();

            Rectangle2D spawnBox = new Rectangle2D(x, y, width, height);

            BoxEntity entity = new BoxEntity(position, priority, spawnBox, weather, time)
            {
                WaterList = new System.Collections.ArrayList(ReadStringList(reader)),
                WeatherList = new System.Collections.ArrayList(ReadStringList(reader)),
                TimedList = new System.Collections.ArrayList(ReadStringList(reader)),
                CommonList = new System.Collections.ArrayList(ReadStringList(reader)),
                UnCommonList = new System.Collections.ArrayList(ReadStringList(reader)),
                RareList = new System.Collections.ArrayList(ReadStringList(reader))
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

        /// <summary>
        /// Load region spawn data using BinaryReader (matches App format)
        /// Format: Version(int), VersionString, MapCount, then per map: MapId, MapName, RegionCount,
        ///         then per region: Id, Name, MapId, WeatherSpawn(int), TimedSpawn(int), 6 spawn lists
        /// </summary>
        private static void LoadRegionSpawnData()
        {
            try
            {
                if (!File.Exists(RegionSpawnFile))
                {
                    UOR_Utility.SendMsg(ConsoleColor.Red, "RegionSpawn binary not found - Use Editor to create UOR_RegionSpawn.bin");
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
                        UOR_Utility.SendMsg(ConsoleColor.Yellow, "RegionSpawn binary has no version info");
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
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Invalid map ID {mapId} in RegionSpawn binary - Skipping {regionCount} regions");
                            for (int r = 0; r < regionCount; r++)
                                SkipRegionEntity(reader);
                            continue;
                        }

                        Map map = Map.Maps[mapId];
                        if (map == null)
                        {
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Map ID {mapId} is null in RegionSpawn binary - Skipping");
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
                    if (map != null && map != Map.Internal && !RegionSpawns.ContainsKey(map))
                    {
                        RegionSpawns[map] = new List<RegionEntity>();
                    }
                }

                UOR_Utility.SendMsg(ConsoleColor.Green, $"REGION SPAWN-[Loaded {totalRegions} regions across {mapCount} maps]");
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"Failed to load RegionSpawn binary - {ex.Message}");
            }
        }

        private static RegionEntity ReadRegionEntity(BinaryReader reader, Map map)
        {
            int id = reader.ReadInt32();
            string name = reader.ReadString();
            int mapId = reader.ReadInt32();
            Enums.WeatherTypes weather = (Enums.WeatherTypes)reader.ReadInt32();
            Enums.TimeTypes time = (Enums.TimeTypes)reader.ReadInt32();

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
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Region '{name}' not found on {map.Name} - Skipping");
                return null;
            }

            RegionEntity entity = new RegionEntity(name, regionHandle, weather, time)
            {
                WaterList = new System.Collections.ArrayList(waterSpawns),
                WeatherList = new System.Collections.ArrayList(weatherSpawns),
                TimedList = new System.Collections.ArrayList(timedSpawns),
                CommonList = new System.Collections.ArrayList(commonSpawns),
                UnCommonList = new System.Collections.ArrayList(uncommonSpawns),
                RareList = new System.Collections.ArrayList(rareSpawns)
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
        /// Load tile spawn data using BinaryReader (matches App format)
        /// Format: Version(int), VersionString, MapCount, then per map: MapId, MapName, TileCount,
        ///         then per tile: Id, Name, MapId, WeatherSpawn(int), TimedSpawn(int), 6 spawn lists
        /// </summary>
        private static void LoadTileSpawnData()
        {
            try
            {
                if (!File.Exists(TileSpawnFile))
                {
                    UOR_Utility.SendMsg(ConsoleColor.Red, "TileSpawn binary not found - Use Editor to create UOR_TileSpawn.bin");

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
                        UOR_Utility.SendMsg(ConsoleColor.Yellow, "TileSpawn binary has no version info");
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
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Invalid map ID {mapId} in TileSpawn binary - Skipping {tileCount} tiles");
                            // Skip tiles for this invalid map
                            for (int t = 0; t < tileCount; t++)
                                SkipTileEntity(reader);
                            continue;
                        }

                        Map map = Map.Maps[mapId];
                        if (map == null)
                        {
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Map ID {mapId} is null in TileSpawn binary - Skipping");
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
                    if (map != null && map != Map.Internal && !TileSpawns.ContainsKey(map))
                    {
                        TileSpawns[map] = new List<TileEntity>();
                    }
                }

                UOR_Utility.SendMsg(ConsoleColor.Green, $"TILE SPAWN-[Loaded {totalTiles} tiles across {mapCount} maps]");
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"Failed to load TileSpawn binary - {ex.Message}");
            }
        }

        private static TileEntity ReadTileEntity(BinaryReader reader)
        {
            int id = reader.ReadInt32();
            string name = reader.ReadString();
            int mapId = reader.ReadInt32();
            Enums.WeatherTypes weather = (Enums.WeatherTypes)reader.ReadInt32();
            Enums.TimeTypes time = (Enums.TimeTypes)reader.ReadInt32();

            TileEntity entity = new TileEntity(name, weather, time)
            {
                WaterList = new System.Collections.ArrayList(ReadStringList(reader)),
                WeatherList = new System.Collections.ArrayList(ReadStringList(reader)),
                TimedList = new System.Collections.ArrayList(ReadStringList(reader)),
                CommonList = new System.Collections.ArrayList(ReadStringList(reader)),
                UnCommonList = new System.Collections.ArrayList(ReadStringList(reader)),
                RareList = new System.Collections.ArrayList(ReadStringList(reader))
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
        /// Load vendor spawn data using BinaryReader (matches App format)
        /// Format: Version(int), VersionString, MapCount, then per map: MapId, MapName, VendorCount,
        ///         then per vendor: IsSign(bool), SignType(int), SignFacing(int), X, Y, Z, VendorList
        /// </summary>
        internal static void LoadVendorSpawnData()
        {
            try
            {
                if (!File.Exists(VendorSpawnFile))
                {
                    UOR_Utility.SendMsg(ConsoleColor.Red, "VendorSpawn binary not found - Use Editor to create UOR_VendorSpawn.bin");
                    return;
                }

                int totalVendors = 0;
                int mapCount = 0;

                using (BinaryReader reader = new BinaryReader(File.Open(VendorSpawnFile, FileMode.Open, FileAccess.Read)))
                {
                    int fileVersion = reader.ReadInt32();
                    string versionString = reader.ReadString();

                    if (string.IsNullOrWhiteSpace(versionString))
                    {
                        UOR_Utility.SendMsg(ConsoleColor.Yellow, "VendorSpawn binary has no version info");
                    }

                    mapCount = reader.ReadInt32();

                    for (int m = 0; m < mapCount; m++)
                    {
                        int mapId = reader.ReadInt32();
                        string mapName = reader.ReadString();
                        int vendorCount = reader.ReadInt32();

                        // Validate map ID
                        if (mapId < 0 || mapId >= Map.Maps.Length)
                        {
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Invalid map ID {mapId} in VendorSpawn binary - Skipping {vendorCount} vendors");

                            for (int v = 0; v < vendorCount; v++)
                                SkipVendorEntity(reader);
                            continue;
                        }

                        Map map = Map.Maps[mapId];

                        if (map == null)
                        {
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Map ID {mapId} is null in VendorSpawn binary - Skipping");

                            for (int v = 0; v < vendorCount; v++)
                                SkipVendorEntity(reader);
                            continue;
                        }

                        // Initialize list for this map
                        if (!VendorSpawns.ContainsKey(map))
                        {
                            VendorSpawns[map] = new List<VendorEntity>();
                        }

                        for (int v = 0; v < vendorCount; v++)
                        {
                            VendorEntity entity = ReadVendorEntity(reader);
                            if (entity != null)
                            {
                                VendorSpawns[map].Add(entity);
                                totalVendors++;
                            }
                        }
                    }
                }

                // Ensure all maps have entries (even if empty)
                foreach (Map map in Map.Maps)
                {
                    if (map != null && map != Map.Internal && !VendorSpawns.ContainsKey(map))
                    {
                        VendorSpawns[map] = new List<VendorEntity>();
                    }
                }

                UOR_Utility.SendMsg(ConsoleColor.Green, $"VENDOR SPAWN-[Loaded {totalVendors} vendor locations across {mapCount} maps]");
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"Failed to load VendorSpawn binary - {ex.Message}");
            }
        }

        private static VendorEntity ReadVendorEntity(BinaryReader reader)
        {
            bool isSign = reader.ReadBoolean();
            SignType signType = (SignType)reader.ReadInt32();
            SignFacing signFacing = (SignFacing)reader.ReadInt32();

            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            int z = reader.ReadInt32();

            Point3D location = new Point3D(x, y, z);

            List<string> vendorList = ReadStringList(reader);

            VendorEntity entity;

            if (isSign)
            {
                entity = new VendorEntity(signType, signFacing, location);
            }
            else
            {
                entity = new VendorEntity(location);
            }

            // Add vendors from the list
            foreach (string vendor in vendorList)
            {
                entity.AddVendor(vendor);
            }

            return entity;
        }

        private static void SkipVendorEntity(BinaryReader reader)
        {
            reader.ReadBoolean(); // IsSign
            reader.ReadInt32();   // SignType
            reader.ReadInt32();   // SignFacing
            reader.ReadInt32();   // X
            reader.ReadInt32();   // Y
            reader.ReadInt32();   // Z
            SkipStringList(reader); // VendorList
        }

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

        /// <summary>
        /// Build O(1) lookup dictionary for region spawns (keyed by Region handle)
        /// </summary>
        private static void BuildRegionLookup()
        {
            RegionLookup = new Dictionary<Map, Dictionary<Region, RegionEntity>>();
            int totalEntries = 0;

            foreach (var kvp in RegionSpawns)
            {
                RegionLookup[kvp.Key] = new Dictionary<Region, RegionEntity>();

                foreach (var entity in kvp.Value)
                {
                    if (entity.RegionHandle != null && !RegionLookup[kvp.Key].ContainsKey(entity.RegionHandle))
                    {
                        RegionLookup[kvp.Key][entity.RegionHandle] = entity;
                        totalEntries++;
                    }
                }
            }

            UOR_Utility.SendMsg(ConsoleColor.Green, $"REGION LOOKUP-[Built with {totalEntries} entries across {RegionLookup.Count} maps]");
        }

        /// <summary>
        /// Build O(1) lookup dictionary for tile spawns (keyed by tile name)
        /// </summary>
        private static void BuildTileLookup()
        {
            TileLookup = new Dictionary<Map, Dictionary<string, TileEntity>>();
            int totalEntries = 0;

            foreach (var kvp in TileSpawns)
            {
                TileLookup[kvp.Key] = new Dictionary<string, TileEntity>(StringComparer.OrdinalIgnoreCase);

                foreach (var entity in kvp.Value)
                {
                    if (!string.IsNullOrEmpty(entity.Name) && !TileLookup[kvp.Key].ContainsKey(entity.Name))
                    {
                        TileLookup[kvp.Key][entity.Name] = entity;
                        totalEntries++;
                    }
                }
            }

            UOR_Utility.SendMsg(ConsoleColor.Green, $"TILE LOOKUP-[Built with {totalEntries} entries across {TileLookup.Count} maps]");
        }
    }
}
