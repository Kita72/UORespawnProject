using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Services;

namespace UORespawnApp.Scripts.Utilities;

/// <summary>
/// Core utility class for UORespawn v2.0
/// 
/// ARCHITECTURE NOTE:
/// This class now delegates to DI-injectable services (SpawnDataService, SessionService).
/// The static properties/methods remain for backward compatibility during migration.
/// New code should inject services directly via DI.
/// 
/// Migration path:
/// 1. Services are set during app startup via SetServices()
/// 2. Static Utility.BoxSpawns etc. delegate to SpawnDataService
/// 3. Components can use either pattern (both access same data)
/// 4. Eventually, direct service injection is preferred
/// </summary>
public static class Utility
{
    internal const string Version = "2.0.1.3";

    // ==================== SERVICE REFERENCES ====================
    // These get set during app startup from DI container

    private static SpawnDataService? _spawnDataService;
    private static SessionService? _sessionService;
    private static MapImageCacheService? _mapImageCache;
    private static BinarySerializationService? _binarySerializationService;

    /// <summary>
    /// Initializes the static utility with DI services.
    /// Called from MainPage during app startup.
    /// </summary>
    internal static void SetServices(
        SpawnDataService spawnDataService,
        SessionService sessionService,
        MapImageCacheService mapImageCache,
        BinarySerializationService binarySerializationService,
        ToastService toastService)
    {
        _spawnDataService = spawnDataService;
        _sessionService = sessionService;
        _mapImageCache = mapImageCache;
        _binarySerializationService = binarySerializationService;
        ErrorHandler.ToastService = toastService;
        Logger.Info("Utility services initialized from DI");
    }

    // ==================== SESSION (delegates to SessionService) ====================

    /// <summary>
    /// Current session - delegates to SessionService.
    /// Prefer injecting SessionService directly in new code.
    /// </summary>
#pragma warning disable CS0618 // Obsolete warning suppressed for internal compatibility
    internal static Session? SESSION => _sessionService?.GetSession();
#pragma warning restore CS0618

    /// <summary>
    /// Map image cache service reference.
    /// </summary>
    internal static MapImageCacheService? MapImageCache => _mapImageCache;

    // ==================== SPAWN DATA (delegates to SpawnDataService) ====================

    /// <summary>
    /// Box spawn data - delegates to SpawnDataService.
    /// Prefer injecting SpawnDataService directly in new code.
    /// </summary>
    internal static Dictionary<int, List<BoxSpawnEntity>> BoxSpawns => 
        _spawnDataService?.BoxSpawns ?? [];

    /// <summary>
    /// Tile spawn data - delegates to SpawnDataService.
    /// </summary>
    internal static Dictionary<int, List<TileSpawnEntity>> TileSpawns => 
        _spawnDataService?.TileSpawns ?? [];

    /// <summary>
    /// Region spawn data - delegates to SpawnDataService.
    /// </summary>
    internal static Dictionary<int, List<RegionSpawnEntity>> RegionSpawns => 
        _spawnDataService?.RegionSpawns ?? [];

    /// <summary>
    /// Vendor spawn data - delegates to SpawnDataService.
    /// </summary>
    internal static Dictionary<int, List<VendorEntity>> VendorSpawns => 
        _spawnDataService?.VendorSpawns ?? [];

    /// <summary>
    /// Initialize all spawn dictionaries - delegates to SpawnDataService.
    /// </summary>
    internal static void InitializeSpawnDictionary()
    {
        _spawnDataService?.InitializeAllSpawns();
    }

    #region Box Spawn Methods (delegate to SpawnDataService)

    internal static void InitializeBoxSpawns() => _spawnDataService?.InitializeBoxSpawns();

    internal static void AddBoxSpawn(int map, BoxSpawnEntity entity) => 
        _spawnDataService?.AddBoxSpawn(map, entity);

    internal static void SaveSpawnData()
    {
        try
        {
            _binarySerializationService?.SaveBoxSpawns();
        }
        catch (Exception ex)
        {
            Logger.Error("Error saving box spawn data", ex);
        }
    }

    internal static void LoadBoxSpawnData()
    {
        try
        {
            _spawnDataService?.ClearBoxSpawns();
            _spawnDataService?.InitializeBoxSpawns();
            _binarySerializationService?.LoadBoxSpawns();
        }
        catch (Exception ex)
        {
            Logger.Error("Error loading box spawn data", ex);
        }
    }

    #endregion

    #region Tile Spawn Methods (delegate to SpawnDataService)

    internal static void InitializeTileSpawns() => _spawnDataService?.InitializeTileSpawns();

    internal static void SaveTileSpawnData()
    {
        try
        {
            _binarySerializationService?.SaveTileSpawns();
        }
        catch (Exception ex)
        {
            Logger.Error("Error saving tile spawn data", ex);
        }
    }

    internal static void LoadTileSpawnData()
    {
        try
        {
            _spawnDataService?.ClearTileSpawns();
            _spawnDataService?.InitializeTileSpawns();
            _binarySerializationService?.LoadTileSpawns();
        }
        catch (Exception ex)
        {
            Logger.Error("Error loading tile spawn data", ex);
        }
    }

    #endregion

    #region Region Spawn Methods (delegate to SpawnDataService)

    internal static void InitializeRegionSpawns() => _spawnDataService?.InitializeRegionSpawns();

    internal static void SaveRegionSpawnData()
    {
        try
        {
            _binarySerializationService?.SaveRegionSpawns();
        }
        catch (Exception ex)
        {
            Logger.Error("Error saving region spawn data", ex);
        }
    }

    internal static void LoadRegionSpawnData()
    {
        try
        {
            _spawnDataService?.ClearRegionSpawns();
            _spawnDataService?.InitializeRegionSpawns();
            _binarySerializationService?.LoadRegionSpawns();
        }
        catch (Exception ex)
        {
            Logger.Error("Error loading region spawn data", ex);
        }
    }

    #endregion

    #region Vendor Spawn Methods (delegate to SpawnDataService)

    internal static void InitializeVendorSpawns() => _spawnDataService?.InitializeVendorSpawns();

    internal static void SaveVendorSpawnData()
    {
        try
        {
            _binarySerializationService?.SaveVendorSpawns();
        }
        catch (Exception ex)
        {
            Logger.Error("Error saving vendor spawn data", ex);
        }
    }

    internal static void LoadVendorSpawnData()
    {
        try
        {
            _spawnDataService?.ClearVendorSpawns();
            _spawnDataService?.InitializeVendorSpawns();
            _binarySerializationService?.LoadVendorSpawns();
        }
        catch (Exception ex)
        {
            Logger.Error("Error loading vendor spawn data", ex);
        }
    }

    #endregion

    #region Settings Methods

    internal static void SaveSettings()
    {
        try
        {
            _binarySerializationService?.SaveSettings();
        }
        catch (Exception ex)
        {
            Logger.Error("Error saving settings", ex);
        }
    }

    internal static void LoadSettings()
    {
        try
        {
            _binarySerializationService?.LoadSettings();
        }
        catch (Exception ex)
        {
            Logger.Error("Error loading settings", ex);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets map image as base64 data URL for current session map.
    /// Delegates to SessionService which uses MapImageCacheService.
    /// </summary>
    internal static string GetMapImagePath()
    {
        return _sessionService?.GetMapImageDataUrl() ?? "";
    }

    #endregion
}
