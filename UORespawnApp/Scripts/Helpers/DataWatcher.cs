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

        private async Task ReloadBestiary()
        {
            try
            {
                // Clear existing list to force reload with server-generated data
                BestiarySpawnUtility.ClearSpawnList();
                await BestiarySpawnUtility.LoadSpawnList();
                Logger.Info("Bestiary reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading bestiary", ex);
            }
        }

        private async Task ReloadRegionList()
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

