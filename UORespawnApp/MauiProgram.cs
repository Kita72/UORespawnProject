using Microsoft.Extensions.Logging;
using UORespawnApp.Scripts.Services;
using UORespawnApp.Scripts.Utilities;
using UORespawnApp.Scripts.Constants;

namespace UORespawnApp
{
    public static class MauiProgram
    {
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

            // Register ToastService for UI notifications
            builder.Services.AddSingleton<ToastService>();

            // Register BackgroundDataLoader as a singleton service
            builder.Services.AddSingleton<BackgroundDataLoader>();

            // Register UpdateChecker as a singleton service
            builder.Services.AddSingleton<UpdateChecker>();

            try
            {
                // MINIMAL STARTUP - Only critical initialization, no data loading!
                Logger.Info($"UORespawn v{Utility.Version} - Starting minimal initialization...");

                // Step 1: Ensure Data/UOR_DATA folder exists (centralized via PathConstants)
                var localDataFolder = PathConstants.LocalDataPath;
                Logger.Info($"Local data folder ready: {localDataFolder}");

                // Step 2: Initialize session and empty spawn dictionaries
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
