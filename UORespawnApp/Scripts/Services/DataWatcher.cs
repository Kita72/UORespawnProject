using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    internal class DataWatcher : IDisposable
    {
        private readonly FileSystemWatcher? _outputWatcher;
        private readonly FileSystemWatcher? _commandsWatcher;
        private readonly Action? _onDataChanged;
        private readonly Action? _onCommandsDetected;
        private readonly BinarySerializationService _binarySerializationService;
        private readonly SpawnPackSyncService _spawnPackSyncService;
        private CancellationTokenSource? _outputDelayTokenSource;
        private CancellationTokenSource? _commandsDelayTokenSource;
        private const int DELAY_MILLISECONDS = 1000;

        public static bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        public bool IsActive => _outputWatcher != null || _commandsWatcher != null;

        public DataWatcher(BinarySerializationService binarySerializationService, SpawnPackSyncService spawnPackSyncService, Action? onDataChanged = null, Action? onCommandsDetected = null)
        {
            _binarySerializationService = binarySerializationService;
            _spawnPackSyncService = spawnPackSyncService;
            _onDataChanged = onDataChanged;
            _onCommandsDetected = onCommandsDetected;

            // Check platform support first
            if (!IsSupported)
            {
                _outputWatcher = null;
                _commandsWatcher = null;

                Logger.Warning("DataWatcher not supported on this platform (requires Windows or macOS)");
                return;
            }

            // Watch the server OUTPUT folder where server writes .txt files
            var outputPath = PathConstants.ServerOutputPath;

            if (!string.IsNullOrEmpty(outputPath) && Directory.Exists(outputPath))
            {
                try
                {
                    _outputWatcher = new FileSystemWatcher(outputPath)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                        Filter = "*.txt",
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = false
                    };

                    _outputWatcher.Changed += OnOutputChanged;
                    _outputWatcher.Created += OnOutputChanged;

                    Logger.Info($"DataWatcher started for OUTPUT folder: {outputPath}");
                }
                catch (Exception ex)
                {
                    Logger.Warning($"DataWatcher (OUTPUT) failed to start: {ex.Message}");

                    _outputWatcher?.Dispose();
                    _outputWatcher = null;
                }
            }
            else
            {
                _outputWatcher = null;

                Logger.Warning("DataWatcher (OUTPUT) not started - Server OUTPUT folder not available");
            }

            // Watch the server COMMANDS folder where server writes edit commands
            var commandsPath = PathConstants.ServerCommandsPath;

            if (!string.IsNullOrEmpty(commandsPath) && Directory.Exists(commandsPath))
            {
                try
                {
                    _commandsWatcher = new FileSystemWatcher(commandsPath)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                        Filter = "*_edits.txt",
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = false
                    };

                    _commandsWatcher.Changed += OnCommandsChanged;
                    _commandsWatcher.Created += OnCommandsChanged;

                    Logger.Info($"DataWatcher started for COMMANDS folder: {commandsPath}");
                }
                catch (Exception ex)
                {
                    Logger.Warning($"DataWatcher (COMMANDS) failed to start: {ex.Message}");

                    _commandsWatcher?.Dispose();
                    _commandsWatcher = null;
                }
            }
            else
            {
                _commandsWatcher = null;

                Logger.Info("DataWatcher (COMMANDS) not started - Server COMMANDS folder not available");
            }
        }

        private async void OnOutputChanged(object sender, FileSystemEventArgs e)
        {
            // Thread-safe token swap: atomically replace the old source with a new one
            // This prevents race conditions where Dispose() is called while Task.Delay is still using the token
            var newSource = new CancellationTokenSource();
            var oldSource = Interlocked.Exchange(ref _outputDelayTokenSource, newSource);

            // Cancel and dispose the old source (if any) after the swap
            if (oldSource != null)
            {
                oldSource.Cancel();
                oldSource.Dispose();
            }

            try
            {
                // Wait 1 second - resets if another change happens during this time
                // This buffers for server file generation to complete
                await Task.Delay(DELAY_MILLISECONDS, newSource.Token).ConfigureAwait(false);

                if (string.IsNullOrEmpty(e.Name))
                {
                    return;
                }

                // Skip stats files (live session data, not for editor)
                if (PathConstants.IsStatsFile(e.Name))
                {
                    return;
                }

                Logger.Info($"File changed: {e.Name}");

                // Process relevant file types
                if (PathConstants.IsBestiaryFile(e.Name) || PathConstants.IsSpawnerListFile(e.Name))
                {
                    await ReloadBestiary(newSource.Token).ConfigureAwait(false);
                }
                else if (PathConstants.IsRegionListFile(e.Name))
                {
                    await ReloadRegionList(newSource.Token).ConfigureAwait(false);
                }
                else if (PathConstants.IsVendorListFile(e.Name))
                {
                    await ReloadVendorList(newSource.Token).ConfigureAwait(false);
                }
                else if (PathConstants.IsSignDataFile(e.Name))
                {
                    await ReloadSignData(newSource.Token).ConfigureAwait(false);
                }
                else if (PathConstants.IsHiveDataFile(e.Name))
                {
                    await ReloadHiveData(newSource.Token).ConfigureAwait(false);
                }
                else if (PathConstants.IsMapListFile(e.Name))
                {
                    await ReloadMapList(newSource.Token).ConfigureAwait(false);
                }
                else if (PathConstants.IsTileListFile(e.Name))
                {
                    await ReloadTileList(newSource.Token).ConfigureAwait(false);
                }
                else
                {
                    Logger.Info($"Ignoring unrecognized OUTPUT file: {e.Name}");
                    return;
                }

                _onDataChanged?.Invoke();
            }
            catch (TaskCanceledException)
            {
                // Another change happened, this delay was cancelled - this is expected
            }
            catch (Exception ex)
            {
                Logger.Error("Error processing file change", ex);
            }
        }

        private static async Task ReloadBestiary(CancellationToken cancellationToken = default)
        {
            try
            {
                // Copy server bestiary file from OUTPUT folder to Resources/Raw
                var serverBestiaryPath = PathConstants.GetServerOutputFilePath(PathConstants.BESTIARY_FILENAME);
                var localBestiaryPath = PathConstants.GetBestiaryFilePath();

                if (!string.IsNullOrEmpty(serverBestiaryPath) && File.Exists(serverBestiaryPath))
                {
                    // Ensure Resources/Raw directory exists
                    var rawDir = Path.GetDirectoryName(localBestiaryPath);
                    if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                    {
                        Directory.CreateDirectory(rawDir);
                    }

                    FileUtility.Copy(serverBestiaryPath, localBestiaryPath, overwrite: true);
                    Logger.Info($"Copied bestiary from server OUTPUT to: {localBestiaryPath}");
                }

                // Clear existing list to force reload with server-generated data
                BestiaryListUtility.ClearBestiaryList();

                await BestiaryListUtility.LoadBestiaryList(cancellationToken);

                Logger.Info("Bestiary reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading bestiary", ex);
            }
        }

        private static async Task ReloadRegionList(CancellationToken cancellationToken = default)
        {
            try
            {
                // Clear existing region data to force reload with server-generated data
                RegionListUtility.ClearRegionData();

                await RegionListUtility.EnsureLoadedAsync(cancellationToken);

                Logger.Info("Region list reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading region list", ex);
            }
        }

        private static async Task ReloadVendorList(CancellationToken cancellationToken = default)
        {
            try
            {
                // Copy server vendor list file from OUTPUT folder to Resources/Raw
                var serverVendorPath = PathConstants.GetServerOutputFilePath(PathConstants.VENDOR_LIST_FILENAME);
                var localVendorPath = PathConstants.GetVendorListFilePath();

                if (!string.IsNullOrEmpty(serverVendorPath) && File.Exists(serverVendorPath))
                {
                    // Ensure Resources/Raw directory exists
                    var rawDir = Path.GetDirectoryName(localVendorPath);
                    if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                    {
                        Directory.CreateDirectory(rawDir);
                    }

                    FileUtility.Copy(serverVendorPath, localVendorPath, overwrite: true);
                    Logger.Info($"Copied vendor list from server OUTPUT to: {localVendorPath}");
                }

                // Clear existing list to force reload with server-generated data
                VendorListUtility.ClearVendorList();

                await VendorListUtility.LoadVendorList(cancellationToken);

                Logger.Info("Vendor list reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading vendor list", ex);
            }
        }

        private async Task ReloadSignData(CancellationToken cancellationToken = default)
        {
            try
            {
                // Copy server sign data file from OUTPUT folder to Resources/Raw
                var serverSignPath = PathConstants.GetServerOutputFilePath(PathConstants.SIGN_DATA_FILENAME);
                var localSignPath = PathConstants.GetSignDataFilePath();

                if (!string.IsNullOrEmpty(serverSignPath) && File.Exists(serverSignPath))
                {
                    // Ensure Resources/Raw directory exists
                    var rawDir = Path.GetDirectoryName(localSignPath);
                    if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                    {
                        Directory.CreateDirectory(rawDir);
                    }

                    FileUtility.Copy(serverSignPath, localSignPath, overwrite: true);
                    Logger.Info($"Copied sign data from server OUTPUT to: {localSignPath}");
                }

                // Clear existing data to force reload with server-generated data
                SignDataUtility.ClearSignData();

                await SignDataUtility.EnsureLoadedAsync(cancellationToken);

                Logger.Info($"Sign data reloaded from server ({SignDataUtility.GetTotalSignCount()} locations)");

                // Sync all packs to remove vendor spawns for locations that no longer exist
                await _spawnPackSyncService.SyncAllPacksAsync(cancellationToken);
                Logger.Info("Spawn packs synced after sign data update");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading sign data", ex);
            }
        }

        private async Task ReloadHiveData(CancellationToken cancellationToken = default)
        {
            try
            {
                // Copy server hive data file from OUTPUT folder to Resources/Raw
                var serverHivePath = PathConstants.GetServerOutputFilePath(PathConstants.HIVE_DATA_FILENAME);
                var localHivePath = PathConstants.GetHiveDataFilePath();

                if (!string.IsNullOrEmpty(serverHivePath) && File.Exists(serverHivePath))
                {
                    // Ensure Resources/Raw directory exists
                    var rawDir = Path.GetDirectoryName(localHivePath);
                    if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                    {
                        Directory.CreateDirectory(rawDir);
                    }

                    FileUtility.Copy(serverHivePath, localHivePath, overwrite: true);
                    Logger.Info($"Copied hive data from server OUTPUT to: {localHivePath}");
                }

                // Clear existing data to force reload with server-generated data
                HiveDataUtility.ClearHiveData();

                await HiveDataUtility.EnsureLoadedAsync(cancellationToken);

                Logger.Info($"Hive data reloaded from server ({HiveDataUtility.GetTotalHiveCount()} locations)");

                // Sync all packs to remove vendor spawns for locations that no longer exist
                await _spawnPackSyncService.SyncAllPacksAsync(cancellationToken);
                Logger.Info("Spawn packs synced after hive data update");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading hive data", ex);
            }
        }

        private static async Task ReloadMapList(CancellationToken cancellationToken = default)
        {
            try
            {
                // Copy server map list file from OUTPUT folder to Resources/Raw
                var serverMapListPath = PathConstants.GetServerOutputFilePath(PathConstants.MAP_LIST_FILENAME);
                var localMapListPath = PathConstants.GetMapListFilePath();

                if (!string.IsNullOrEmpty(serverMapListPath) && File.Exists(serverMapListPath))
                {
                    // Ensure Resources/Raw directory exists
                    var rawDir = Path.GetDirectoryName(localMapListPath);
                    if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                    {
                        Directory.CreateDirectory(rawDir);
                    }

                    FileUtility.Copy(serverMapListPath, localMapListPath, overwrite: true);
                    Logger.Info($"Copied map list from server OUTPUT to: {localMapListPath}");
                }

                // Clear existing list to force reload with server-generated data
                MapListUtility.ClearMapList();

                await MapListUtility.LoadMapList(cancellationToken);

                Logger.Info($"Map list reloaded from server ({MapListUtility.GetMapCount()} maps)");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading map list", ex);
            }
        }

        private static async Task ReloadTileList(CancellationToken cancellationToken = default)
        {
            try
            {
                // Copy server tile list file from OUTPUT folder to Resources/Raw
                var serverTileListPath = PathConstants.GetServerOutputFilePath(PathConstants.TILE_LIST_FILENAME);
                var localTileListPath = PathConstants.GetTileListFilePath();

                if (!string.IsNullOrEmpty(serverTileListPath) && File.Exists(serverTileListPath))
                {
                    // Ensure Resources/Raw directory exists
                    var rawDir = Path.GetDirectoryName(localTileListPath);
                    if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                    {
                        Directory.CreateDirectory(rawDir);
                    }

                    FileUtility.Copy(serverTileListPath, localTileListPath, overwrite: true);
                    Logger.Info($"Copied tile list from server OUTPUT to: {localTileListPath}");
                }

                // Clear existing list to force reload with server-generated data
                TileListUtility.ClearTileList();

                await TileListUtility.LoadTileList(cancellationToken);

                Logger.Info($"Tile list reloaded from server ({TileListUtility.GetTileList().Count} tile types)");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading tile list", ex);
            }
        }

        /// <summary>
        /// Handles file changes in the COMMANDS folder.
        /// Syncs command files to local folder and notifies UI.
        /// </summary>
        private async void OnCommandsChanged(object sender, FileSystemEventArgs e)
        {
            // Thread-safe token swap: atomically replace the old source with a new one
            // This prevents race conditions where Dispose() is called while Task.Delay is still using the token
            var newSource = new CancellationTokenSource();
            var oldSource = Interlocked.Exchange(ref _commandsDelayTokenSource, newSource);

            // Cancel and dispose the old source (if any) after the swap
            if (oldSource != null)
            {
                oldSource.Cancel();
                oldSource.Dispose();
            }

            try
            {
                // Wait 1 second - resets if another change happens during this time
                await Task.Delay(DELAY_MILLISECONDS, newSource.Token).ConfigureAwait(false);

                if (string.IsNullOrEmpty(e.Name))
                    return;

                // Only process command edit files
                if (!PathConstants.IsCommandEditFile(e.Name))
                    return;

                Logger.Info($"Command file changed: {e.Name}");

                // Notify UI that commands are available (BackgroundDataLoader callback handles sync)
                _onCommandsDetected?.Invoke();
            }
            catch (TaskCanceledException)
            {
                // Another change happened, this delay was cancelled - this is expected
            }
            catch (Exception ex)
            {
                Logger.Error("Error processing command file change", ex);
            }
        }

        public void Dispose()
        {
            // Thread-safe disposal of token sources
            var outputSource = Interlocked.Exchange(ref _outputDelayTokenSource, null);
            if (outputSource != null)
            {
                outputSource.Cancel();
                outputSource.Dispose();
            }

            var commandsSource = Interlocked.Exchange(ref _commandsDelayTokenSource, null);
            if (commandsSource != null)
            {
                commandsSource.Cancel();
                commandsSource.Dispose();
            }

            if (_outputWatcher != null)
            {
                _outputWatcher.Changed -= OnOutputChanged;
                _outputWatcher.Created -= OnOutputChanged;
                _outputWatcher.Dispose();

                Logger.Info("DataWatcher (OUTPUT) stopped");
            }

            if (_commandsWatcher != null)
            {
                _commandsWatcher.Changed -= OnCommandsChanged;
                _commandsWatcher.Created -= OnCommandsChanged;
                _commandsWatcher.Dispose();

                Logger.Info("DataWatcher (COMMANDS) stopped");
            }
        }
    }
}

