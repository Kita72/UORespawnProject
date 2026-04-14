using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using UORespawnApp.Scripts;
using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Services;
using UORespawnApp.Scripts.Services.Platform;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Linux;

/// <summary>
/// Linux entry point using Photino.Blazor (WebKitGTK).
/// Mirrors MauiProgram.cs service registration.
/// </summary>
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Ensure wwwroot exists in output dir — Photino.Blazor creates a PhysicalFileProvider for it.
        // The actual assets are resolved via the static web assets manifest (pointing to source symlinks).
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"));

        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.RootComponents.Add<Routes>("#app");

        // Platform abstraction services (Linux implementations)
        var linuxPrefs = new LinuxPreferencesService();
        PreferencesProvider.Initialize(linuxPrefs);
        builder.Services.AddSingleton<IPreferencesService>(linuxPrefs);
        builder.Services.AddSingleton<IPlatformDialogService, LinuxDialogService>();

        // Core application services (same as MauiProgram.cs)
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
        builder.Services.AddSingleton<SpawnDataService>();
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<BinarySerializationService>();

        // FTP & Account Services
        builder.Services.AddSingleton<AccountService>();
        builder.Services.AddSingleton<FtpCredentialService>();
        builder.Services.AddSingleton<FtpConnectionService>();
        builder.Services.AddSingleton<FtpSyncService>();

        // XML Spawner Management
        builder.Services.AddSingleton<XmlSpawnerCommandService>();

        // Spawn Pack Sync
        builder.Services.AddSingleton<SpawnPackSyncService>();

        try
        {
            Logger.Info($"UORespawn v{Utility.Version} (Linux) - Starting initialization...");

            var validationResult = ConfigurationValidator.ValidateStartup();
            if (validationResult.HasErrors)
            {
                Logger.Error("Configuration validation failed - app may not function correctly");
            }

            Logger.Info("Initialization complete - UI ready to launch");
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle(ex, "Startup initialization", notifyUser: false);
            Console.Error.WriteLine($"Error during initialization: {ex.Message}");
        }

        var app = builder.Build();

        // Wire up DebugService to Logger
        var debugService = app.Services.GetRequiredService<DebugService>();
        Logger.DebugService = debugService;

        if (Settings.IsDebugMode)
        {
            debugService.SetEnabled(true);
        }

        // Initialize static Utility services (backward compatibility)
        var spawnDataService = app.Services.GetRequiredService<SpawnDataService>();
        var sessionService = app.Services.GetRequiredService<SessionService>();
        var mapImageCache = app.Services.GetRequiredService<MapImageCacheService>();
        var binarySerializationService = app.Services.GetRequiredService<BinarySerializationService>();
        var toastService = app.Services.GetRequiredService<ToastService>();
        Utility.SetServices(spawnDataService, sessionService, mapImageCache, binarySerializationService, toastService);

        // Enable file URL support for map images (avoids expensive base64 encoding)
        var platformService = app.Services.GetRequiredService<IPlatformDialogService>();
        mapImageCache.SetPlatformService(platformService);

        // Configure the Photino window
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
        app.MainWindow
            .SetTitle("UORespawn Editor")
            .SetUseOsDefaultSize(false)
            .SetSize(1600, 900)
            .SetResizable(true)
            .SetDevToolsEnabled(true)
            .SetIconFile(iconPath);

        // Save data on window closing
        app.MainWindow.WindowClosing += (sender, e) =>
        {
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

            Logger.Shutdown();
            return false; // allow close
        };

        // Start background data loading after window shows
        Task.Run(async () =>
        {
            await Task.Delay(500);
            var loader = app.Services.GetRequiredService<BackgroundDataLoader>();
            await loader.LoadAllDataAsync();
        });

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            Console.Error.WriteLine($"Unhandled exception: {error.ExceptionObject}");
        };

        app.Run();
    }
}
