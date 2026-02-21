using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp
{
    internal partial class DataWatcher : IDisposable
    {
        private readonly FileSystemWatcher? _watcher;
        private readonly Action? _onDataChanged;
        private CancellationTokenSource? _delayTokenSource;
        private const int DELAY_MILLISECONDS = 1000;
        
        public static bool IsSupported => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        public bool IsActive => _watcher != null;

        public DataWatcher(Action? onDataChanged = null)
        {
            _onDataChanged = onDataChanged;
            
            // Check platform support first
            if (!IsSupported)
            {
                _watcher = null;

                Logger.Warning("DataWatcher not supported on this platform (requires Windows or macOS)");
                return;
            }
            
            if (!string.IsNullOrEmpty(Settings.ServUODataFolder) && 
                Directory.Exists(Settings.ServUODataFolder))
            {
                try
                {
                    _watcher = new FileSystemWatcher(Settings.ServUODataFolder)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                        Filter = "*.txt",
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = false
                    };

                    SetupWatcher();

                    Logger.Info($"DataWatcher started for: {Settings.ServUODataFolder}");
                }
                catch (Exception ex)
                {
                    Logger.Warning($"DataWatcher failed to start: {ex.Message}");

                    _watcher?.Dispose();
                    _watcher = null;
                }
            }
            else
            {
                _watcher = null;

                Logger.Warning("DataWatcher not started - No ServUO folder configured");
            }
        }

        private void SetupWatcher()
        {
            if (_watcher != null)
            {
                _watcher.Changed += OnChanged;
                _watcher.Created += OnChanged;
            }
        }

        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
            // Cancel any existing delay if file changes again
            _delayTokenSource?.Cancel();
            _delayTokenSource = new CancellationTokenSource();

            try
            {
                // Wait 1 second - resets if another change happens during this time
                // This buffers for server file generation to complete
                await Task.Delay(DELAY_MILLISECONDS, _delayTokenSource.Token).ConfigureAwait(false);

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
                    await ReloadBestiary().ConfigureAwait(false);
                }
                else if (PathConstants.IsRegionListFile(e.Name))
                {
                    await ReloadRegionList().ConfigureAwait(false);
                }
                else if (PathConstants.IsVendorListFile(e.Name))
                {
                    await ReloadVendorList().ConfigureAwait(false);
                }
                else if (PathConstants.IsSignDataFile(e.Name))
                {
                    await ReloadSignData().ConfigureAwait(false);
                }
                else if (PathConstants.IsHiveDataFile(e.Name))
                {
                    await ReloadHiveData().ConfigureAwait(false);
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

        private static async Task ReloadBestiary()
        {
            try
            {
                // Copy server bestiary file to Resources/Raw to update local copy
                var serverPath = Settings.ServUODataFolder;
                if (!string.IsNullOrEmpty(serverPath))
                {
                    var serverBestiaryPath = Path.Combine(serverPath, PathConstants.UOR_DATA_SUBFOLDER, PathConstants.BESTIARY_FILENAME);
                    var localBestiaryPath = PathConstants.GetBestiaryFilePath();

                    if (File.Exists(serverBestiaryPath))
                    {
                        // Ensure Resources/Raw directory exists
                        var rawDir = Path.GetDirectoryName(localBestiaryPath);
                        if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                        {
                            Directory.CreateDirectory(rawDir);
                        }

                        File.Copy(serverBestiaryPath, localBestiaryPath, overwrite: true);
                        Logger.Info($"Copied bestiary from server to: {localBestiaryPath}");
                    }
                }

                // Clear existing list to force reload with server-generated data
                BestiaryListUtility.ClearSpawnList();

                await BestiaryListUtility.LoadSpawnList();

                Logger.Info("Bestiary reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading bestiary", ex);
            }
        }

        private static async Task ReloadRegionList()
        {
            try
            {
                // Clear existing region data to force reload with server-generated data
                RegionDataUtility.ClearRegionData();

                await RegionDataUtility.EnsureLoadedAsync();

                Logger.Info("Region list reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading region list", ex);
            }
        }

        private static async Task ReloadVendorList()
        {
            try
            {
                // Copy server vendor list file to Resources/Raw to update local copy
                var serverPath = Settings.ServUODataFolder;
                if (!string.IsNullOrEmpty(serverPath))
                {
                    var serverVendorPath = Path.Combine(serverPath, PathConstants.UOR_DATA_SUBFOLDER, PathConstants.VENDOR_LIST_FILENAME);
                    var localVendorPath = PathConstants.GetVendorListFilePath();

                    if (File.Exists(serverVendorPath))
                    {
                        // Ensure Resources/Raw directory exists
                        var rawDir = Path.GetDirectoryName(localVendorPath);
                        if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                        {
                            Directory.CreateDirectory(rawDir);
                        }

                        File.Copy(serverVendorPath, localVendorPath, overwrite: true);
                        Logger.Info($"Copied vendor list from server to: {localVendorPath}");
                    }
                }

                // Clear existing list to force reload with server-generated data
                VendorListUtility.ClearVendorList();

                await VendorListUtility.LoadVendorList();

                Logger.Info("Vendor list reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading vendor list", ex);
            }
        }

        private static async Task ReloadSignData()
        {
            try
            {
                // Copy server sign data file to Resources/Raw to update local copy
                var serverPath = Settings.ServUODataFolder;
                if (!string.IsNullOrEmpty(serverPath))
                {
                    var serverSignPath = Path.Combine(serverPath, PathConstants.UOR_DATA_SUBFOLDER, PathConstants.SIGN_DATA_FILENAME);
                    var localSignPath = PathConstants.GetSignDataFilePath();

                    if (File.Exists(serverSignPath))
                    {
                        // Ensure Resources/Raw directory exists
                        var rawDir = Path.GetDirectoryName(localSignPath);
                        if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                        {
                            Directory.CreateDirectory(rawDir);
                        }

                        File.Copy(serverSignPath, localSignPath, overwrite: true);
                        Logger.Info($"Copied sign data from server to: {localSignPath}");
                    }
                }

                // Clear existing data to force reload with server-generated data
                SignDataUtility.ClearSignData();

                await SignDataUtility.EnsureLoadedAsync();

                Logger.Info("Sign data reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading sign data", ex);
            }
        }

        private static async Task ReloadHiveData()
        {
            try
            {
                // Copy server hive data file to Resources/Raw to update local copy
                var serverPath = Settings.ServUODataFolder;
                if (!string.IsNullOrEmpty(serverPath))
                {
                    var serverHivePath = Path.Combine(serverPath, PathConstants.UOR_DATA_SUBFOLDER, PathConstants.HIVE_DATA_FILENAME);
                    var localHivePath = PathConstants.GetHiveDataFilePath();

                    if (File.Exists(serverHivePath))
                    {
                        // Ensure Resources/Raw directory exists
                        var rawDir = Path.GetDirectoryName(localHivePath);
                        if (!string.IsNullOrEmpty(rawDir) && !Directory.Exists(rawDir))
                        {
                            Directory.CreateDirectory(rawDir);
                        }

                        File.Copy(serverHivePath, localHivePath, overwrite: true);
                        Logger.Info($"Copied hive data from server to: {localHivePath}");
                    }
                }

                // Clear existing data to force reload with server-generated data
                HiveDataUtility.ClearHiveData();

                await HiveDataUtility.EnsureLoadedAsync();

                Logger.Info("Hive data reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading hive data", ex);
            }
        }

        public void Dispose()
        {
            _delayTokenSource?.Cancel();
            _delayTokenSource?.Dispose();

            if (_watcher != null)
            {
                _watcher.Changed -= OnChanged;
                _watcher.Created -= OnChanged;
                _watcher.Dispose();

                Logger.Info("DataWatcher stopped");
            }
        }
    }
}

