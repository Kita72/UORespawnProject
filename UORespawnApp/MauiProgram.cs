using Microsoft.Extensions.Logging;

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
                Utility.StartSession(new Session());
                Utility.InitializeSpawnDictionary();
                
                try { Utility.LoadSpawnData(); } catch { /* No spawn data file yet */ }
                try { WorldSpawnUtility.LoadWorldSpawnList(); } catch { /* No world spawn list yet */ }
                try { WorldSpawnUtility.LoadStaticSpawnList(); } catch { /* No static spawn list yet */ }
                try { XMLSpawnUtility.LoadSpawnerList(); } catch { /* No spawner list yet */ }
                try { XMLSpawnUtility.LoadStaticList(); } catch { /* No static list yet */ }

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
