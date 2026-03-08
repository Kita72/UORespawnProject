using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Services;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp
{
    /// <summary>
    /// Main application class for UORespawn Editor.
    /// Handles application lifecycle, window creation, and background data loading initialization.
    /// </summary>
    public partial class App : Application
    {
        private readonly BackgroundDataLoader _backgroundLoader;

        /// <summary>
        /// Initializes the application with dependency-injected services.
        /// </summary>
        /// <param name="backgroundLoader">Service for loading application data in the background</param>
        public App(BackgroundDataLoader backgroundLoader)
        {
            InitializeComponent();

            _backgroundLoader = backgroundLoader;

            // Delay background loading to ensure UI is fully rendered before any work begins
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(300); // Increased from 100ms to allow window to fully appear
                await _backgroundLoader.LoadAllDataAsync();
            });
        }

        /// <summary>
        /// Creates and configures the main application window.
        /// </summary>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) 
            { 
                Title = "UORespawn Editor"
            };

#if WINDOWS
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
                            presenter.IsResizable = false;
                        }

                        // When user clicks maximize button (toggles to Restored), snap back to Maximized
                        appWindow.Changed += (appWin, args) =>
                        {
                            var p = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                            if (p != null && p.State == Microsoft.UI.Windowing.OverlappedPresenterState.Restored)
                            {
                                p.Maximize();
                            }
                        };
                    }
                }
            };
#endif

            window.Destroying += (s, e) =>
            {
                // Save all data before closing
                // Suppress pack sync to prevent re-serialized bytes from marking pack as modified
                try
                {
                    PathConstants.SuppressPackSync = true;
                    Utility.SaveSettings();
                    Utility.SaveSpawnData();
                    Utility.SaveTileSpawnData();
                    Utility.SaveRegionSpawnData();
                    Utility.SaveVendorSpawnData();
                    Logger.Info("Application closing - all data saved");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error saving on close", ex);
                }

                _backgroundLoader?.Dispose();
                Logger.Shutdown();
            };

            return window;
        }
    }
}
