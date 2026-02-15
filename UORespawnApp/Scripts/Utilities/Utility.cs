using UORespawnApp.Scripts.DTO.Models;
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
        internal const string Version = "2.0.0.1";

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
        }

        #region Box Spawn Data

        /// <summary>
        /// Box spawn data indexed by MapId
        /// Each map contains a list of BoxSpawnEntity objects
        /// </summary>
        internal static Dictionary<int, List<BoxSpawnEntity>> BoxSpawns { get; private set; } = new();

        /// <summary>
        /// Initialize Box Spawns dictionary with empty lists for each map (0-5)
        /// </summary>
        internal static void InitializeBoxSpawns()
        {
            for (int i = 0; i <= 5; i++)
            {
                BoxSpawns[i] = new List<BoxSpawnEntity>();
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
        /// Save box spawn data using binary serialization
        /// Creates BoxContainer DTO and saves to binary file(s)
        /// </summary>
        internal static void SaveSpawnData()
        {
            try
            {
                var container = new BoxContainer
                {
                    Version = Version,
                    BoxData = new List<MapBoxData>()
                };

                foreach (var mapEntry in BoxSpawns)
                {
                    if (mapEntry.Value.Count > 0)
                    {
                        var mapBoxData = new MapBoxData
                        {
                            MapId = mapEntry.Key,
                            MapName = MapUtility.GetMapName(mapEntry.Key),
                            BoxSpawns = new List<BoxModel>(mapEntry.Value.Select(e => e.ToBoxModel()))
                        };
                        container.BoxData.Add(mapBoxData);
                    }
                }

                // Use BinarySerializationService to save box spawns
                BinarySerializationService.SaveBoxSpawns(container);
                var totalSpawns = container.BoxData.Sum(m => m.BoxSpawns?.Count ?? 0);
                Logger.Info($"Saved box spawn data: {totalSpawns} total spawns across {BoxSpawns.Count} maps");
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving box spawn data", ex);
            }
        }

        /// <summary>
        /// Load box spawn data using binary deserialization
        /// </summary>
        internal static void LoadSpawnData()
        {
            try
            {
                BoxSpawns.Clear();

                var container = BinarySerializationService.LoadBoxSpawns();
                if (container != null && container.BoxData != null)
                {
                    foreach (var mapData in container.BoxData)
                    {
                        if (!BoxSpawns.ContainsKey(mapData.MapId))
                        {
                            BoxSpawns[mapData.MapId] = new List<BoxSpawnEntity>();
                        }

                        if (mapData.BoxSpawns != null)
                        {
                            foreach (var boxModel in mapData.BoxSpawns)
                            {
                                var entity = BoxSpawnEntity.FromBoxModel(boxModel, mapData.MapId);
                                BoxSpawns[mapData.MapId].Add(entity);
                            }
                        }
                    }
                }
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
        internal static Dictionary<int, List<TileSpawnEntity>> TileSpawns { get; private set; } = new();

        /// <summary>
        /// Initialize Tile Spawns dictionary with empty lists for each map (0-5)
        /// </summary>
        internal static void InitializeTileSpawns()
        {
            for (int i = 0; i <= 5; i++)
            {
                TileSpawns[i] = new List<TileSpawnEntity>();
            }
        }

        /// <summary>
        /// Save tile spawn data using binary serialization
        /// </summary>
        internal static void SaveTileSpawnData()
        {
            try
            {
                var container = new TileContainer
                {
                    Version = Version,
                    TileData = new List<MapTileData>()
                };

                foreach (var mapEntry in TileSpawns)
                {
                    if (mapEntry.Value.Count > 0)
                    {
                        var mapTileData = new MapTileData
                        {
                            MapId = mapEntry.Key,
                            MapName = MapUtility.GetMapName(mapEntry.Key),
                            TileSpawns = new List<TileModel>(mapEntry.Value.Select(e => e.ToTileModel()))
                        };
                        container.TileData.Add(mapTileData);
                    }
                }

                // Use BinarySerializationService to save tile spawns
                BinarySerializationService.SaveTileSpawns(container);
                var totalTileSpawns = container.TileData.Sum(m => m.TileSpawns?.Count ?? 0);
                Logger.Info($"Saved tile spawn data: {totalTileSpawns} total tile spawns across {TileSpawns.Count} maps");
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving tile spawn data", ex);
            }
        }

        /// <summary>
        /// Load tile spawn data using binary deserialization
        /// </summary>
        internal static void LoadTileSpawnData()
        {
            try
            {
                TileSpawns.Clear();

                var container = BinarySerializationService.LoadTileSpawns();
                if (container != null && container.TileData != null)
                {
                    foreach (var mapData in container.TileData)
                    {
                        if (!TileSpawns.ContainsKey(mapData.MapId))
                        {
                            TileSpawns[mapData.MapId] = new List<TileSpawnEntity>();
                        }

                        if (mapData.TileSpawns != null)
                        {
                            foreach (var tileModel in mapData.TileSpawns)
                            {
                                var entity = TileSpawnEntity.FromTileModel(tileModel, mapData.MapId);
                                TileSpawns[mapData.MapId].Add(entity);
                            }
                        }
                    }
                }
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
        internal static Dictionary<int, List<RegionSpawnEntity>> RegionSpawns { get; private set; } = new();

        /// <summary>
        /// Initialize Region Spawns dictionary with empty lists for each map (0-5)
        /// </summary>
        internal static void InitializeRegionSpawns()
        {
            for (int i = 0; i <= 5; i++)
            {
                RegionSpawns[i] = new List<RegionSpawnEntity>();
            }
        }

        /// <summary>
        /// Save region spawn data using binary serialization
        /// </summary>
        internal static void SaveRegionSpawnData()
        {
            try
            {
                var container = new RegionContainer
                {
                    Version = Version,
                    RegionData = new List<MapRegionData>()
                };

                foreach (var mapEntry in RegionSpawns)
                {
                    if (mapEntry.Value.Count > 0)
                    {
                        var mapRegionData = new MapRegionData
                        {
                            MapId = mapEntry.Key,
                            MapName = MapUtility.GetMapName(mapEntry.Key),
                            RegionSpawns = new List<RegionModel>(mapEntry.Value.Select(e => e.ToRegionModel()))
                        };
                        container.RegionData.Add(mapRegionData);
                    }
                }

                // Use BinarySerializationService to save region spawns
                BinarySerializationService.SaveRegionSpawns(container);
                var totalRegionSpawns = container.RegionData.Sum(m => m.RegionSpawns?.Count ?? 0);
                Logger.Info($"Saved region spawn data: {totalRegionSpawns} total region spawns across {RegionSpawns.Count} maps");
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving region spawn data", ex);
            }
        }

        /// <summary>
        /// Load region spawn data using binary deserialization
        /// </summary>
        internal static void LoadRegionSpawnData()
        {
            try
            {
                RegionSpawns.Clear();

                var container = BinarySerializationService.LoadRegionSpawns();
                if (container != null && container.RegionData != null)
                {
                    foreach (var mapData in container.RegionData)
                    {
                        if (!RegionSpawns.ContainsKey(mapData.MapId))
                        {
                            RegionSpawns[mapData.MapId] = new List<RegionSpawnEntity>();
                        }

                        if (mapData.RegionSpawns != null)
                        {
                            foreach (var regionModel in mapData.RegionSpawns)
                            {
                                var entity = RegionSpawnEntity.FromRegionModel(regionModel, mapData.MapId);
                                RegionSpawns[mapData.MapId].Add(entity);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading region spawn data", ex);
            }
        }

        #endregion

        #region Settings Data

        /// <summary>
        /// Save settings to binary file (UOR_SpawnSettings.bin)
        /// Maps Settings.cs Preferences properties to SettingsModel DTO
        /// </summary>
        /// <remarks>
        /// Two-tier settings model:
        /// - Preferences: UI immediate access (Preferences API)
        /// - Binary file: Server-readable format (BinaryFormatter)
        /// Binary file is synced to server; server reads it (doesn't modify)
        /// </remarks>
        internal static void SaveSettings()
        {
            try
            {
                var model = new SettingsModel
                {
                    Version = Version,
                    MaxMobs = Settings.MaxMobs,
                    MinRange = Settings.MinRange,
                    MaxRange = Settings.MaxRange,
                    MaxCrowd = Settings.MaxCrowd,
                    ChanceWater = Settings.WaterChance,
                    ChanceWeather = Settings.WeatherChance,
                    ChanceTimed = Settings.TimedChance,
                    ChanceCommon = Settings.CommonChance,
                    ChanceUncommon = Settings.UnCommonChance,
                    ChanceRare = Settings.RareChance,
                    ScaleSpawn = Settings.IsScaleSpawn,
                    EnableRiftSpawn = Settings.EnableRiftSpawn,
                    EnableDebug = Settings.EnableDebugSpawn
                };

                BinarySerializationService.SaveSettings(model);
                Logger.Info("Settings saved using binary serialization");
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving settings", ex);
            }
        }

        /// <summary>
        /// Load settings from binary file (UOR_SpawnSettings.bin)
        /// Maps SettingsModel DTO properties to Settings.cs Preferences
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
                var model = BinarySerializationService.LoadSettings();
                if (model != null)
                {
                    Settings.MaxMobs = model.MaxMobs;
                    Settings.MinRange = model.MinRange;
                    Settings.MaxRange = model.MaxRange;
                    Settings.MaxCrowd = model.MaxCrowd;
                    Settings.WaterChance = model.ChanceWater;
                    Settings.WeatherChance = model.ChanceWeather;
                    Settings.TimedChance = model.ChanceTimed;
                    Settings.CommonChance = model.ChanceCommon;
                    Settings.UnCommonChance = model.ChanceUncommon;
                    Settings.RareChance = model.ChanceRare;
                    Settings.IsScaleSpawn = model.ScaleSpawn;
                    Settings.EnableRiftSpawn = model.EnableRiftSpawn;
                    Settings.EnableDebugSpawn = model.EnableDebug;
                    Logger.Info("Settings loaded from binary file");
                }
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
