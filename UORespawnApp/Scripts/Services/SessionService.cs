using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Service for managing application session state.
/// Replaces the static Utility.SESSION pattern with DI-injectable service.
/// Registered as a singleton in DI for shared state across components.
/// </summary>
public class SessionService
{
    private readonly Session _session;
    private readonly MapImageCacheService? _mapImageCache;

    /// <summary>
    /// Creates a new SessionService with optional map image cache.
    /// </summary>
    public SessionService(MapImageCacheService? mapImageCache = null)
    {
        _session = new Session();
        _mapImageCache = mapImageCache;
        Logger.Info("SessionService initialized");
    }

    /// <summary>
    /// Currently selected map ID (0-5 for standard maps, 6+ for custom).
    /// </summary>
    public int CurrentMapId
    {
        get => _session.CurrentMap;
        set => _session.CurrentMap = value;
    }

    /// <summary>
    /// Last selected tile name in Tile Spawn page.
    /// </summary>
    public string? LastSelectedTile
    {
        get => _session.LastSelectedTile;
        set => _session.LastSelectedTile = value;
    }

    /// <summary>
    /// Whether the Tile Spawn page has been visited in this session.
    /// </summary>
    public bool HasVisitedTileSpawnPage => _session.HasVisitedTileSpawnPage;

    /// <summary>
    /// Gets the map image as a base64 data URL for the current map.
    /// Uses caching to avoid repeated disk reads.
    /// </summary>
    /// <returns>Base64 data URL or empty string if not found</returns>
    public string GetMapImageDataUrl()
    {
        if (!MapUtility.IsValidMapId(CurrentMapId))
        {
            return "";
        }

        // Use cache service if available
        if (_mapImageCache != null)
        {
            return _mapImageCache.GetMapImageDataUrl(CurrentMapId);
        }

        // Fallback: direct load without caching
        try
        {
            var fullPath = MapUtility.GetMapImagePath(CurrentMapId);

            if (File.Exists(fullPath))
            {
                var bytes = File.ReadAllBytes(fullPath);
                var base64 = Convert.ToBase64String(bytes);

                Logger.Warning("Map loaded without cache - MapImageCacheService not available");
                return $"data:image/bmp;base64,{base64}";
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading map image for Map{CurrentMapId}", ex);
        }

        return "";
    }

    /// <summary>
    /// Gets the underlying Session object.
    /// Prefer using SessionService properties, but this provides compatibility
    /// during migration from static Utility.SESSION pattern.
    /// </summary>
    [Obsolete("Use SessionService properties directly. This is for migration compatibility only.")]
    public Session GetSession() => _session;
}
