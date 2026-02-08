using Microsoft.Extensions.Logging;
using UORespawnApp.Scripts.Services;
using UORespawnApp.Scripts.Utilities;

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
            
            // Register BackgroundDataLoader as a singleton service
            builder.Services.AddSingleton<BackgroundDataLoader>();

            try
            {
                // MINIMAL STARTUP - Only critical initialization, no data loading!
                Logger.Info($"UORespawn v{Utility.Version} - Starting minimal initialization...");
                
                // Step 1: Ensure Data folder exists
                var localDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(localDataFolder))
                {
                    Directory.CreateDirectory(localDataFolder);
                    Logger.Info("Created Data folder");
                }
                
                // Step 2: Create empty CSV files if they don't exist
                var requiredFiles = new[]
                {
                    Path.Combine(localDataFolder, "UOR_Spawn.csv"),
                    Path.Combine(localDataFolder, "UOR_WorldSpawn.csv"),
                    Path.Combine(localDataFolder, "UOR_StaticSpawn.csv"),
                    Path.Combine(localDataFolder, "UOR_SpawnSettings.csv")
                };
                
                foreach (var file in requiredFiles)
                {
                    if (!File.Exists(file))
                    {
                        File.Create(file).Dispose();
                        Logger.Info($"Created empty CSV file: {Path.GetFileName(file)}");
                    }
                }
                
                // Step 3: Initialize session and empty spawn dictionary
                Utility.StartSession(new Session());
                Utility.InitializeSpawnDictionary();
                
                Logger.Info("Minimal initialization complete - UI ready to launch");
                Logger.Info("Data loading will continue in background after UI renders");
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
