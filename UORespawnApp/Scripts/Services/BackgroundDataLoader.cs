using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    /// <summary>
    /// Service for loading application data in the background after UI initialization.
    /// This prevents blocking the UI thread during app startup.
    /// Supports cancellation for graceful shutdown.
    /// </summary>
    public class BackgroundDataLoader : IDisposable
    {
        private volatile bool _isLoading = false;
        private volatile bool _isComplete = false;
        private readonly Lock _startLock = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposed = false;

        // Loading state flags
        public bool IsMapListLoaded { get; private set; }
        public bool IsTileListLoaded { get; private set; }
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
        public bool HasPendingCommands { get; private set; }

        // Overall loading state
        public bool IsLoading => _isLoading;
        public bool IsComplete => _isComplete;

        // Progress tracking — one entry per step that calls CompletedSteps++.
        // Add/remove names here when adding/removing tracked load steps.
        private static readonly string[] _trackedStepNames =
        [
            "Settings", "MapList", "TileList", "BoxSpawns", "TileSpawns",
            "RegionSpawns", "VendorSpawns", "Bestiary", "VendorList",
            "SignData", "HiveData", "XMLSpawners", "SpawnPackSync", "MapFiles", "DataWatcher"
        ];

        public static int TotalSteps => _trackedStepNames.Length;
        public int CompletedSteps { get; private set; }
        public double ProgressPercentage => (double)CompletedSteps / TotalSteps * 100;

        // Events for components to subscribe to
        public event EventHandler? MapListLoaded;
        public event EventHandler? TileListLoaded;
        public event EventHandler? BestiaryLoaded;
        public event EventHandler? VendorListLoaded;
        public event EventHandler? BoxSpawnDataLoaded;
        public event EventHandler? TileSpawnDataLoaded;
        public event EventHandler? RegionSpawnDataLoaded;
        public event EventHandler? VendorSpawnDataLoaded;
        public event EventHandler? AllDataLoaded;
        public event EventHandler? PendingCommandsDetected;

        /// <summary>
        /// Event raised when a server update is available and needs user confirmation.
        /// MainLayout subscribes to this to show the ServerUpdateModal.
        /// </summary>
        public event EventHandler<ServerUpdateService.ServerUpdateInfo>? ServerUpdateAvailable;

        private DataWatcher? _dataWatcher;
        private readonly CommandService _commandService;
        private readonly ServerUpdateService _serverUpdateService;
        private readonly BinarySerializationService _binarySerializationService;
        private readonly SpawnDataService _spawnDataService;
        private readonly SpawnPackService _spawnPackService;
        private readonly SpawnPackSyncService _spawnPackSyncService;

        public BackgroundDataLoader(
            CommandService commandService,
            ServerUpdateService serverUpdateService,
            BinarySerializationService binarySerializationService,
            SpawnDataService spawnDataService,
            SpawnPackService spawnPackService,
            SpawnPackSyncService spawnPackSyncService)
        {
            _commandService = commandService;
            _serverUpdateService = serverUpdateService;
            _binarySerializationService = binarySerializationService;
            _spawnDataService = spawnDataService;
            _spawnPackService = spawnPackService;
            _spawnPackSyncService = spawnPackSyncService;

            // Forward server update events to our own event for MainLayout
            _serverUpdateService.OnServerUpdateAvailable += (sender, info) =>
            {
                ServerUpdateAvailable?.Invoke(this, info);
            };
        }

        /// <summary>
        /// Initializes the ActivePackDataPath from Settings.CurrentPackFolder.
        /// Called on startup to ensure edits sync back to the correct pack folder.
        /// Does NOT copy pack data — use Apply Pack explicitly to load a pack's data.
        /// </summary>
        private void InitializeActivePackPath()
        {
            try
            {
                var packFolder = Settings.CurrentPackFolder;

                // If no pack folder is set, find the pack by CurrentPackName
                // This handles fresh installs and migration from old settings format
                if (string.IsNullOrEmpty(packFolder))
                {
                    var packName = Settings.CurrentPackName;
                    if (!string.IsNullOrEmpty(packName))
                    {
                        // Try to find the pack folder by name in all pack directories
                        var approvedPath = Path.Combine(PathConstants.PacksApprovedPath, packName);
                        var createdPath = Path.Combine(PathConstants.PacksCreatedPath, packName);
                        var importedPath = Path.Combine(PathConstants.PacksImportedPath, packName);

                        if (Directory.Exists(approvedPath))
                            packFolder = approvedPath;
                        else if (Directory.Exists(createdPath))
                            packFolder = createdPath;
                        else if (Directory.Exists(importedPath))
                            packFolder = importedPath;

                        // If found, save the folder path so we don't have to look it up again
                        if (!string.IsNullOrEmpty(packFolder))
                        {
                            Settings.CurrentPackFolder = packFolder;
                            Logger.Info($"Resolved pack folder for '{packName}': {packFolder}");
                        }
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

                // If LocalDataPath is empty, ensure the approved DefaultPack is applied
                // so editors are never blank on a fresh install or after a data wipe.
                ApplyDefaultPackIfEmpty();
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
        /// Copies the first approved pack (DefaultPack) into LocalDataPath when it is
        /// genuinely empty — i.e. no spawn .bin files exist yet.
        /// Approved packs cannot be deleted, so this is always a safe, predictable source.
        /// Called after EnsureApprovedPacksUnpackedAsync guarantees the folder exists.
        /// </summary>
        private static void ApplyDefaultPackIfEmpty()
        {
            try
            {
                var localDataPath = PathConstants.LocalDataPath;

                // Only the four spawn .bin files matter — settings absence is fine
                string[] spawnFiles = [PathConstants.BOX_FILENAME, PathConstants.TILE_FILENAME,
                                       PathConstants.REGION_FILENAME, PathConstants.VENDOR_FILENAME];

                // If any spawn file already exists, LocalDataPath is not empty — do nothing
                if (spawnFiles.Any(f => File.Exists(Path.Combine(localDataPath, f))))
                    return;

                Logger.Info("[DefaultPack] LocalDataPath has no spawn files — applying approved pack as fallback...");

                // Find first approved pack folder that contains spawn data
                var approvedPath = PathConstants.PacksApprovedPath;
                if (!Directory.Exists(approvedPath))
                {
                    Logger.Warning("[DefaultPack] Approved packs folder not found — cannot apply fallback");
                    return;
                }

                string? packFolder = null;
                string? packDataPath = null;

                foreach (var folder in Directory.GetDirectories(approvedPath))
                {
                    if (spawnFiles.Any(f => File.Exists(Path.Combine(folder, f))))
                    {
                        packFolder = folder;
                        packDataPath = folder;
                        break;
                    }

                    var nested = Path.Combine(folder, PathConstants.UOR_DATA_SUBFOLDER);
                    if (Directory.Exists(nested) && spawnFiles.Any(f => File.Exists(Path.Combine(nested, f))))
                    {
                        packFolder = folder;
                        packDataPath = nested;
                        break;
                    }
                }

                if (packFolder == null || packDataPath == null)
                {
                    Logger.Warning("[DefaultPack] No approved pack with spawn data found — cannot apply fallback");
                    return;
                }

                // Copy all data files (spawn + settings) from approved pack to LocalDataPath
                string[] allDataFiles = [PathConstants.SETTINGS_FILENAME, PathConstants.BOX_FILENAME,
                                         PathConstants.TILE_FILENAME, PathConstants.REGION_FILENAME, PathConstants.VENDOR_FILENAME];

                if (!Directory.Exists(localDataPath))
                    Directory.CreateDirectory(localDataPath);

                foreach (var fileName in allDataFiles)
                {
                    var src = Path.Combine(packDataPath, fileName);
                    if (File.Exists(src))
                        FileUtility.Copy(src, Path.Combine(localDataPath, fileName), overwrite: true);
                }

                // Update settings so save-back targets this pack correctly
                var packName = Path.GetFileName(packFolder);
                Settings.CurrentPackFolder = packFolder;
                Settings.CurrentPackName   = packName;
                PathConstants.ActivePackDataPath = packDataPath;

                Logger.Info($"[DefaultPack] Fallback applied: '{packName}' → LocalDataPath");
            }
            catch (Exception ex)
            {
                Logger.Error("[DefaultPack] Error applying default pack fallback", ex);
            }
        }

        /// <summary>
        /// Loads all application data asynchronously in the background.
        /// Should be called after the UI has rendered.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token for graceful shutdown</param>
        public async Task LoadAllDataAsync(CancellationToken cancellationToken = default)
        {
            lock (_startLock)
            {
                if (_isLoading || _isComplete)
                {
                    Logger.Info("Background data loading already in progress or complete");
                    return;
                }
                _isLoading = true;
            }

            // Create internal cancellation source that can be cancelled via Cancel() or the passed token
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;

            CompletedSteps = 0;

            Logger.Info("Starting background data loading...");

            var startTime = DateTime.Now;

            try
            {
                // Load data in logical order (some dependencies exist)

                // Step 0: Load Settings (FIRST - other systems may depend on settings)
                await LoadSettingsAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 0.25: Check and update server installation if linked
                // This MUST happen BEFORE syncing server data to ensure scripts are current
                await CheckAndUpdateServerAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 0.5: Sync server OUTPUT data to Resources/Raw if linked
                // This MUST happen BEFORE loading map/tile lists and other server-generated data
                await SyncServerOutputDataAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 0.6: Check for and sync pending commands from server
                await SyncAndCheckPendingCommandsAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 1: Load Map List (needed for map name lookups)
                await LoadMapListAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 2: Load Tile List (needed for tile spawn page)
                await LoadTileListAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 2.5: Ensure approved packs are unpacked from Backup folder
                // This MUST happen BEFORE InitializeActivePackPath so the pack exists in Approved
                await EnsureApprovedPacksUnpackedAsync(token);
                token.ThrowIfCancellationRequested();

                // Initialize active pack path from saved CurrentPackName setting
                // Now the pack will exist in Approved (either from Backup ZIP or folder)
                InitializeActivePackPath();

                // Step 3: Load Box Spawn Data (Binary)
                await LoadBoxSpawnDataAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 4: Load Tile Spawn Data (Binary)
                await LoadTileSpawnDataAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 5: Load Region Spawn Data (Binary)
                await LoadRegionSpawnDataAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 6: Load Vendor Spawn Data (Binary)
                await LoadVendorSpawnDataAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 7: Load Bestiary (creature list from server-generated text file)
                await LoadBestiaryAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 8: Load Vendor List (vendor names from server-generated text file)
                await LoadVendorListAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 9: Load Sign Data (shop sign locations for vendor spawning)
                await LoadSignDataAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 10: Load Hive Data (bee hive locations for beekeeper spawning)
                await LoadHiveDataAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 11: Load XML Spawner List
                await LoadXMLSpawnerListAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 12: Sync spawn packs with server data (remove invalid creatures, regions, locations)
                await SyncSpawnPacksWithServerDataAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 13: Verify Map Files exist in Data/MAPS
                await CopyMapFilesAsync(token);
                token.ThrowIfCancellationRequested();

                // Step 14: Start DataWatcher (LAST - after all data is loaded)
                await StartDataWatcherAsync(token);

                _isComplete = true;
                var elapsed = DateTime.Now - startTime;

                Logger.Info($"Background data loading completed in {elapsed.TotalSeconds:F2} seconds");

                AllDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Background data loading was cancelled");
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
        /// Cancels any ongoing background loading operation.
        /// </summary>
        public void Cancel()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                Logger.Info("Background data loading cancellation requested");
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }
        }

        /// <summary>
        /// Ensures approved packs are unpacked from Backup folder to Approved folder.
        /// Must be called BEFORE InitializeActivePackPath so the packs exist.
        /// 
        /// Flow:
        ///   Backup/DefaultPack.zip  â†’ Approved/DefaultPack/ (for releases)
        ///   Backup/DefaultPack/     â†’ Approved/DefaultPack/ (for Git repo/dev)
        /// </summary>
        private async Task EnsureApprovedPacksUnpackedAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup] Ensuring approved packs are unpacked from Backup...");

            try
            {
                await Task.Run(() => _spawnPackService.UnpackApprovedPacks(), cancellationToken);

                Logger.Info("[Startup] Approved packs unpacked successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup] Error unpacking approved packs", ex);
            }
        }

        /// <summary>
        /// Syncs all server OUTPUT data files to Resources/Raw if server is linked.
        /// This ensures we always load the latest server-generated data on startup.
        /// Files synced: MapList, TileList, BestiaryList, VendorList, RegionList, SpawnerList, SignData, HiveData
        /// </summary>
        private static async Task SyncServerOutputDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var serverOutputPath = PathConstants.ServerOutputPath;
                if (string.IsNullOrEmpty(serverOutputPath))
                {
                    Logger.Info("[ServerSync] No server linked - using bundled data files");
                    return;
                }

                Logger.Info("[ServerSync] Server linked - syncing OUTPUT data to Resources/Raw...");

                // All server OUTPUT files that need to be synced
                string[] outputFiles = [
                    PathConstants.MAP_LIST_FILENAME,
                    PathConstants.TILE_LIST_FILENAME,
                    PathConstants.BESTIARY_FILENAME,
                    PathConstants.VENDOR_LIST_FILENAME,
                    PathConstants.REGION_LIST_FILENAME,
                    PathConstants.SPAWNER_LIST_FILENAME,
                    PathConstants.SIGN_DATA_FILENAME,
                    PathConstants.HIVE_DATA_FILENAME
                ];

                var rawDir = PathConstants.ResourcesRawPath;
                if (!Directory.Exists(rawDir))
                {
                    Directory.CreateDirectory(rawDir);
                }

                int syncedCount = 0;
                await Task.Run(() =>
                {
                    foreach (var fileName in outputFiles)
                    {
                        var serverFilePath = Path.Combine(serverOutputPath, fileName);
                        if (File.Exists(serverFilePath))
                        {
                            var localFilePath = Path.Combine(rawDir, fileName);
                            FileUtility.Copy(serverFilePath, localFilePath, overwrite: true);
                            syncedCount++;
                        }
                    }
                }, cancellationToken);

                Logger.Info($"[ServerSync] Synced {syncedCount} OUTPUT files from server");
            }
            catch (Exception ex)
            {
                Logger.Error("[ServerSync] Error syncing server OUTPUT data", ex);
            }
        }

        /// <summary>
        /// Syncs pending command files from server COMMANDS folder and checks for local command files.
        /// If commands are found, raises PendingCommandsDetected event for UI to show modal.
        /// </summary>
        private async Task SyncAndCheckPendingCommandsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Info("[CommandSync] Checking for pending edit commands...");

                // First, sync any commands from linked server
                var serverCommandsPath = PathConstants.ServerCommandsPath;
                if (serverCommandsPath != null)
                {
                    await Task.Run(() => _commandService.SyncCommandsFromServer(), cancellationToken);
                }

                // Then check for local command files (including any just synced)
                int pendingCount = await Task.Run(() => _commandService.CheckForPendingCommands(), cancellationToken);

                if (pendingCount > 0)
                {
                    HasPendingCommands = true;
                    Logger.Info($"[CommandSync] Found {pendingCount} pending commands - UI will show modal");
                    PendingCommandsDetected?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Logger.Info("[CommandSync] No pending commands found");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[CommandSync] Error checking for pending commands: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if server is linked and verify server installation status.
        /// If an update is available, raises ServerUpdateAvailable event for UI to show confirmation modal.
        /// Does NOT auto-update - user confirmation is always required.
        /// </summary>
        private async Task CheckAndUpdateServerAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.ScriptsCustomFolder))
                {
                    Logger.Info("[ServerCheck] No server linked - skipping server version check");
                    return;
                }

                Logger.Info("[ServerCheck] Checking server installation...");

                await Task.Run(() =>
                {
                    // Use ServerUpdateService for version checking
                    // This will raise OnServerUpdateAvailable event if update is needed
                    // The event is forwarded to ServerUpdateAvailable which MainLayout subscribes to
                    bool checked_ok = _serverUpdateService.CheckForServerUpdate();

                    if (checked_ok)
                    {
                        if (_serverUpdateService.PendingUpdate != null)
                        {
                            Logger.Info("[ServerCheck] Update available - waiting for user confirmation");
                        }
                        else
                        {
                            Logger.Info("[ServerCheck] Server is ready");
                        }
                    }
                    else
                    {
                        Logger.Warning("[ServerCheck] Server check encountered issues - check logs");
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Warning($"[ServerCheck] Error during server check: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the map list from file (Resources/Raw/UOR_MapList.txt).
        /// This provides map IDs and names including custom maps from server.
        /// </summary>
        private async Task LoadMapListAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup] Loading map list...");

            try
            {
                await MapListUtility.LoadMapList(cancellationToken);

                IsMapListLoaded = true;
                CompletedSteps++;
                MapListLoaded?.Invoke(this, EventArgs.Empty);

                var mapCount = MapListUtility.GetMapCount();
                var hasCustomMaps = MapListUtility.HasCustomMaps();

                Logger.Info($"[Startup] Loaded {mapCount} maps (custom maps: {hasCustomMaps})");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup] LoadMapList failed", ex);
            }
        }

        /// <summary>
        /// Loads the tile list from file (Resources/Raw/UOR_TileList.txt).
        /// This provides valid tile type names for tile spawning.
        /// </summary>
        private async Task LoadTileListAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup] Loading tile list...");

            try
            {
                await TileListUtility.LoadTileList(cancellationToken);

                IsTileListLoaded = true;
                CompletedSteps++;
                TileListLoaded?.Invoke(this, EventArgs.Empty);

                Logger.Info($"[Startup] Loaded {TileListUtility.GetTileList().Count} tile types");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup] LoadTileList failed", ex);
            }
        }

        private async Task LoadSettingsAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 0/14] Loading settings...");

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        _binarySerializationService.LoadSettings();

                        Logger.Info("[Startup Step 0/14] Settings loaded from binary file (or defaults if file missing)");

                        // Always save settings to ensure binary file exists (creates file if missing)
                        _binarySerializationService.SaveSettings();

                        Logger.Info($"[Startup Step 0/14] Current pack: {Settings.CurrentPackName}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 0/14] LoadSettings failed - using Preferences defaults", ex);
                    }
                }, cancellationToken);

                CompletedSteps++;
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 0/14] Error loading settings", ex);
            }
        }

        private async Task LoadBoxSpawnDataAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 3/14] Loading box spawn data...");

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        _spawnDataService.ClearBoxSpawns();
                        _spawnDataService.InitializeBoxSpawns();
                        _binarySerializationService.LoadBoxSpawns();

                        var totalSpawns = _spawnDataService.GetTotalBoxSpawnCount();

                        Logger.Info($"[Startup Step 3/14] Loaded {totalSpawns} spawn boxes across all maps");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 3/14] LoadSpawnData failed", ex);
                    }
                }, cancellationToken);

                IsBoxSpawnDataLoaded = true;
                CompletedSteps++;
                BoxSpawnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 3/14] Error loading spawn data", ex);
            }
        }

        private async Task LoadTileSpawnDataAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 4/14] Loading tile spawn data...");

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        _spawnDataService.ClearTileSpawns();
                        _spawnDataService.InitializeTileSpawns();
                        _binarySerializationService.LoadTileSpawns();

                        var totalTileSpawns = _spawnDataService.GetTotalTileSpawnCount();

                        Logger.Info($"[Startup Step 4/14] Loaded {totalTileSpawns} tile spawn configurations across all maps");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 4/14] LoadTileSpawnData failed", ex);
                    }
                }, cancellationToken);

                IsTileSpawnDataLoaded = true;
                CompletedSteps++;
                TileSpawnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 4/14] Error loading tile spawn data", ex);
            }
        }

        private async Task LoadRegionSpawnDataAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 5/14] Loading region spawn data...");

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        _spawnDataService.ClearRegionSpawns();
                        _spawnDataService.InitializeRegionSpawns();
                        _binarySerializationService.LoadRegionSpawns();

                        var totalRegionSpawns = _spawnDataService.GetTotalRegionSpawnCount();

                        Logger.Info($"[Startup Step 5/14] Loaded {totalRegionSpawns} region spawn configurations across all maps");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 5/14] LoadRegionSpawnData failed", ex);
                    }
                }, cancellationToken);

                IsRegionSpawnDataLoaded = true;
                CompletedSteps++;
                RegionSpawnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 5/14] Error loading region spawn data", ex);
            }
        }

        private async Task LoadVendorSpawnDataAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 6/14] Loading vendor spawn data...");

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        _spawnDataService.ClearVendorSpawns();
                        _spawnDataService.InitializeVendorSpawns();
                        _binarySerializationService.LoadVendorSpawns();

                        var totalVendorSpawns = _spawnDataService.GetTotalVendorSpawnCount();

                        Logger.Info($"[Startup Step 6/14] Loaded {totalVendorSpawns} vendor spawn configurations across all maps");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("[Startup Step 6/14] LoadVendorSpawnData failed", ex);
                    }
                }, cancellationToken);

                IsVendorSpawnDataLoaded = true;
                CompletedSteps++;
                VendorSpawnDataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 6/14] Error loading vendor spawn data", ex);
            }
        }

        private async Task LoadBestiaryAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 7/14] Loading bestiary...");

            try
            {
                await BestiaryListUtility.LoadBestiaryList(cancellationToken);

                IsBestiaryLoaded = true;
                CompletedSteps++;
                BestiaryLoaded?.Invoke(this, EventArgs.Empty);

                Logger.Info($"[Startup Step 7/14] Loaded {BestiaryListUtility.BestiaryNameList?.Count ?? 0} creatures in bestiary");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 7/14] LoadBestiaryList failed", ex);
            }
        }

        private async Task LoadVendorListAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 8/14] Loading vendor list...");

            try
            {
                await VendorListUtility.LoadVendorList(cancellationToken);

                IsVendorListLoaded = true;
                CompletedSteps++;
                VendorListLoaded?.Invoke(this, EventArgs.Empty);

                Logger.Info($"[Startup Step 8/14] Loaded {VendorListUtility.VendorNameList?.Count ?? 0} vendors in vendor list");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 8/14] LoadVendorList failed", ex);
            }
        }

        private async Task LoadSignDataAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 9/14] Loading sign data...");

            try
            {
                // If server is linked, copy sign data from OUTPUT to Resources/Raw BEFORE loading
                // This ensures we have the latest server-generated data
                await SyncSignDataFromServerAsync(cancellationToken);

                // Clear any previously loaded data to force fresh load
                SignDataUtility.ClearSignData();

                await SignDataUtility.EnsureLoadedAsync(cancellationToken);

                IsSignDataLoaded = true;
                CompletedSteps++;

                Logger.Info($"[Startup Step 9/14] Loaded {SignDataUtility.GetTotalSignCount()} sign locations");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 9/14] LoadSignData failed", ex);
            }
        }

        /// <summary>
        /// Copies sign data from server OUTPUT folder to Resources/Raw if server is linked.
        /// This ensures we always load the latest server-generated sign data on startup.
        /// </summary>
        private static async Task SyncSignDataFromServerAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var serverOutputPath = PathConstants.ServerOutputPath;
                if (string.IsNullOrEmpty(serverOutputPath))
                {
                    Logger.Info("[SignSync] No server linked - using local sign data");
                    return;
                }

                var serverSignPath = Path.Combine(serverOutputPath, PathConstants.SIGN_DATA_FILENAME);
                if (!File.Exists(serverSignPath))
                {
                    Logger.Warning($"[SignSync] Server sign data file not found at: {serverSignPath}");
                    return;
                }

                var localSignPath = PathConstants.GetSignDataFilePath();

                // Ensure Resources/Raw directory exists
                var rawDir = Path.GetDirectoryName(localSignPath);
                if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                {
                    Directory.CreateDirectory(rawDir);
                }

                await Task.Run(() => FileUtility.Copy(serverSignPath, localSignPath, overwrite: true), cancellationToken);
                Logger.Info($"[SignSync] Copied sign data from server OUTPUT to: {localSignPath}");
            }
            catch (Exception ex)
            {
                Logger.Error("[SignSync] Error syncing sign data from server", ex);
            }
        }

        private async Task LoadHiveDataAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 10/14] Loading hive data...");

            try
            {
                // If server is linked, copy hive data from OUTPUT to Resources/Raw BEFORE loading
                // This ensures we have the latest server-generated data
                await SyncHiveDataFromServerAsync(cancellationToken);

                // Clear any previously loaded data to force fresh load
                HiveDataUtility.ClearHiveData();

                await HiveDataUtility.EnsureLoadedAsync(cancellationToken);

                IsHiveDataLoaded = true;
                CompletedSteps++;

                Logger.Info($"[Startup Step 10/14] Loaded {HiveDataUtility.GetTotalHiveCount()} hive locations");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 10/14] LoadHiveData failed", ex);
            }
        }

        /// <summary>
        /// Copies hive data from server OUTPUT folder to Resources/Raw if server is linked.
        /// This ensures we always load the latest server-generated hive data on startup.
        /// </summary>
        private static async Task SyncHiveDataFromServerAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var serverOutputPath = PathConstants.ServerOutputPath;
                if (string.IsNullOrEmpty(serverOutputPath))
                {
                    Logger.Info("[HiveSync] No server linked - using local hive data");
                    return;
                }

                var serverHivePath = Path.Combine(serverOutputPath, PathConstants.HIVE_DATA_FILENAME);
                if (!File.Exists(serverHivePath))
                {
                    Logger.Warning($"[HiveSync] Server hive data file not found at: {serverHivePath}");
                    return;
                }

                var localHivePath = PathConstants.GetHiveDataFilePath();

                // Ensure Resources/Raw directory exists
                var rawDir = Path.GetDirectoryName(localHivePath);
                if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                {
                    Directory.CreateDirectory(rawDir);
                }

                await Task.Run(() => FileUtility.Copy(serverHivePath, localHivePath, overwrite: true), cancellationToken);
                Logger.Info($"[HiveSync] Copied hive data from server OUTPUT to: {localHivePath}");
            }
            catch (Exception ex)
            {
                Logger.Error("[HiveSync] Error syncing hive data from server", ex);
            }
        }

        private async Task LoadXMLSpawnerListAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 11/14] Loading XML spawner list...");
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
                        Logger.Error("[Startup Step 11/14] LoadSpawnerList failed", ex);
                    }
                }, cancellationToken);

                IsXMLSpawnerListLoaded = true;
                CompletedSteps++;
                Logger.Info($"[Startup Step 11/14] XML spawner list loaded");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 11/14] Error loading XML spawner list", ex);
            }
        }

        /// <summary>
        /// Synchronizes all spawn packs with current server data.
        /// Removes invalid creatures, regions, and vendor locations from all packs.
        /// This ensures packs stay aligned when server data changes.
        /// </summary>
        private async Task SyncSpawnPacksWithServerDataAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 12/14] Syncing spawn packs with server data...");
            try
            {
                await _spawnPackSyncService.SyncAllPacksAsync(cancellationToken);

                CompletedSteps++;
                Logger.Info("[Startup Step 12/14] Spawn pack sync completed");
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 12/14] Error syncing spawn packs", ex);
            }
        }

        private async Task CopyMapFilesAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 13/14] Checking map files...");
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
                        Logger.Info($"[Startup Step 13/14] Found {mapFiles.Length} map files in Data/MAPS");
                    }
                    else
                    {
                        Logger.Warning("[Startup Step 13/14] No map files found in Data/MAPS - maps may not display correctly");
                    }
                }, cancellationToken);

                IsMapFilesCopied = true;
                CompletedSteps++;
            }
            catch (Exception ex)
            {
                Logger.Error("[Startup Step 13/14] Error checking map files", ex);
            }
        }

        private async Task StartDataWatcherAsync(CancellationToken cancellationToken = default)
        {
            Logger.Info("[Startup Step 14/14] Starting DataWatcher...");
                try
                {
                    // DataWatcher starts LAST to avoid false change events during initial loading
                    await Task.Run(() =>
                    {
                        try
                        {
                            _dataWatcher = new DataWatcher(
                                _binarySerializationService,
                                _spawnPackSyncService,
                                onDataChanged: () =>
                                {
                                    Logger.Info("[DataWatcher] Server data files have been updated - reloading...");
                                    // Trigger reload of affected data
                                    _ = ReloadDataAsync();
                                },
                                onCommandsDetected: () =>
                                {
                                    Logger.Info("[DataWatcher] Server command files detected");
                                    // Sync from server then check local
                                    _commandService.SyncCommandsFromServer();
                                    _commandService.CheckForPendingCommands();
                                    HasPendingCommands = _commandService.HasPendingCommands;
                                    if (HasPendingCommands)
                                    {
                                        PendingCommandsDetected?.Invoke(this, EventArgs.Empty);
                                    }
                                }
                            );

                            Logger.Info("[Startup Step 14/14] DataWatcher started successfully");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"[Startup Step 14/14] DataWatcher failed to start: {ex.Message}");
                        }
                    }, cancellationToken);

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
        private async Task ReloadDataAsync()
        {
            try
            {
                Logger.Info("Reloading spawn data due to server file changes...");

                await Task.WhenAll(
                    Task.Run(() =>
                    {
                        _spawnDataService.ClearBoxSpawns();
                        _spawnDataService.InitializeBoxSpawns();
                        _binarySerializationService.LoadBoxSpawns();
                    }),
                    Task.Run(() =>
                    {
                        _spawnDataService.ClearRegionSpawns();
                        _spawnDataService.InitializeRegionSpawns();
                        _binarySerializationService.LoadRegionSpawns();
                    }),
                    Task.Run(() =>
                    {
                        _spawnDataService.ClearTileSpawns();
                        _spawnDataService.InitializeTileSpawns();
                        _binarySerializationService.LoadTileSpawns();
                    }),
                    Task.Run(() =>
                    {
                        _spawnDataService.ClearVendorSpawns();
                        _spawnDataService.InitializeVendorSpawns();
                        _binarySerializationService.LoadVendorSpawns();
                    })
                );

                Logger.Info("Spawn data reloaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading data", ex);
            }
        }

        /// <summary>
        /// Forces reload of all vendor-related reference data from server.
        /// Call this when server sign/hive data changes and you need the editor to pick up the changes.
        /// This will:
        /// 1. Copy sign/hive data from server OUTPUT to Resources/Raw
        /// 2. Clear and reload SignDataUtility and HiveDataUtility
        /// 3. Sync all packs to remove invalid vendor locations
        /// </summary>
        public async Task ForceReloadVendorReferenceDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Info("[ForceReload] Forcing reload of vendor reference data...");

                // Copy fresh data from server
                await SyncSignDataFromServerAsync(cancellationToken);
                await SyncHiveDataFromServerAsync(cancellationToken);

                // Clear and reload utilities
                SignDataUtility.ClearSignData();
                HiveDataUtility.ClearHiveData();
                await SignDataUtility.EnsureLoadedAsync(cancellationToken);
                await HiveDataUtility.EnsureLoadedAsync(cancellationToken);

                Logger.Info($"[ForceReload] Loaded {SignDataUtility.GetTotalSignCount()} signs, {HiveDataUtility.GetTotalHiveCount()} hives");

                // Sync all packs to remove invalid vendor locations
                await _spawnPackSyncService.SyncAllPacksAsync(cancellationToken);

                Logger.Info("[ForceReload] Vendor reference data reload complete");
            }
            catch (Exception ex)
            {
                Logger.Error("[ForceReload] Error reloading vendor reference data", ex);
            }
        }

        /// <summary>
        /// Cleanup resources including cancellation token and data watcher.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Cancel any ongoing loading
            Cancel();

            // Dispose resources
            _cancellationTokenSource?.Dispose();
            _dataWatcher?.Dispose();

            Logger.Info("BackgroundDataLoader disposed");
            GC.SuppressFinalize(this);
        }
    }
}
