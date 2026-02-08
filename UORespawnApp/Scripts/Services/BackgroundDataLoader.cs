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
        public bool IsSpawnDataLoaded { get; private set; }
        public bool IsWorldSpawnLoaded { get; private set; }
        public bool IsStaticSpawnLoaded { get; private set; }
        public bool IsXMLSpawnerListLoaded { get; private set; }
        public bool IsStaticListLoaded { get; private set; }
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
        public event EventHandler? SpawnDataLoaded;
        public event EventHandler? WorldSpawnLoaded;
        public event EventHandler? StaticSpawnLoaded;
        public event EventHandler? AllDataLoaded;

        private DataWatcher? _dataWatcher;

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
                
                // Step 1: Load Spawn Data (CSV files)
                await LoadSpawnDataAsync();
                
                // Step 2: Load World Spawn List
                await LoadWorldSpawnListAsync();
                
                // Step 3: Load Static Spawn List
                await LoadStaticSpawnListAsync();
                
                // Step 4: Load Bestiary (creature list)
                await LoadBestiaryAsync();
                
                // Step 5: Load XML Spawner List
                await LoadXMLSpawnerListAsync();
                
                // Step 6: Load Static Object List
                await LoadStaticObjectListAsync();
                
                // Step 7: Copy Map Files to wwwroot
                await CopyMapFilesAsync();
                
                // Step 8: Start DataWatcher (LAST - after all data is loaded)
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

        private async Task LoadSpawnDataAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Utility.LoadSpawnData();
                        var totalSpawns = Utility.Spawns.Values.Sum(list => list.Count);
                        Logger.Info($"Loaded {totalSpawns} spawn boxes across all maps");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("LoadSpawnData failed", ex);
                    }
                });

                IsSpawnDataLoaded = true;
                CompletedSteps++;
                SpawnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading spawn data", ex);
            }
        }

        private async Task LoadWorldSpawnListAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        WorldSpawnUtility.LoadWorldSpawnListSync();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("LoadWorldSpawnList failed", ex);
                    }
                });

                IsWorldSpawnLoaded = true;
                CompletedSteps++;
                WorldSpawnLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading world spawn list", ex);
            }
        }

        private async Task LoadStaticSpawnListAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        WorldSpawnUtility.LoadStaticSpawnListSync();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("LoadStaticSpawnList failed", ex);
                    }
                });

                IsStaticSpawnLoaded = true;
                CompletedSteps++;
                StaticSpawnLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading static spawn list", ex);
            }
        }

        private async Task LoadBestiaryAsync()
        {
            try
            {
                await WorldSpawnUtility.LoadSpawnList();
                
                IsBestiaryLoaded = true;
                CompletedSteps++;
                BestiaryLoaded?.Invoke(this, EventArgs.Empty);
                Logger.Info($"Loaded {WorldSpawnUtility.SpawnList?.Count ?? 0} creatures in bestiary");
            }
            catch (Exception ex)
            {
                Logger.Error("LoadSpawnList (bestiary) failed", ex);
            }
        }

        private async Task LoadXMLSpawnerListAsync()
        {
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
                        Logger.Error("LoadSpawnerList failed", ex);
                    }
                });

                IsXMLSpawnerListLoaded = true;
                CompletedSteps++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading XML spawner list", ex);
            }
        }

        private async Task LoadStaticObjectListAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        XMLSpawnUtility.LoadStaticList();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("LoadStaticList failed", ex);
                    }
                });

                IsStaticListLoaded = true;
                CompletedSteps++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading static object list", ex);
            }
        }

        private async Task CopyMapFilesAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "maps");
                    Directory.CreateDirectory(wwwrootPath);
                    var dataPath = Path.Combine(AppContext.BaseDirectory, "Data");
                    
                    if (Directory.Exists(dataPath))
                    {
                        int copiedCount = 0;
                        foreach (var file in Directory.GetFiles(dataPath, "*.bmp"))
                        {
                            try
                            {
                                var dest = Path.Combine(wwwrootPath, Path.GetFileName(file));
                                if (!File.Exists(dest))
                                {
                                    File.Copy(file, dest);
                                    copiedCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning($"Error copying map file {Path.GetFileName(file)}: {ex.Message}");
                            }
                        }
                        
                        if (copiedCount > 0)
                        {
                            Logger.Info($"Copied {copiedCount} map files to wwwroot");
                        }
                    }
                });

                IsMapFilesCopied = true;
                CompletedSteps++;
            }
            catch (Exception ex)
            {
                Logger.Error("Error copying map files", ex);
            }
        }

        private async Task StartDataWatcherAsync()
        {
            try
            {
                // DataWatcher starts LAST to avoid false change events during initial loading
                await Task.Run(() =>
                {
                    try
                    {
                        _dataWatcher = new DataWatcher(() =>
                        {
                            Logger.Info("Server data files have been updated - reloading...");
                            // Trigger reload of affected data
                            _ = BackgroundDataLoader.ReloadDataAsync();
                        });
                        
                        Logger.Info("DataWatcher started successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"DataWatcher failed to start: {ex.Message}");
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
