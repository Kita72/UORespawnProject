using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    /// <summary>
    /// Service for loading application data in the background after UI initialization.
    /// This prevents blocking the UI thread during app startup.
    /// </summary>
    public class BackgroundDataLoader
    {
        private bool _isLoading = false;
        private bool _isComplete = false;

        // Loading state flags
        public bool IsBestiaryLoaded { get; private set; }
        public bool IsBoxSpawnDataLoaded { get; private set; }
        public bool IsTileSpawnDataLoaded { get; private set; }
        public bool IsRegionSpawnDataLoaded { get; private set; }
        public bool IsXMLSpawnerListLoaded { get; private set; }
        public bool IsMapFilesCopied { get; private set; }
        public bool IsDataWatcherStarted { get; private set; }

        // Overall loading state
        public bool IsLoading => _isLoading;
        public bool IsComplete => _isComplete;

        // Progress tracking
        public const int TotalSteps = 8;
        public int CompletedSteps { get; private set; }
        public double ProgressPercentage => (double)CompletedSteps / TotalSteps * 100;

        // Events for components to subscribe to
        public event EventHandler? BestiaryLoaded;
        public event EventHandler? BoxSpawnDataLoaded;
        public event EventHandler? TileSpawnDataLoaded;
        public event EventHandler? RegionSpawnDataLoaded;
        public event EventHandler? AllDataLoaded;

        private DataWatcher? _dataWatcher;

        /// <summary>
        /// Initializes the active pack path from the saved CurrentPackName setting.
        /// Called on startup to ensure edits sync back to the correct pack folder.
        /// </summary>
        private void InitializeActivePackPath()
        {
            try
            {
                var packName = Settings.CurrentPackName;
                if (string.IsNullOrEmpty(packName))
                {
                    Logger.Info("No current pack name set - active pack path not initialized");
                    return;
                }

                // Check approved packs first, then imported
                var approvedPath = Path.Combine(PathConstants.PacksApprovedPath, packName);
                var importedPath = Path.Combine(PathConstants.PacksImportedPath, packName);

                string? packFolder = null;
                if (Directory.Exists(approvedPath))
                {
                    packFolder = approvedPath;
                }
                else if (Directory.Exists(importedPath))
                {
                    packFolder = importedPath;
                }

                if (packFolder == null)
                {
                    Logger.Warning($"Pack folder not found for '{packName}' - active pack path not set");
                    return;
                }

                // Resolve the actual data path (might be in UOR_DATA subfolder)
                var dataPath = ResolvePackDataPath(packFolder);
                if (dataPath != null)
                {
                    PathConstants.ActivePackDataPath = dataPath;
                    Logger.Info($"Active pack path initialized: {dataPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing active pack path", ex);
            }
        }

        /// <summary>
        /// Resolves the data path within a pack folder (handles UOR_DATA subfolder).
        /// </summary>
        private static string? ResolvePackDataPath(string packFolder)
        {
            string[] dataFiles = [PathConstants.SETTINGS_FILENAME, PathConstants.BOX_FILENAME, 
                                  PathConstants.TILE_FILENAME, PathConstants.REGION_FILENAME];

            if (dataFiles.Any(file => File.Exists(Path.Combine(packFolder, file))))
            {
                return packFolder;
            }

            var nestedPath = Path.Combine(packFolder, PathConstants.UOR_DATA_SUBFOLDER);
            if (Directory.Exists(nestedPath) && dataFiles.Any(file => File.Exists(Path.Combine(nestedPath, file))))
            {
                return nestedPath;
            }

            return packFolder; // Return pack folder even if no files found yet
        }

        /// <summary>
        /// Loads all application data asynchronously in the background.
        /// Should be called after the UI has rendered.
        /// </summary>
        public async Task LoadAllDataAsync()
        {
            if (_isLoading || _isComplete)
            {
                Logger.Info("Background data loading already in progress or complete");
                return;
            }

            _isLoading = true;
            CompletedSteps = 0;
            Logger.Info("Starting background data loading...");

            var startTime = DateTime.Now;

            try
            {
                // Load data in logical order (some dependencies exist)

                // Step 0: Load Settings (FIRST - other systems may depend on settings)
                await LoadSettingsAsync();

                // Initialize active pack path from saved CurrentPackName setting
                InitializeActivePackPath();

                // Step 1: Load Box Spawn Data (Binary)
                await LoadBoxSpawnDataAsync();

                // Step 2: Load Tile Spawn Data (Binary)
                await LoadTileSpawnDataAsync();

                // Step 3: Load Region Spawn Data (Binary)
                await LoadRegionSpawnDataAsync();

                // Step 4: Load Bestiary (creature list from server-generated text file)
                await LoadBestiaryAsync();

                // Step 5: Load XML Spawner List
                await LoadXMLSpawnerListAsync();

                // Step 6: Verify Map Files exist in Data/MAPS
                await CopyMapFilesAsync();

                // Step 7: Start DataWatcher (LAST - after all data is loaded)
                await StartDataWatcherAsync();

                _isComplete = true;
                var elapsed = DateTime.Now - startTime;
                Logger.Info($"Background data loading completed in {elapsed.TotalSeconds:F2} seconds");
                
                AllDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("Error during background data loading", ex);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadSettingsAsync()
        {
            Logger.Info("[Startup Step 0/7] Loading settings...");
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Utility.LoadSettings();
                        Logger.Info("[Startup Step 0/7] Settings loaded from binary file (or defaults if file missing)");

                        // Always save settings to ensure binary file exists (creates file if missing)
                        Utility.SaveSettings();
                        Logger.Info($"[Startup Step 0/7] Current pack: {Settings.CurrentPackName}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 0/7] LoadSettings failed - using Preferences defaults", ex);
                    }
                });

                CompletedSteps++;
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 0/7] Error loading settings", ex);
            }
        }

        private async Task LoadBoxSpawnDataAsync()
        {
            Logger.Info("[Startup Step 1/7] Loading box spawn data...");
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Utility.LoadSpawnData();
                        var totalSpawns = Utility.BoxSpawns.Values.Sum(list => list.Count);
                        Logger.Info($"[Startup Step 1/7] Loaded {totalSpawns} spawn boxes across all maps");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 1/7] LoadSpawnData failed", ex);
                    }
                });

                IsBoxSpawnDataLoaded = true;
                CompletedSteps++;
                BoxSpawnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 1/7] Error loading spawn data", ex);
            }
        }

        private async Task LoadTileSpawnDataAsync()
        {
            Logger.Info("[Startup Step 2/7] Loading tile spawn data...");
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Utility.LoadTileSpawnData();
                        var totalTileSpawns = Utility.TileSpawns.Values.Sum(list => list.Count);
                        Logger.Info($"[Startup Step 2/7] Loaded {totalTileSpawns} tile spawn configurations across all maps");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 2/7] LoadTileSpawnData failed", ex);
                    }
                });

                IsTileSpawnDataLoaded = true;
                CompletedSteps++;
                TileSpawnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 2/7] Error loading tile spawn data", ex);
            }
        }

        private async Task LoadRegionSpawnDataAsync()
        {
            Logger.Info("[Startup Step 3/7] Loading region spawn data...");
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Utility.LoadRegionSpawnData();
                        var totalRegionSpawns = Utility.RegionSpawns.Values.Sum(list => list.Count);
                        Logger.Info($"[Startup Step 3/7] Loaded {totalRegionSpawns} region spawn configurations across all maps");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 3/7] LoadRegionSpawnData failed", ex);
                    }
                });

                IsRegionSpawnDataLoaded = true;
                CompletedSteps++;
                RegionSpawnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 3/7] Error loading region spawn data", ex);
            }
        }

        private async Task LoadBestiaryAsync()
        {
            Logger.Info("[Startup Step 4/7] Loading bestiary...");
            try
            {
                await BestiarySpawnUtility.LoadSpawnList();

                IsBestiaryLoaded = true;
                CompletedSteps++;
                BestiaryLoaded?.Invoke(this, EventArgs.Empty);
                Logger.Info($"[Startup Step 4/7] Loaded {BestiarySpawnUtility.BestiaryNameList?.Count ?? 0} creatures in bestiary");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 4/7] LoadSpawnList (bestiary) failed", ex);
            }
        }

        private async Task LoadXMLSpawnerListAsync()
        {
            Logger.Info("[Startup Step 5/7] Loading XML spawner list...");
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        XMLSpawnUtility.LoadSpawnerList();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 5/7] LoadSpawnerList failed", ex);
                    }
                });

                IsXMLSpawnerListLoaded = true;
                CompletedSteps++;
                Logger.Info($"[Startup Step 5/7] XML spawner list loaded");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 5/7] Error loading XML spawner list", ex);
            }
        }

        private async Task CopyMapFilesAsync()
        {
            Logger.Info("[Startup Step 6/7] Checking map files...");
            try
            {
                await Task.Run(() =>
                {
                    // Maps are now included in the build at Data/MAPS/ directly
                    // Just ensure the folder exists for user-added custom maps
                    var mapsPath = PathConstants.MapsPath;
                    if (!Directory.Exists(mapsPath))
                    {
                        Directory.CreateDirectory(mapsPath);
                    }

                    // Log available maps
                    var mapFiles = Directory.GetFiles(mapsPath, "Map*.bmp");
                    if (mapFiles.Length > 0)
                    {
                        Logger.Info($"[Startup Step 6/7] Found {mapFiles.Length} map files in Data/MAPS");
                    }
                    else
                    {
                        Logger.Warning("[Startup Step 6/7] No map files found in Data/MAPS - maps may not display correctly");
                    }
                });

                IsMapFilesCopied = true;
                CompletedSteps++;
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 6/7] Error checking map files", ex);
            }
        }

        private async Task StartDataWatcherAsync()
        {
            Logger.Info("[Startup Step 7/7] Starting DataWatcher...");
            try
            {
                // DataWatcher starts LAST to avoid false change events during initial loading
                await Task.Run(() =>
                {
                    try
                    {
                        _dataWatcher = new DataWatcher(() =>
                        {
                            Logger.Info("[DataWatcher] Server data files have been updated - reloading...");
                            // Trigger reload of affected data
                            _ = ReloadDataAsync();
                        });

                        Logger.Info("[Startup Step 7/7] DataWatcher started successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"[Startup Step 7/7] DataWatcher failed to start: {ex.Message}");
                    }
                });

                IsDataWatcherStarted = true;
                CompletedSteps++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error starting DataWatcher", ex);
            }
        }

        /// <summary>
        /// Reloads data when server files change (triggered by DataWatcher)
        /// </summary>
        private static async Task ReloadDataAsync()
        {
            try
            {
                Logger.Info("Reloading spawn data due to server file changes...");
                
                // Reload spawn data from CSV files
                await Task.Run(() => Utility.LoadSpawnData());
                
                Logger.Info("Spawn data reloaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading data", ex);
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            _dataWatcher?.Dispose();
        }
    }
}
