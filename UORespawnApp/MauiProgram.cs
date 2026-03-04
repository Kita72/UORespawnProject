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
            builder.Services.AddSingleton<CommandService>();
            builder.Services.AddSingleton<ServerUpdateService>();
            builder.Services.AddSingleton<BackgroundDataLoader>();
            builder.Services.AddSingleton<UpdateChecker>();
            builder.Services.AddSingleton<SpawnPackService>();
            builder.Services.AddSingleton<WebViewService>();
            builder.Services.AddSingleton<DebugService>();
            builder.Services.AddSingleton<MapImageCacheService>();

            // FTP & Account Services
            builder.Services.AddSingleton<AccountService>();
            builder.Services.AddSingleton<FtpCredentialService>();
            builder.Services.AddSingleton<FtpConnectionService>();
            builder.Services.AddSingleton<FtpSyncService>();

            // XML Spawner Management
            builder.Services.AddSingleton<XmlSpawnerCommandService>();

            try
            {
                Logger.Info($"UORespawn v{Utility.Version} - Starting minimal initialization...");

                // Validate configuration and create missing folders
                var validationResult = ConfigurationValidator.ValidateStartup();
                if (validationResult.HasErrors)
                {
                    Logger.Error("Configuration validation failed - app may not function correctly");
                }

                Utility.StartSession(new Session());
                Utility.InitializeSpawnDictionary();

                Logger.Info("Minimal initialization complete - UI ready to launch");
                Logger.Info("Settings and spawn data will load in background after UI renders");
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex, "Startup initialization", notifyUser: false);
                System.Diagnostics.Debug.WriteLine($"Error during initialization: {ex.Message}");
            }

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Wire up DebugService to Logger for in-app log visualization
            var debugService = app.Services.GetRequiredService<DebugService>();
            Logger.DebugService = debugService;

            // Initialize debug mode from settings
            if (Settings.IsDebugMode)
            {
                debugService.SetEnabled(true);
            }

            return app;
        }
    }
}
