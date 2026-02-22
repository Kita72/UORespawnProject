using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Services;

namespace UORespawnApp.Scripts.Utilities
{

    /// <summary>
    /// Core utility class for UORespawn v2.0
    /// Manages spawn data (Box, Tile, Region) and settings with binary serialization
    /// </summary>
    public static class Utility
    {
        internal const string Version = "2.0.0.6";

        /// <summary>
        /// Current session (map, view state, etc.)
        /// </summary>
        internal static Session? SESSION { get; private set; }

        /// <summary>
        /// Initialize a new session
        /// </summary>
        internal static void StartSession(Session session)
        {
            SESSION = session;
        }

        internal static void InitializeSpawnDictionary()
        {
            InitializeBoxSpawns(); 
            InitializeTileSpawns(); 
            InitializeRegionSpawns();
            InitializeVendorSpawns();
        }

        #region Box Spawn Data

        /// <summary>
        /// Box spawn data indexed by MapId
        /// Each map contains a list of BoxSpawnEntity objects
        /// </summary>
        internal static Dictionary<int, List<BoxSpawnEntity>> BoxSpawns { get; private set; } = [];

        /// <summary>
        /// Initialize Box Spawns dictionary with empty lists for each map (0-5)
        /// </summary>
        internal static void InitializeBoxSpawns()
        {
            for (int i = 0; i <= 5; i++)
            {
                BoxSpawns[i] = [];
            }
        }

        internal static void AddBoxSpawn(int map, BoxSpawnEntity entity)
        {
            if (!BoxSpawns.TryGetValue(map, out List<BoxSpawnEntity>? value))
            {
                value = [];

                BoxSpawns[map] = value;
            }

            if (!value.Contains(entity))
            {
                value.Add(entity);
            }
        }

        /// <summary>
        /// Save box spawn data using binary serialization (ServUO-style BinaryWriter)
        /// </summary>
        internal static void SaveSpawnData()
        {
            try
            {
                BinarySerializationService.SaveBoxSpawns();
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving box spawn data", ex);
            }
        }

        /// <summary>
        /// Load box spawn data using binary deserialization (ServUO-style BinaryReader)
        /// </summary>
        internal static void LoadBoxSpawnData()
        {
            try
            {
                BoxSpawns.Clear();

                InitializeBoxSpawns();

                BinarySerializationService.LoadBoxSpawns();
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading box spawn data", ex);
            }
        }

        #endregion

        #region Tile Spawn Data

        /// <summary>
        /// Tile spawn data indexed by MapId
        /// Each map contains a list of TileSpawnEntity objects
        /// </summary>
        internal static Dictionary<int, List<TileSpawnEntity>> TileSpawns { get; private set; } = [];

        /// <summary>
        /// Initialize Tile Spawns dictionary with empty lists for each map (0-5)
        /// </summary>
        internal static void InitializeTileSpawns()
        {
            for (int i = 0; i <= 5; i++)
            {
                TileSpawns[i] = [];
            }
        }

        /// <summary>
        /// Save tile spawn data using binary serialization (ServUO-style BinaryWriter)
        /// </summary>
        internal static void SaveTileSpawnData()
        {
            try
            {
                BinarySerializationService.SaveTileSpawns();
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving tile spawn data", ex);
            }
        }

        /// <summary>
        /// Load tile spawn data using binary deserialization (ServUO-style BinaryReader)
        /// </summary>
        internal static void LoadTileSpawnData()
        {
            try
            {
                TileSpawns.Clear();

                InitializeTileSpawns();

                BinarySerializationService.LoadTileSpawns();
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading tile spawn data", ex);
            }
        }

        #endregion

        #region Region Spawn Data

        /// <summary>
        /// Region spawn data indexed by MapId
        /// Each map contains a list of RegionSpawnEntity objects
        /// </summary>
        internal static Dictionary<int, List<RegionSpawnEntity>> RegionSpawns { get; private set; } = [];

        /// <summary>
        /// Initialize Region Spawns dictionary with empty lists for each map (0-5)
        /// </summary>
        internal static void InitializeRegionSpawns()
        {
            for (int i = 0; i <= 5; i++)
            {
                RegionSpawns[i] = [];
            }
        }

