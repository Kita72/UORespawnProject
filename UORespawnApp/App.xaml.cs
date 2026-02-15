using UORespawnApp.Scripts.Services;

namespace UORespawnApp
{
    public partial class App : Application
    {
        private readonly BackgroundDataLoader _backgroundLoader;
        
        public App(BackgroundDataLoader backgroundLoader)
        {
            InitializeComponent();
            
            _backgroundLoader = backgroundLoader;
            
            // Start background data loading after constructor completes
            // This allows the UI to render first
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(100); // Brief delay to ensure UI is rendered
                await _backgroundLoader.LoadAllDataAsync();
            });
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

                        presenter?.Maximize();
                    }
                }
            };
#endif
            
            // Clean up BackgroundDataLoader when window is destroyed
            window.Destroying += (s, e) =>
            {
                _backgroundLoader?.Dispose();
            };
            
            return window;
        }
    }
}




