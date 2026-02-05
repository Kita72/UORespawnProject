using Microsoft.Extensions.Logging;
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

            try
            {
                // Ensure Data folder and all required CSV files exist before loading
                var localDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(localDataFolder))
                {
                    Directory.CreateDirectory(localDataFolder);
                }
                
                // Create empty CSV files if they don't exist
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
                
                // Initialize session and load spawn data
                Utility.StartSession(new Session());
                Utility.InitializeSpawnDictionary();
                
                try 
                { 
                    Utility.LoadSpawnData(); 
                } 
                catch (Exception ex) 
                { 
                    Logger.Error("LoadSpawnData failed", ex);
                }
                
                // Load world and static spawn data synchronously (avoids async deadlock)
                try 
                {
                    WorldSpawnUtility.LoadWorldSpawnListSync();
                } 
                catch (Exception ex) 
                {
                    Logger.Error("LoadWorldSpawnList failed", ex);
                }
                
                try 
                {
                    WorldSpawnUtility.LoadStaticSpawnListSync();
                } 
                catch (Exception ex) 
                {
                    Logger.Error("LoadStaticSpawnList failed", ex);
                }
                
                try 
                { 
                    XMLSpawnUtility.LoadSpawnerList(); 
                } 
                catch (Exception ex) 
                { 
                    Logger.Error("LoadSpawnerList failed", ex);
                }
                
                try 
                { 
                    XMLSpawnUtility.LoadStaticList(); 
                } 
                catch (Exception ex) 
                { 
                    Logger.Error("LoadStaticList failed", ex);
                }
                
                // Load bestiary (creature list) synchronously
                try
                {
                    // Use sync wrapper to avoid async deadlock
                    Task.Run(async () => await WorldSpawnUtility.LoadSpawnList()).Wait();
                }
                catch (Exception ex)
                {
                    Logger.Error("LoadSpawnList (bestiary) failed", ex);
                }
                
                Logger.Info($"App initialized - Version {Utility.Version}");
                
                var totalSpawns = Utility.Spawns.Values.Sum(list => list.Count);
                Logger.Info($"Loaded {totalSpawns} total spawn boxes across all maps");

                // Copy map images to wwwroot
                var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "maps");
                Directory.CreateDirectory(wwwrootPath);
                var dataPath = Path.Combine(AppContext.BaseDirectory, "Data");
                if (Directory.Exists(dataPath))
                {
                    foreach (var file in Directory.GetFiles(dataPath, "*.bmp"))
                    {
                        try
                        {
                            var dest = Path.Combine(wwwrootPath, Path.GetFileName(file));
                            if (!File.Exists(dest))
                            {
                                File.Copy(file, dest);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error copying map file: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
