using Microsoft.Extensions.Logging;
using UORespawnApp.Scripts.Services;
using UORespawnApp.Scripts.Utilities;
using UORespawnApp.Scripts.Constants;

namespace UORespawnApp
{
    /// <summary>
    /// Application entry point and service configuration for UORespawn Editor.
    /// Configures MAUI services, Blazor WebView, and performs minimal startup initialization.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates and configures the MAUI application.
        /// Registers all services and performs minimal startup to allow fast UI rendering.
        /// </summary>
        /// <returns>Configured MauiApp instance</returns>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<ViewService>();
            builder.Services.AddSingleton<ToastService>();
            builder.Services.AddSingleton<BackgroundDataLoader>();
            builder.Services.AddSingleton<UpdateChecker>();
            builder.Services.AddSingleton<SpawnPackService>();

            try
            {
                Logger.Info($"UORespawn v{Utility.Version} - Starting minimal initialization...");

                var localDataFolder = PathConstants.LocalDataPath;
                Logger.Info($"Local data folder ready: {localDataFolder}");

                Utility.StartSession(new Session());
                Utility.InitializeSpawnDictionary();

                Logger.Info("Minimal initialization complete - UI ready to launch");
                Logger.Info("Settings and spawn data will load in background after UI renders");
            }
            catch (Exception ex)
            {
                Logger.Error("Error during minimal initialization", ex);
                System.Diagnostics.Debug.WriteLine($"Error during initialization: {ex.Message}");
            }

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
