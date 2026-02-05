using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp
{
    internal class DataWatcher : IDisposable
    {
        private readonly FileSystemWatcher? _watcher;
        private readonly Action? _onDataChanged;
        private DateTime _lastReloadTime = DateTime.MinValue;
        private const int DEBOUNCE_MILLISECONDS = 500;
        
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

#pragma warning disable CS4014 // Suppress async void warning for event handler
        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce - prevent multiple rapid fire events
            var now = DateTime.Now;
            if ((now - _lastReloadTime).TotalMilliseconds < DEBOUNCE_MILLISECONDS)
            {
                return;
            }
            _lastReloadTime = now;
            
            // Wait a bit to ensure file is fully written
            await Task.Delay(DEBOUNCE_MILLISECONDS).ConfigureAwait(false);
            
            if (!string.IsNullOrEmpty(e.Name))
            {
                Logger.Info($"File changed: {e.Name}");
                
                try
                {
                    if (IsBestiaryFile(e.Name))
                    {
                        await ReloadBestiary().ConfigureAwait(false);
                    }
                    else if (IsStaticListFile(e.Name))
                    {
                        await ReloadStaticList().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error processing file change", ex);
                }
            }
        }
#pragma warning restore CS4014

        private static bool IsBestiaryFile(string fileName)
        {
            return fileName.Contains("UOR_SpawnerList", StringComparison.OrdinalIgnoreCase) ||
                   fileName.Contains("Bestiary", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStaticListFile(string fileName)
        {
            return fileName.Contains("UOR_StaticList", StringComparison.OrdinalIgnoreCase);
        }

        private async Task ReloadBestiary()
        {
            try
            {
                await WorldSpawnUtility.LoadSpawnList();
                _onDataChanged?.Invoke();
                Logger.Info("Bestiary reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading bestiary", ex);
            }
        }

        private async Task ReloadStaticList()
        {
            try
            {
                await WorldSpawnUtility.LoadStaticList();
                _onDataChanged?.Invoke();
                Logger.Info("Static list reloaded from server");
            }
            catch (Exception ex)
            {
                Logger.Error("Error reloading static list", ex);
            }
        }

        public void Dispose()
        {
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

