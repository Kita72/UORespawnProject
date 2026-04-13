using Microsoft.Extensions.Logging;
using UORespawnApp.Scripts.Services;
using UORespawnApp.Scripts.Services.Platform;















































































































































</Project>  </ItemGroup>    </Content>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>    <Content Include="../UORespawnApp/Resources/Raw/UOR_TileList.txt" Link="Resources/Raw/UOR_TileList.txt">    </Content>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>    <Content Include="../UORespawnApp/Resources/Raw/UOR_MapList.txt" Link="Resources/Raw/UOR_MapList.txt">    </Content>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>    <Content Include="../UORespawnApp/Resources/Raw/UOR_HiveData.txt" Link="Resources/Raw/UOR_HiveData.txt">    </Content>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>    <Content Include="../UORespawnApp/Resources/Raw/UOR_SignData.txt" Link="Resources/Raw/UOR_SignData.txt">    </Content>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>    <Content Include="../UORespawnApp/Resources/Raw/UOR_VendorList.txt" Link="Resources/Raw/UOR_VendorList.txt">    </Content>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>    <Content Include="../UORespawnApp/Resources/Raw/UOR_BestiaryList.txt" Link="Resources/Raw/UOR_BestiaryList.txt">    </Content>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>    <Content Include="../UORespawnApp/Resources/Raw/UOR_RegionList.txt" Link="Resources/Raw/UOR_RegionList.txt">    </Content>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>    <Content Include="../UORespawnApp/Resources/Raw/UOR_SpawnerList.txt" Link="Resources/Raw/UOR_SpawnerList.txt">  <ItemGroup>  <!-- Resource/Raw reference data files -->  </ItemGroup>    </None>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>          Link="Data/UORespawn/%(Filename)%(Extension)">          Condition="Exists('../UORespawnApp/Data/UORespawn/')"    <None Include="../UORespawnApp/Data/UORespawn/*.*"    <!-- Working data directory -->    </None>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>          Link="Data/SERVER/MUO/UORespawnServer.zip">          Condition="Exists('../UORespawnApp/Data/SERVER/MUO/UORespawnServer.zip')"    <None Include="../UORespawnApp/Data/SERVER/MUO/UORespawnServer.zip"    </None>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>          Link="Data/SERVER/SERVUO/UORespawnServer.zip">          Condition="Exists('../UORespawnApp/Data/SERVER/SERVUO/UORespawnServer.zip')"    <None Include="../UORespawnApp/Data/SERVER/SERVUO/UORespawnServer.zip"    <!-- Server scripts (for export feature) -->    </None>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>          Link="Data/PACKS/Created/.gitkeep">          Condition="Exists('../UORespawnApp/Data/PACKS/Created/.gitkeep')"    <None Include="../UORespawnApp/Data/PACKS/Created/.gitkeep"    </None>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>          Link="Data/PACKS/Imported/.gitkeep">          Condition="Exists('../UORespawnApp/Data/PACKS/Imported/.gitkeep')"    <None Include="../UORespawnApp/Data/PACKS/Imported/.gitkeep"    </None>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>          Link="Data/PACKS/Approved/.gitkeep">          Condition="Exists('../UORespawnApp/Data/PACKS/Approved/.gitkeep')"    <None Include="../UORespawnApp/Data/PACKS/Approved/.gitkeep"    </None>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>          Link="Data/PACKS/Approved/DefaultPack/%(RecursiveDir)%(Filename)%(Extension)">          Condition="Exists('../UORespawnApp/Data/PACKS/Approved/DefaultPack/')"    <None Include="../UORespawnApp/Data/PACKS/Approved/DefaultPack/**/*.*"    </None>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>          Link="Data/PACKS/Approved/DefaultPack.zip">          Condition="Exists('../UORespawnApp/Data/PACKS/Approved/DefaultPack.zip')"    <None Include="../UORespawnApp/Data/PACKS/Approved/DefaultPack.zip"    <!-- Packs: DefaultPack -->    </None>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>    <None Include="../UORespawnApp/Data/TILES/*.png" Link="Data/TILES/%(Filename)%(Extension)">    <!-- Tiles -->    </None>      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>    <None Include="../UORespawnApp/Data/MAPS/*.bmp" Link="Data/MAPS/%(Filename)%(Extension)">    <!-- Maps -->    <Compile Remove="Data/**/*.cs" />    <!-- Exclude server .cs files from compilation -->  <ItemGroup>  <!-- Data files (maps, packs, tiles, server scripts) -->  </ItemGroup>             Exclude="../UORespawnApp/wwwroot/index.html" />             CopyToOutputDirectory="PreserveNewest"             Link="wwwroot/%(RecursiveDir)%(Filename)%(Extension)"    <Content Include="../UORespawnApp/wwwroot/**/*.*"  <ItemGroup>  <!-- wwwroot static assets (JS, CSS, fonts, videos) -->  </ItemGroup>             Link="Components/%(RecursiveDir)%(Filename)%(Extension)" />    <Content Include="../UORespawnApp/Components/**/*.razor.css"  <ItemGroup>  <!-- Scoped CSS for components -->  </ItemGroup>             Exclude="../UORespawnApp/Components/Routes.razor" />             Link="Components/%(RecursiveDir)%(Filename)%(Extension)"    <Content Include="../UORespawnApp/Components/**/*.razor"  <ItemGroup>  <!-- Blazor Components: All .razor files -->  </ItemGroup>             Exclude="../UORespawnApp/Scripts/Services/Platform/Maui*.cs" />             Link="Scripts/%(RecursiveDir)%(Filename)%(Extension)"    <Compile Include="../UORespawnApp/Scripts/**/*.cs"  <ItemGroup>  <!-- Scripts: All business logic, entities, services, utilities -->  <!-- ==================== Shared Source Files (linked from MAUI project) ==================== -->  </ItemGroup>    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="10.0.3" />    <PackageReference Include="Microsoft.Maui.Graphics" Version="10.0.41" />    <PackageReference Include="FluentFTP" Version="53.0.2" />    <PackageReference Include="Photino.Blazor" Version="4.0.13" />  <ItemGroup>  <!-- ==================== NuGet Packages ==================== -->  </PropertyGroup>    <Description>UORespawn Editor - Linux build via Photino.Blazor</Description>    <Product>UORespawn</Product>    <AssemblyTitle>UORespawn Editor (Linux)</AssemblyTitle>    <!-- Application metadata -->    <EnableDefaultCssItems>false</EnableDefaultCssItems>    <Nullable>enable</Nullable>    <ImplicitUsings>enable</ImplicitUsings>    <RootNamespace>UORespawnApp</RootNamespace>    <OutputType>Exe</OutputType>    <TargetFramework>net10.0</TargetFramework>using UORespawnApp.Scripts.Utilities;
using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts;

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

            // Platform abstraction services (MAUI implementations)
            var mauiPrefs = new MauiPreferencesService();
            PreferencesProvider.Initialize(mauiPrefs);
            builder.Services.AddSingleton<IPreferencesService>(mauiPrefs);
            builder.Services.AddSingleton<IPlatformDialogService, MauiDialogService>();

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

            // Spawn Pack Sync (keeps packs aligned when server data changes)
            builder.Services.AddSingleton<SpawnPackSyncService>();

            try
            {
                Logger.Info($"UORespawn v{Utility.Version} - Starting minimal initialization...");

                // Validate configuration and create missing folders
                var validationResult = ConfigurationValidator.ValidateStartup();
                if (validationResult.HasErrors)
                {
                    Logger.Error("Configuration validation failed - app may not function correctly");
                }

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
