using System.Runtime.Serialization.Formatters.Binary;

using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.DTO.Models;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    /// <summary>
    /// Service for binary serialization of UORespawn v2.0 data files
    /// Generates files compatible with server-side UORespawnDataBase loader
    /// </summary>
    public static class BinarySerializationService
    {
        #region Save Methods

        /// <summary>
        /// Save SettingsModel to binary file(s)
        /// </summary>
        public static void SaveSettings(SettingsModel settings)
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.SETTINGS_FILENAME);
                SaveToBinaryFile(settings, localPath);
                Logger.Info($"Settings saved to: {localPath}");

                var serverPath = PathConstants.ServerDataPath;
                if (serverPath != null)
                {
                    var serverFilePath = PathConstants.GetServerFilePath(PathConstants.SETTINGS_FILENAME);
                    SaveToBinaryFile(settings, serverFilePath);
                    Logger.Info($"Settings synced to server: {serverFilePath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving settings", ex);
                throw;
            }
        }

        /// <summary>
        /// Save BoxContainer to binary file(s)
        /// </summary>
        public static void SaveBoxSpawns(BoxContainer container)
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.BOX_FILENAME);
                SaveToBinaryFile(container, localPath);
                Logger.Info($"Box spawns saved to: {localPath} ({GetBoxSpawnCount(container)} boxes)");

                var serverPath = PathConstants.ServerDataPath;
                if (serverPath != null)
                {
                    var serverFilePath = PathConstants.GetServerFilePath(PathConstants.BOX_FILENAME);
                    SaveToBinaryFile(container, serverFilePath);
                    Logger.Info($"Box spawns synced to server: {serverFilePath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving box spawns", ex);
                throw;
            }
        }

        /// <summary>
        /// Save TileContainer to binary file(s)
        /// </summary>
        public static void SaveTileSpawns(TileContainer container)
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.TILE_FILENAME);
                SaveToBinaryFile(container, localPath);
                Logger.Info($"Tile spawns saved to: {localPath} ({GetTileSpawnCount(container)} tiles)");

                var serverPath = PathConstants.ServerDataPath;
                if (serverPath != null)
                {
                    var serverFilePath = PathConstants.GetServerFilePath(PathConstants.TILE_FILENAME);
                    SaveToBinaryFile(container, serverFilePath);
                    Logger.Info($"Tile spawns synced to server: {serverFilePath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving tile spawns", ex);
                throw;
            }
        }

        /// <summary>
        /// Save RegionContainer to binary file(s)
        /// </summary>
        public static void SaveRegionSpawns(RegionContainer container)
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.REGION_FILENAME);
                SaveToBinaryFile(container, localPath);
                Logger.Info($"Region spawns saved to: {localPath} ({GetRegionSpawnCount(container)} regions)");

                var serverPath = PathConstants.ServerDataPath;
                if (serverPath != null)
                {
                    var serverFilePath = PathConstants.GetServerFilePath(PathConstants.REGION_FILENAME);
                    SaveToBinaryFile(container, serverFilePath);
                    Logger.Info($"Region spawns synced to server: {serverFilePath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving region spawns", ex);
                throw;
            }
        }

        /// <summary>
        /// Core binary serialization method
        /// </summary>
        private static void SaveToBinaryFile<T>(T data, string? filePath)
        {
            if (data == null || filePath == null)
            {
                Logger.Error($"Error saving : {data == null} || {filePath == null}");

                return;
            }

#pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete
            var formatter = new BinaryFormatter();
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            formatter.Serialize(stream, data);
#pragma warning restore SYSLIB0011
        }

        #endregion

        #region Load Methods

        /// <summary>
        /// Load SettingsModel from binary file
        /// </summary>
        public static SettingsModel? LoadSettings()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.SETTINGS_FILENAME);

                if (!File.Exists(localPath))
                {
                    Logger.Warning($"Settings file not found: {localPath}");
                    return null;
                }

                var settings = LoadFromBinaryFile<SettingsModel>(localPath);
                Logger.Info($"Settings loaded from: {localPath}");
                return settings;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading settings", ex);
                return null;
            }
        }

        /// <summary>
        /// Load BoxContainer from binary file
        /// </summary>
        public static BoxContainer? LoadBoxSpawns()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.BOX_FILENAME);

                if (!File.Exists(localPath))
                {
                    Logger.Warning($"Box spawns file not found: {localPath}");
                    return null;
                }

                var container = LoadFromBinaryFile<BoxContainer>(localPath);
                Logger.Info($"Box spawns loaded from: {localPath} ({GetBoxSpawnCount(container)} boxes)");
                return container;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading box spawns", ex);
                return null;
            }
        }

        /// <summary>
        /// Load TileContainer from binary file
        /// </summary>
        public static TileContainer? LoadTileSpawns()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.TILE_FILENAME);

                if (!File.Exists(localPath))
                {
                    Logger.Warning($"Tile spawns file not found: {localPath}");
                    return null;
                }

                var container = LoadFromBinaryFile<TileContainer>(localPath);
                Logger.Info($"Tile spawns loaded from: {localPath} ({GetTileSpawnCount(container)} tiles)");
                return container;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading tile spawns", ex);
                return null;
            }
        }

        /// <summary>
        /// Load RegionContainer from binary file
        /// </summary>
        public static RegionContainer? LoadRegionSpawns()
        {
            try
            {
                var localPath = PathConstants.GetLocalFilePath(PathConstants.REGION_FILENAME);

                if (!File.Exists(localPath))
                {
                    Logger.Warning($"Region spawns file not found: {localPath}");
                    return null;
                }

                var container = LoadFromBinaryFile<RegionContainer>(localPath);
                Logger.Info($"Region spawns loaded from: {localPath} ({GetRegionSpawnCount(container)} regions)");
                return container;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading region spawns", ex);
                return null;
            }
        }

        /// <summary>
        /// Core binary deserialization method
        /// </summary>
        private static T? LoadFromBinaryFile<T>(string filePath) where T : class
        {
#pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete
            var formatter = new BinaryFormatter();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return formatter.Deserialize(stream) as T;
#pragma warning restore SYSLIB0011
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get total box spawn count from container
        /// </summary>
        private static int GetBoxSpawnCount(BoxContainer? container)
        {
            if (container == null || container.BoxData == null)
                return 0;

            return container.BoxData.Sum(mapData => mapData.BoxSpawns?.Count ?? 0);
        }

        /// <summary>
        /// Get total tile spawn count from container
        /// </summary>
        private static int GetTileSpawnCount(TileContainer? container)
        {
            if (container == null || container.TileData == null)
                return 0;

            return container.TileData.Sum(mapData => mapData.TileSpawns?.Count ?? 0);
        }

        /// <summary>
        /// Get total region spawn count from container
        /// </summary>
        private static int GetRegionSpawnCount(RegionContainer? container)
        {
            if (container == null || container.RegionData == null)
                return 0;

            return container.RegionData.Sum(mapData => mapData.RegionSpawns?.Count ?? 0);
        }

        /// <summary>
        /// Check if binary files exist
        /// </summary>
        public static bool BinaryFilesExist()
        {
            return File.Exists(PathConstants.GetLocalFilePath(PathConstants.SETTINGS_FILENAME)) ||
                   File.Exists(PathConstants.GetLocalFilePath(PathConstants.BOX_FILENAME)) ||
                   File.Exists(PathConstants.GetLocalFilePath(PathConstants.TILE_FILENAME)) ||
                   File.Exists(PathConstants.GetLocalFilePath(PathConstants.REGION_FILENAME));
        }

        /// <summary>
        /// Get info about existing binary files
        /// </summary>
        public static Dictionary<string, bool> GetFileStatus()
        {
            return new Dictionary<string, bool>
            {
                ["Settings"] = File.Exists(PathConstants.GetLocalFilePath(PathConstants.SETTINGS_FILENAME)),
                ["BoxSpawns"] = File.Exists(PathConstants.GetLocalFilePath(PathConstants.BOX_FILENAME)),
                ["TileSpawns"] = File.Exists(PathConstants.GetLocalFilePath(PathConstants.TILE_FILENAME)),
                ["RegionSpawns"] = File.Exists(PathConstants.GetLocalFilePath(PathConstants.REGION_FILENAME))
            };
        }

        #endregion
    }
}