        /// <summary>
        /// Save region spawn data using binary serialization (ServUO-style BinaryWriter)
        /// </summary>
        internal static void SaveRegionSpawnData()
        {
            try
            {
                BinarySerializationService.SaveRegionSpawns();
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving region spawn data", ex);
            }
        }

        /// <summary>
        /// Load region spawn data using binary deserialization (ServUO-style BinaryReader)
        /// </summary>
        internal static void LoadRegionSpawnData()
        {
            try
            {
                RegionSpawns.Clear();

                InitializeRegionSpawns();

                BinarySerializationService.LoadRegionSpawns();
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading region spawn data", ex);
            }
        }

        #endregion

        #region Vendor Spawn Data

        /// <summary>
        /// Vendor spawn data indexed by MapId
        /// Each map contains a list of VendorEntity objects
        /// </summary>
        internal static Dictionary<int, List<VendorEntity>> VendorSpawns { get; private set; } = [];

        /// <summary>
        /// Initialize Vendor Spawns dictionary with empty lists for each map (0-5)
        /// </summary>
        internal static void InitializeVendorSpawns()
        {
            for (int i = 0; i <= 5; i++)
            {
                VendorSpawns[i] = [];
            }
        }

        /// <summary>
        /// Save vendor spawn data using binary serialization (ServUO-style BinaryWriter)
        /// </summary>
        internal static void SaveVendorSpawnData()
        {
            try
            {
                BinarySerializationService.SaveVendorSpawns();
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving vendor spawn data", ex);
            }
        }

        /// <summary>
        /// Load vendor spawn data using binary deserialization (ServUO-style BinaryReader)
        /// </summary>
        internal static void LoadVendorSpawnData()
        {
            try
            {
                VendorSpawns.Clear();

                InitializeVendorSpawns();

                BinarySerializationService.LoadVendorSpawns();
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading vendor spawn data", ex);
            }
        }

        #endregion

        #region Settings Data

        /// <summary>
        /// Save settings to binary file (UOR_SpawnSettings.bin) using ServUO-style BinaryWriter
        /// Reads directly from Settings.cs properties
        /// </summary>
        /// <remarks>
        /// Two-tier settings model:
        /// - Preferences: UI immediate access (Preferences API)
        /// - Binary file: Server-readable format (BinaryWriter)
        /// Binary file is synced to server; server reads it (doesn't modify)
        /// </remarks>
        internal static void SaveSettings()
        {
            try
            {
                BinarySerializationService.SaveSettings();
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving settings", ex);
            }
        }

        /// <summary>
        /// Load settings from binary file (UOR_SpawnSettings.bin) using ServUO-style BinaryReader
        /// Writes directly to Settings.cs Preferences
        /// If binary file doesn't exist, Settings use their default Preferences values
        /// </summary>
        /// <remarks>
        /// Called during app startup by BackgroundDataLoader.LoadSettingsAsync()
        /// Loads from local Data/UOR_DATA/UOR_SpawnSettings.bin
        /// Binary file is editor-created; server reads it (not modified by server)
        /// </remarks>
        internal static void LoadSettings()
        {
            try
            {
                BinarySerializationService.LoadSettings();
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading settings", ex);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets map image path for current session map
        /// Returns base64 data URL for MAUI Blazor WebView to display from Data/maps/
        /// Falls back to empty string if map doesn't exist
        /// </summary>
        internal static string GetMapImagePath()
        {
            if (SESSION != null && MapUtility.IsValidMapId(SESSION.Current_Map))
            {
                try
                {
                    var fullPath = MapUtility.GetMapImagePath(SESSION.Current_Map);

                    if (File.Exists(fullPath))
                    {
                        var bytes = File.ReadAllBytes(fullPath);
                        var base64 = Convert.ToBase64String(bytes);

                        return $"data:image/bmp;base64,{base64}";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error loading map image for Map{SESSION.Current_Map}", ex);
                }
            }

            return "";
        }

        #endregion
    }
}
