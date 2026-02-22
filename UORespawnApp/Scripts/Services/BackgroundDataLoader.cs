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
        public bool IsVendorListLoaded { get; private set; }
        public bool IsSignDataLoaded { get; private set; }
        public bool IsHiveDataLoaded { get; private set; }
        public bool IsBoxSpawnDataLoaded { get; private set; }
        public bool IsTileSpawnDataLoaded { get; private set; }
        public bool IsRegionSpawnDataLoaded { get; private set; }
        public bool IsVendorSpawnDataLoaded { get; private set; }
        public bool IsXMLSpawnerListLoaded { get; private set; }
        public bool IsMapFilesCopied { get; private set; }
        public bool IsDataWatcherStarted { get; private set; }

        // Overall loading state
        public bool IsLoading => _isLoading;
        public bool IsComplete => _isComplete;

        // Progress tracking
        public const int TotalSteps = 13;
        public int CompletedSteps { get; private set; }
        public double ProgressPercentage => (double)CompletedSteps / TotalSteps * 100;

        // Events for components to subscribe to
        public event EventHandler? BestiaryLoaded;
        public event EventHandler? VendorListLoaded;
        public event EventHandler? BoxSpawnDataLoaded;
        public event EventHandler? TileSpawnDataLoaded;
        public event EventHandler? RegionSpawnDataLoaded;
        public event EventHandler? VendorSpawnDataLoaded;
        public event EventHandler? AllDataLoaded;

        private DataWatcher? _dataWatcher;

        /// <summary>
        /// Initializes the ActivePackDataPath from Settings.CurrentPackFolder.
        /// Called on startup to ensure edits sync back to the correct pack folder.
        /// On first launch (no data files in LocalDataPath), applies the default pack.
        /// </summary>
        private static void InitializeActivePackPath()
        {
            try
            {
                var packFolder = Settings.CurrentPackFolder;

                // If no pack folder is set, check if we have a legacy CurrentPackName (folder name only)
                // This handles migration from old settings format
                if (string.IsNullOrEmpty(packFolder))
                {
                    var legacyPackName = Settings.CurrentPackName;
                    if (!string.IsNullOrEmpty(legacyPackName) && legacyPackName != "DefaultPack")
                    {
                        // Try to find the pack folder by name in all pack directories
                        var approvedPath = Path.Combine(PathConstants.PacksApprovedPath, legacyPackName);
                        var createdPath = Path.Combine(PathConstants.PacksCreatedPath, legacyPackName);
                        var importedPath = Path.Combine(PathConstants.PacksImportedPath, legacyPackName);

                        if (Directory.Exists(approvedPath))
                            packFolder = approvedPath;
                        else if (Directory.Exists(createdPath))
                            packFolder = createdPath;
                        else if (Directory.Exists(importedPath))
                            packFolder = importedPath;
                    }
                }

                if (string.IsNullOrEmpty(packFolder) || !Directory.Exists(packFolder))
                {
                    Logger.Info("No valid pack folder set - active pack path not initialized");
                    return;
                }

                // Resolve the actual data path (might be in UOR_DATA subfolder)
                var dataPath = ResolvePackDataPath(packFolder);

                if (dataPath != null)
                {
                    PathConstants.ActivePackDataPath = dataPath;

                    Logger.Info($"Active pack path initialized: {dataPath}");
                }

                // Sync pack data files to LocalDataPath
                // - First launch: Copies all pack data files
                // - Subsequent launches: Copies any MISSING files (handles upgrades)
                SyncPackDataToLocalPath(packFolder, dataPath);
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing active pack path", ex);
            }
        }

        /// <summary>
        /// Ensures all pack data files exist in LocalDataPath (Data/UORespawn/).
        /// - First launch: Copies all pack data files to LocalDataPath
        /// - Subsequent launches: Copies any MISSING files from pack to LocalDataPath
        /// This handles upgrades where new file types are added (e.g., VendorSpawn).
        /// </summary>
        private static void SyncPackDataToLocalPath(string packFolder, string? packDataPath)
        {
            try
            {
                var localDataPath = PathConstants.LocalDataPath;
                var sourcePath = packDataPath ?? packFolder;

                // ALL data files that should exist in LocalDataPath
                string[] allDataFiles = [PathConstants.SETTINGS_FILENAME, PathConstants.BOX_FILENAME,
                                         PathConstants.TILE_FILENAME, PathConstants.REGION_FILENAME, PathConstants.VENDOR_FILENAME];

                // Check which files are missing from LocalDataPath
                var missingFiles = allDataFiles
                    .Where(fileName => !File.Exists(Path.Combine(localDataPath, fileName)))
                    .ToList();

                if (missingFiles.Count == 0)
                {
                    Logger.Info("LocalDataPath has all pack data files - no sync needed");
                    return;
                }

                // Log what we're doing
                bool isFirstLaunch = missingFiles.Count == allDataFiles.Length || 
                                     !allDataFiles.Take(4).Any(f => File.Exists(Path.Combine(localDataPath, f))); // No spawn files = first launch

                if (isFirstLaunch)
                {
                    Logger.Info($"First launch detected - applying pack data from: {sourcePath}");
                }
                else
                {
                    Logger.Info($"Syncing {missingFiles.Count} missing file(s) from pack: {sourcePath}");
                }

                // Copy missing files from pack to LocalDataPath
                foreach (var fileName in missingFiles)
                {
                    var sourceFile = Path.Combine(sourcePath, fileName);

                    if (File.Exists(sourceFile))
                    {
                        var destFile = Path.Combine(localDataPath, fileName);
                        File.Copy(sourceFile, destFile, overwrite: true);

                        Logger.Info($"  Copied: {fileName}");
                    }
                    else
                    {
                        // Only warn for spawn files, not settings (settings might be intentionally absent in pack)
                        if (fileName != PathConstants.SETTINGS_FILENAME)
                        {
                            Logger.Warning($"  Missing in pack: {fileName}");
                        }
                    }
                }

                Logger.Info("Pack data sync completed");
            }
            catch (Exception ex)
            {
                Logger.Error("Error syncing pack data to LocalDataPath", ex);
            }
        }

        /// <summary>
        /// Resolves the data path within a pack folder (handles UOR_DATA subfolder).
        /// </summary>
        private static string? ResolvePackDataPath(string packFolder)
        {
            string[] dataFiles = [PathConstants.SETTINGS_FILENAME, PathConstants.BOX_FILENAME,
                                  PathConstants.TILE_FILENAME, PathConstants.REGION_FILENAME, PathConstants.VENDOR_FILENAME];

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

                // Step 0.5: Ensure approved packs are unpacked from Backup folder
                // This MUST happen BEFORE InitializeActivePackPath so the pack exists in Approved
                await BackgroundDataLoader.EnsureApprovedPacksUnpackedAsync();

                // Initialize active pack path from saved CurrentPackName setting
                // Now the pack will exist in Approved (either from Backup ZIP or folder)
                BackgroundDataLoader.InitializeActivePackPath();

                // Step 1: Load Box Spawn Data (Binary)
                await LoadBoxSpawnDataAsync();

                // Step 2: Load Tile Spawn Data (Binary)
                await LoadTileSpawnDataAsync();

                // Step 3: Load Region Spawn Data (Binary)
                await LoadRegionSpawnDataAsync();

                // Step 4: Load Vendor Spawn Data (Binary)
                await LoadVendorSpawnDataAsync();

                // Step 5: Load Bestiary (creature list from server-generated text file)
                await LoadBestiaryAsync();

                // Step 6: Load Vendor List (vendor names from server-generated text file)
                await LoadVendorListAsync();

                // Step 7: Load Sign Data (shop sign locations for vendor spawning)
                await LoadSignDataAsync();

                // Step 8: Load Hive Data (bee hive locations for beekeeper spawning)
                await LoadHiveDataAsync();

                // Step 9: Load XML Spawner List
                await LoadXMLSpawnerListAsync();

                // Step 10: Sync spawn packs with server data (remove invalid creatures, regions, locations)
                await SyncSpawnPacksWithServerDataAsync();

                // Step 11: Verify Map Files exist in Data/MAPS
                await CopyMapFilesAsync();

                // Step 12: Start DataWatcher (LAST - after all data is loaded)
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

        /// <summary>
        /// Ensures approved packs are unpacked from Backup folder to Approved folder.
        /// Must be called BEFORE InitializeActivePackPath so the packs exist.
        /// 
        /// Flow:
        ///   Backup/DefaultPack.zip  → Approved/DefaultPack/ (for releases)
        ///   Backup/DefaultPack/     → Approved/DefaultPack/ (for Git repo/dev)
        /// </summary>
        private static async Task EnsureApprovedPacksUnpackedAsync()
        {
            Logger.Info("[Startup] Ensuring approved packs are unpacked from Backup...");

            try
            {
                await Task.Run(() =>
                {
                    var packService = new SpawnPackService();

                    packService.UnpackApprovedPacks();
                });

                Logger.Info("[Startup] Approved packs unpacked successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup] Error unpacking approved packs", ex);
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
                        Utility.LoadBoxSpawnData();

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
            Logger.Info("[Startup Step 3/12] Loading region spawn data...");

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Utility.LoadRegionSpawnData();

                        var totalRegionSpawns = Utility.RegionSpawns.Values.Sum(list => list.Count);

                        Logger.Info($"[Startup Step 3/12] Loaded {totalRegionSpawns} region spawn configurations across all maps");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 3/12] LoadRegionSpawnData failed", ex);
                    }
                });

                IsRegionSpawnDataLoaded = true;
                CompletedSteps++;
                RegionSpawnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 3/12] Error loading region spawn data", ex);
            }
        }

        private async Task LoadVendorSpawnDataAsync()
        {
            Logger.Info("[Startup Step 4/12] Loading vendor spawn data...");

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Utility.LoadVendorSpawnData();

                        var totalVendorSpawns = Utility.VendorSpawns.Values.Sum(list => list.Count);

                        Logger.Info($"[Startup Step 4/12] Loaded {totalVendorSpawns} vendor spawn configurations across all maps");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 4/12] LoadVendorSpawnData failed", ex);
                    }
                });

                IsVendorSpawnDataLoaded = true;
                CompletedSteps++;
                VendorSpawnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 4/12] Error loading vendor spawn data", ex);
            }
        }

        private async Task LoadBestiaryAsync()
        {
            Logger.Info("[Startup Step 5/12] Loading bestiary...");

            try
            {
                await BestiaryListUtility.LoadBestiaryList();

                IsBestiaryLoaded = true;
                CompletedSteps++;
                BestiaryLoaded?.Invoke(this, EventArgs.Empty);

                Logger.Info($"[Startup Step 5/12] Loaded {BestiaryListUtility.BestiaryNameList?.Count ?? 0} creatures in bestiary");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 5/12] LoadBestiaryList failed", ex);
            }
        }

        private async Task LoadVendorListAsync()
        {
            Logger.Info("[Startup Step 5/10] Loading vendor list...");

            try
            {
                await VendorListUtility.LoadVendorList();

                IsVendorListLoaded = true;
                CompletedSteps++;
                VendorListLoaded?.Invoke(this, EventArgs.Empty);

                Logger.Info($"[Startup Step 5/10] Loaded {VendorListUtility.VendorNameList?.Count ?? 0} vendors in vendor list");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 5/10] LoadVendorList failed", ex);
            }
        }

        private async Task LoadSignDataAsync()
        {
            Logger.Info("[Startup Step 6/10] Loading sign data...");

            try
            {
                await SignDataUtility.EnsureLoadedAsync();

                IsSignDataLoaded = true;
                CompletedSteps++;

                Logger.Info($"[Startup Step 6/10] Loaded {SignDataUtility.GetTotalSignCount()} sign locations");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 6/10] LoadSignData failed", ex);
            }
        }

        private async Task LoadHiveDataAsync()
        {
            Logger.Info("[Startup Step 7/10] Loading hive data...");

            try
            {
                await HiveDataUtility.EnsureLoadedAsync();

                IsHiveDataLoaded = true;
                CompletedSteps++;

                Logger.Info($"[Startup Step 7/10] Loaded {HiveDataUtility.GetTotalHiveCount()} hive locations");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 7/10] LoadHiveData failed", ex);
            }
        }

        private async Task LoadXMLSpawnerListAsync()
        {
            Logger.Info("[Startup Step 8/10] Loading XML spawner list...");
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        SpawnerListUtility.LoadSpawnerList();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 8/10] LoadSpawnerList failed", ex);
                    }
                });

                IsXMLSpawnerListLoaded = true;
                CompletedSteps++;
                Logger.Info($"[Startup Step 8/10] XML spawner list loaded");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 8/10] Error loading XML spawner list", ex);
            }
        }

        /// <summary>
        /// Synchronizes all spawn packs with current server data.
        /// Removes invalid creatures, regions, and vendor locations from all packs.
        /// This ensures packs stay aligned when server data changes.
        /// </summary>
        private async Task SyncSpawnPacksWithServerDataAsync()
        {
            Logger.Info("[Startup Step 10/13] Syncing spawn packs with server data...");
            try
            {
                var syncService = new SpawnPackSyncService();
                await syncService.SyncAllPacksAsync();

                CompletedSteps++;
                Logger.Info("[Startup Step 10/13] Spawn pack sync completed");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 10/13] Error syncing spawn packs", ex);
            }
        }

        private async Task CopyMapFilesAsync()
        {
            Logger.Info("[Startup Step 9/10] Checking map files...");
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
                        Logger.Info($"[Startup Step 9/10] Found {mapFiles.Length} map files in Data/MAPS");
                    }
                    else
                    {
                        Logger.Warning("[Startup Step 9/10] No map files found in Data/MAPS - maps may not display correctly");
                    }
                });

                IsMapFilesCopied = true;
                CompletedSteps++;
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 9/10] Error checking map files", ex);
            }
        }

        private async Task StartDataWatcherAsync()
        {
            Logger.Info("[Startup Step 10/10] Starting DataWatcher...");
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

                        Logger.Info("[Startup Step 10/10] DataWatcher started successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"[Startup Step 10/10] DataWatcher failed to start: {ex.Message}");
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

                await Task.Run(() => Utility.LoadBoxSpawnData());
                await Task.Run(() => Utility.LoadRegionSpawnData());
                await Task.Run(() => Utility.LoadTileSpawnData());

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
