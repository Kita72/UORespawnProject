using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    /// <summary>
    /// Service for binary serialization of UORespawn v2.0 data files
    /// Uses BinaryReader/BinaryWriter (ServUO-style) for .NET 10 compatibility
    /// Generates files compatible with server-side UORespawnDataBase loader
    /// </summary>
    public static class BinarySerializationService
    {
        // Current file format versions
        private const int SETTINGS_VERSION = 1;
        private const int BOX_SPAWN_VERSION = 1;
        private const int TILE_SPAWN_VERSION = 1;
        private const int REGION_SPAWN_VERSION = 1;

        #region Active Pack Sync Helper

        /// <summary>
        /// Copies a file to the active approved pack folder if one is set.
        /// Called after saving spawn/settings files to keep the pack in sync with edits.
        /// </summary>
        private static void SyncFileToActivePack(string sourceFilePath, string fileName)
        {
            try
            {
                // Skip sync if suppressed (during ApplyPack to preserve original bytes)
                if (PathConstants.SuppressPackSync)
                {
                    return;
                }

                var activePackPath = PathConstants.ActivePackDataPath;
                if (string.IsNullOrEmpty(activePackPath) || !Directory.Exists(activePackPath))
                {
                    return;
                }

                var destFilePath = Path.Combine(activePackPath, fileName);
                File.Copy(sourceFilePath, destFilePath, overwrite: true);

                Logger.Info($"Synced {fileName} to active pack: {activePackPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error syncing {fileName} to active pack", ex);
            }
        }

        #endregion

        #region Settings Save/Load

        /// <summary>
        /// Save settings to binary file using BinaryWriter
        /// </summary>
        public static void SaveSettings()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.SETTINGS_FILENAME);

                WriteSettings(localPath);

                Logger.Info($"Settings saved to: {localPath}");

                // Sync to server if linked
                var serverPath = PathConstants.ServerDataPath;

                if (serverPath != null)
                {
                    var serverFilePath = PathConstants.GetServerFilePath(PathConstants.SETTINGS_FILENAME);

                    if (serverFilePath != null)
                    {
                        WriteSettings(serverFilePath);

                        Logger.Info($"Settings synced to server: {serverFilePath}");
                    }
                }

                // Sync to active approved pack if set
                SyncFileToActivePack(localPath, PathConstants.SETTINGS_FILENAME);
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving settings", ex);
            }
        }

        private static void WriteSettings(string filePath)
        {
            using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));

            writer.Write(SETTINGS_VERSION);
            writer.Write(Utility.Version);

            // Basic spawn limits
            writer.Write(Settings.MaxMobs);
            writer.Write(Settings.MinRange);
            writer.Write(Settings.MaxRange);
            writer.Write(Settings.MaxCrowd);

            // Spawn chances (doubles)
            writer.Write(Settings.WaterChance);
            writer.Write(Settings.WeatherChance);
            writer.Write(Settings.TimedChance);
            writer.Write(Settings.CommonChance);
            writer.Write(Settings.UnCommonChance);
            writer.Write(Settings.RareChance);

            // Feature flags
            writer.Write(Settings.IsScaleSpawn);
            writer.Write(Settings.EnableRiftSpawn);
            writer.Write(Settings.EnableDebugSpawn);
            writer.Write(Settings.EnableVendorSpawn);
        }

        /// <summary>
        /// Load settings from binary file using BinaryReader
        /// </summary>
        public static bool LoadSettings()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.SETTINGS_FILENAME);

                if (!File.Exists(localPath))
                {
                    Logger.Warning($"Settings file not found: {localPath}");

                    return false;
                }

                ReadSettings(localPath);

                Logger.Info($"Settings loaded from: {localPath}");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading settings", ex);

                return false;
            }
        }

        private static void ReadSettings(string filePath)
        {
            using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));

            int version = reader.ReadInt32();
            string fileVersion = reader.ReadString(); // App version string (for info)

            // Basic spawn limits
            Settings.MaxMobs = reader.ReadInt32();
            Settings.MinRange = reader.ReadInt32();
            Settings.MaxRange = reader.ReadInt32();
            Settings.MaxCrowd = reader.ReadInt32();

            // Spawn chances
            Settings.WaterChance = reader.ReadDouble();
            Settings.WeatherChance = reader.ReadDouble();
            Settings.TimedChance = reader.ReadDouble();
            Settings.CommonChance = reader.ReadDouble();
            Settings.UnCommonChance = reader.ReadDouble();
            Settings.RareChance = reader.ReadDouble();

            // Feature flags
            Settings.IsScaleSpawn = reader.ReadBoolean();
            Settings.EnableRiftSpawn = reader.ReadBoolean();
            Settings.EnableDebugSpawn = reader.ReadBoolean();
            Settings.EnableVendorSpawn = reader.ReadBoolean();
        }

        #endregion

        #region Box Spawn Save/Load

        /// <summary>
        /// Save box spawns to binary file using BinaryWriter
        /// </summary>
        public static void SaveBoxSpawns()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.BOX_FILENAME);

                int count = WriteBoxSpawns(localPath);

                Logger.Info($"Box spawns saved to: {localPath} ({count} boxes)");

                // Sync to server if linked
                var serverPath = PathConstants.ServerDataPath;

                if (serverPath != null)
                {
                    var serverFilePath = PathConstants.GetServerFilePath(PathConstants.BOX_FILENAME);

                    if (serverFilePath != null)
                    {
                        WriteBoxSpawns(serverFilePath);

                        Logger.Info($"Box spawns synced to server: {serverFilePath}");
                    }
                }

                // Sync to active approved pack if set
                SyncFileToActivePack(localPath, PathConstants.BOX_FILENAME);
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving box spawns", ex);
            }
        }

        private static int WriteBoxSpawns(string filePath)
        {
            using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));

            writer.Write(BOX_SPAWN_VERSION);
            writer.Write(Utility.Version);

            // Count maps that have spawn data
            var mapsWithData = Utility.BoxSpawns.Where(kvp => kvp.Value.Count > 0).ToList();
            writer.Write(mapsWithData.Count);

            int totalBoxes = 0;

            foreach (var mapEntry in mapsWithData)
            {
                writer.Write(mapEntry.Key); // MapId
                writer.Write(MapUtility.GetMapName(mapEntry.Key)); // MapName
                writer.Write(mapEntry.Value.Count); // Number of boxes

                foreach (var box in mapEntry.Value)
                {
                    WriteBoxSpawnEntity(writer, box);
                    totalBoxes++;
                }
            }

            return totalBoxes;
        }

        private static void WriteBoxSpawnEntity(BinaryWriter writer, BoxSpawnEntity box)
        {
            writer.Write(box.Position);
            writer.Write(box.Priority);
            writer.Write(box.MapId);

            // SpawnBox rect
            writer.Write((int)box.SpawnBox.X);
            writer.Write((int)box.SpawnBox.Y);
            writer.Write((int)box.SpawnBox.Width);
            writer.Write((int)box.SpawnBox.Height);

            // Enums as int
            writer.Write((int)box.WeatherSpawn);
            writer.Write((int)box.TimedSpawn);

            // Spawn lists
            WriteStringList(writer, box.WaterSpawns);
            WriteStringList(writer, box.WeatherSpawns);
            WriteStringList(writer, box.TimedSpawns);
            WriteStringList(writer, box.CommonSpawns);
            WriteStringList(writer, box.UncommonSpawns);
            WriteStringList(writer, box.RareSpawns);
        }

        /// <summary>
        /// Load box spawns from binary file using BinaryReader
        /// </summary>
        public static bool LoadBoxSpawns()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.BOX_FILENAME);

                if (!File.Exists(localPath))
                {
                    Logger.Warning($"Box spawns file not found: {localPath}");

                    return false;
                }

                int count = ReadBoxSpawns(localPath);

                Logger.Info($"Box spawns loaded from: {localPath} ({count} boxes)");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading box spawns", ex);

                return false;
            }
        }

        private static int ReadBoxSpawns(string filePath)
        {
            using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));

            int version = reader.ReadInt32();
            string fileVersion = reader.ReadString();

            int mapCount = reader.ReadInt32();
            int totalBoxes = 0;

            for (int m = 0; m < mapCount; m++)
            {
                int mapId = reader.ReadInt32();
                string mapName = reader.ReadString();
                int boxCount = reader.ReadInt32();

                // Ensure the map entry exists
                if (!Utility.BoxSpawns.TryGetValue(mapId, out List<BoxSpawnEntity>? value))
                {
                    value = [];

                    Utility.BoxSpawns[mapId] = value;
                }

                for (int b = 0; b < boxCount; b++)
                {
                    var box = ReadBoxSpawnEntity(reader);
                    value.Add(box);
                    totalBoxes++;
                }
            }

            return totalBoxes;
        }

        private static BoxSpawnEntity ReadBoxSpawnEntity(BinaryReader reader)
        {
            var box = new BoxSpawnEntity
            {
                Position = reader.ReadInt32(),
                Priority = reader.ReadInt32(),
                MapId = reader.ReadInt32()
            };

            // SpawnBox rect
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();

            box.SpawnBox = new Rect(x, y, width, height);

            // Enums
            box.WeatherSpawn = (WeatherTypes)reader.ReadInt32();
            box.TimedSpawn = (TimeNames)reader.ReadInt32();

            // Spawn lists
            box.WaterSpawns = ReadStringList(reader);
            box.WeatherSpawns = ReadStringList(reader);
            box.TimedSpawns = ReadStringList(reader);
            box.CommonSpawns = ReadStringList(reader);
            box.UncommonSpawns = ReadStringList(reader);
            box.RareSpawns = ReadStringList(reader);

            return box;
        }

        #endregion

        #region Tile Spawn Save/Load

        /// <summary>
        /// Save tile spawns to binary file using BinaryWriter
        /// </summary>
        public static void SaveTileSpawns()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.TILE_FILENAME);

                int count = WriteTileSpawns(localPath);

                Logger.Info($"Tile spawns saved to: {localPath} ({count} tiles)");

                // Sync to server if linked
                var serverPath = PathConstants.ServerDataPath;

                if (serverPath != null)
                {
                    var serverFilePath = PathConstants.GetServerFilePath(PathConstants.TILE_FILENAME);

                    if(serverFilePath != null)
                    {
                        WriteTileSpawns(serverFilePath);

                        Logger.Info($"Tile spawns synced to server: {serverFilePath}");
                    }
                }

                // Sync to active approved pack if set
                SyncFileToActivePack(localPath, PathConstants.TILE_FILENAME);
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving tile spawns", ex);
            }
        }

        private static int WriteTileSpawns(string filePath)
        {
            using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));

            writer.Write(TILE_SPAWN_VERSION);
            writer.Write(Utility.Version);

            var mapsWithData = Utility.TileSpawns.Where(kvp => kvp.Value.Count > 0).ToList();
            writer.Write(mapsWithData.Count);

            int totalTiles = 0;

            foreach (var mapEntry in mapsWithData)
            {
                writer.Write(mapEntry.Key);
                writer.Write(MapUtility.GetMapName(mapEntry.Key));
                writer.Write(mapEntry.Value.Count);

                foreach (var tile in mapEntry.Value)
                {
                    WriteTileSpawnEntity(writer, tile);
                    totalTiles++;
                }
            }

            return totalTiles;
        }

        private static void WriteTileSpawnEntity(BinaryWriter writer, TileSpawnEntity tile)
        {
            writer.Write(tile.Id);
            writer.Write(tile.Name ?? string.Empty);
            writer.Write(tile.MapId);

            writer.Write((int)tile.WeatherSpawn);
            writer.Write((int)tile.TimedSpawn);

            WriteStringList(writer, tile.WaterSpawns);
            WriteStringList(writer, tile.WeatherSpawns);
            WriteStringList(writer, tile.TimedSpawns);
            WriteStringList(writer, tile.CommonSpawns);
            WriteStringList(writer, tile.UncommonSpawns);
            WriteStringList(writer, tile.RareSpawns);
        }

        /// <summary>
        /// Load tile spawns from binary file using BinaryReader
        /// </summary>
        public static bool LoadTileSpawns()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.TILE_FILENAME);

                if (!File.Exists(localPath))
                {
                    Logger.Warning($"Tile spawns file not found: {localPath}");

                    return false;
                }

                int count = ReadTileSpawns(localPath);

                Logger.Info($"Tile spawns loaded from: {localPath} ({count} tiles)");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading tile spawns", ex);

                return false;
            }
        }

        private static int ReadTileSpawns(string filePath)
        {
            using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));

            int version = reader.ReadInt32();
            string fileVersion = reader.ReadString();

            int mapCount = reader.ReadInt32();
            int totalTiles = 0;

            for (int m = 0; m < mapCount; m++)
            {
                int mapId = reader.ReadInt32();
                string mapName = reader.ReadString();
                int tileCount = reader.ReadInt32();

                if (!Utility.TileSpawns.TryGetValue(mapId, out List<TileSpawnEntity>? value))
                {
                    value = [];

                    Utility.TileSpawns[mapId] = value;
                }

                for (int t = 0; t < tileCount; t++)
                {
                    var tile = ReadTileSpawnEntity(reader);
                    value.Add(tile);
                    totalTiles++;
                }
            }

            return totalTiles;
        }

        private static TileSpawnEntity ReadTileSpawnEntity(BinaryReader reader)
        {
            var tile = new TileSpawnEntity
            {
                Id = reader.ReadInt32(),
                Name = reader.ReadString(),
                MapId = reader.ReadInt32(),
                WeatherSpawn = (WeatherTypes)reader.ReadInt32(),
                TimedSpawn = (TimeNames)reader.ReadInt32(),
                WaterSpawns = ReadStringList(reader),
                WeatherSpawns = ReadStringList(reader),
                TimedSpawns = ReadStringList(reader),
                CommonSpawns = ReadStringList(reader),
                UncommonSpawns = ReadStringList(reader),
                RareSpawns = ReadStringList(reader)
            };

            return tile;
        }

        #endregion

        #region Region Spawn Save/Load

        /// <summary>
        /// Save region spawns to binary file using BinaryWriter
        /// </summary>
        public static void SaveRegionSpawns()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.REGION_FILENAME);

                int count = WriteRegionSpawns(localPath);

                Logger.Info($"Region spawns saved to: {localPath} ({count} regions)");

                // Sync to server if linked
                var serverPath = PathConstants.ServerDataPath;

                if (serverPath != null)
                {
                    var serverFilePath = PathConstants.GetServerFilePath(PathConstants.REGION_FILENAME);

                    if (serverFilePath != null)
                    {
                        WriteRegionSpawns(serverFilePath);

                        Logger.Info($"Region spawns synced to server: {serverFilePath}");
                    }
                }

                // Sync to active approved pack if set
                SyncFileToActivePack(localPath, PathConstants.REGION_FILENAME);
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving region spawns", ex);
            }
        }

        private static int WriteRegionSpawns(string filePath)
        {
            using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));

            writer.Write(REGION_SPAWN_VERSION);
            writer.Write(Utility.Version);

            var mapsWithData = Utility.RegionSpawns.Where(kvp => kvp.Value.Count > 0).ToList();
            writer.Write(mapsWithData.Count);

            int totalRegions = 0;

            foreach (var mapEntry in mapsWithData)
            {
                writer.Write(mapEntry.Key);
                writer.Write(MapUtility.GetMapName(mapEntry.Key));
                writer.Write(mapEntry.Value.Count);

                foreach (var region in mapEntry.Value)
                {
                    WriteRegionSpawnEntity(writer, region);
                    totalRegions++;
                }
            }

            return totalRegions;
        }

        private static void WriteRegionSpawnEntity(BinaryWriter writer, RegionSpawnEntity region)
        {
            writer.Write(region.Id);
            writer.Write(region.Name ?? string.Empty);
            writer.Write(region.MapId);

            writer.Write((int)region.WeatherSpawn);
            writer.Write((int)region.TimedSpawn);

            WriteStringList(writer, region.WaterSpawns);
            WriteStringList(writer, region.WeatherSpawns);
            WriteStringList(writer, region.TimedSpawns);
            WriteStringList(writer, region.CommonSpawns);
            WriteStringList(writer, region.UncommonSpawns);
            WriteStringList(writer, region.RareSpawns);
        }

        /// <summary>
        /// Load region spawns from binary file using BinaryReader
        /// </summary>
        public static bool LoadRegionSpawns()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.REGION_FILENAME);

                if (!File.Exists(localPath))
                {
                    Logger.Warning($"Region spawns file not found: {localPath}");

                    return false;
                }

                int count = ReadRegionSpawns(localPath);

                Logger.Info($"Region spawns loaded from: {localPath} ({count} regions)");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading region spawns", ex);

                return false;
            }
        }

        private static int ReadRegionSpawns(string filePath)
        {
            using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));

            int version = reader.ReadInt32();
            string fileVersion = reader.ReadString();

            int mapCount = reader.ReadInt32();
            int totalRegions = 0;

            for (int m = 0; m < mapCount; m++)
            {
                int mapId = reader.ReadInt32();
                string mapName = reader.ReadString();
                int regionCount = reader.ReadInt32();

                if (!Utility.RegionSpawns.TryGetValue(mapId, out List<RegionSpawnEntity>? value))
                {
                    value = [];

                    Utility.RegionSpawns[mapId] = value;
                }

                for (int r = 0; r < regionCount; r++)
                {
                    var region = ReadRegionSpawnEntity(reader);
                    value.Add(region);
                    totalRegions++;
                }
            }

            return totalRegions;
        }

        private static RegionSpawnEntity ReadRegionSpawnEntity(BinaryReader reader)
        {
            var region = new RegionSpawnEntity
            {
                Id = reader.ReadInt32(),
                Name = reader.ReadString(),
                MapId = reader.ReadInt32(),
                WeatherSpawn = (WeatherTypes)reader.ReadInt32(),
                TimedSpawn = (TimeNames)reader.ReadInt32(),
                WaterSpawns = ReadStringList(reader),
                WeatherSpawns = ReadStringList(reader),
                TimedSpawns = ReadStringList(reader),
                CommonSpawns = ReadStringList(reader),
                UncommonSpawns = ReadStringList(reader),
                RareSpawns = ReadStringList(reader)
            };

            return region;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Write a list of strings to binary
        /// </summary>
        private static void WriteStringList(BinaryWriter writer, List<string> list)
        {
            writer.Write(list?.Count ?? 0);

            if (list != null)
            {
                foreach (var item in list)
                {
                    writer.Write(item ?? string.Empty);
                }
            }
        }

        /// <summary>
        /// Read a list of strings from binary
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

        #endregion
    }
}
