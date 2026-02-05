using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp
{
    public partial class App : Application
    {
        private DataWatcher? _dataWatcher;
        
        public App()
        {
            InitializeComponent();
            
            // Start DataWatcher for server file sync
            InitializeDataWatcher();
        }
        
        private void InitializeDataWatcher()
        {
            try
            {
                _dataWatcher = new DataWatcher(() =>
                {
                    // Callback when server files change
                    Logger.Info("Server data files have been updated!");
                    // Could trigger UI refresh here if needed
                });
            }
            catch (Exception ex)
            {
                Logger.Warning($"DataWatcher failed to start: {ex.Message}");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) 
            { 
                Title = "UORespawn Editor",
                Width = 1400,
                Height = 900
            };
            
            // Maximize on startup (Windows only)
#if WINDOWS
            window.MaximumWidth = double.PositiveInfinity;
            window.MaximumHeight = double.PositiveInfinity;
            
            // Set to maximized state
            window.Created += (s, e) =>
            {
                var platformWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (platformWindow != null)
                {
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                        Microsoft.UI.Win32Interop.GetWindowIdFromWindow(
                            WinRT.Interop.WindowNative.GetWindowHandle(platformWindow)));
                    
                    if (appWindow != null)
                    {
                        var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                        if (presenter != null)
                        {
                            presenter.Maximize();
                        }
                    }
                }
            };
#endif
            
            // Clean up DataWatcher when window is destroyed
            window.Destroying += (s, e) =>
            {
                _dataWatcher?.Dispose();
            };
            
            return window;
        }
    }
}


