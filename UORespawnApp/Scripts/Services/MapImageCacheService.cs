using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Caches map images as base64 data URLs to avoid repeated disk reads.
/// Map images (BMP files) can be several megabytes each - reading and converting
/// to base64 on every map switch causes noticeable lag.
/// 
/// Cache is invalidated when:
/// - A map image file is replaced via Settings
/// - The app is restarted
/// 
/// Memory usage: ~1.3x the file size per cached map (base64 overhead)
/// For typical UO maps this is 3-10 MB per map, which is acceptable for desktop.
/// </summary>
public class MapImageCacheService
{
    private readonly Dictionary<int, CachedMapImage> _cache = [];
    private readonly HashSet<int> _preloading = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// Gets the base64 data URL for a map image, using cache when available.
    /// </summary>
    /// <param name="mapId">The map ID to load</param>
    /// <returns>Base64 data URL (data:image/bmp;base64,...) or empty string if not found</returns>
    public string GetMapImageDataUrl(int mapId)
    {
        lock (_lock)
        {
            // Check cache first
            if (_cache.TryGetValue(mapId, out var cached))
            {
                var filePath = MapUtility.GetMapImagePath(mapId);
                var currentModified = GetFileModifiedTime(filePath);

                // Validate cache is still fresh (file hasn't changed)
                if (cached.FileModifiedTime == currentModified && !string.IsNullOrEmpty(cached.DataUrl))
                {
                    return cached.DataUrl;
                }

                // Cache is stale, remove it
                _cache.Remove(mapId);
                Logger.Info($"Map {mapId} cache invalidated (file changed)");
            }
        }

        // Load from disk
        return LoadAndCacheMap(mapId);
    }

    /// <summary>
    /// Loads a map image from disk and caches it.
    /// </summary>
    private string LoadAndCacheMap(int mapId)
    {
        try
        {
            var filePath = MapUtility.GetMapImagePath(mapId);

            if (!File.Exists(filePath))
            {
                Logger.Warning($"Map image not found: {filePath}");
                return "";
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Read file and convert to base64
            var bytes = File.ReadAllBytes(filePath);
            var base64 = Convert.ToBase64String(bytes);
            var dataUrl = $"data:image/bmp;base64,{base64}";

            stopwatch.Stop();

            // Cache the result
            lock (_lock)
            {
                _cache[mapId] = new CachedMapImage
                {
                    DataUrl = dataUrl,
                    FileModifiedTime = GetFileModifiedTime(filePath),
                    FileSizeBytes = bytes.Length
                };
            }

            Logger.Info($"Map {mapId} loaded from disk in {stopwatch.ElapsedMilliseconds}ms ({bytes.Length / 1024:N0} KB)");
            return dataUrl;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading map image for Map {mapId}", ex);
            return "";
        }
    }

    /// <summary>
    /// Invalidates the cache for a specific map (call after replacing map image).
    /// </summary>
    public void InvalidateMap(int mapId)
    {
        lock (_lock)
        {
            if (_cache.Remove(mapId))
            {
                Logger.Info($"Map {mapId} cache invalidated");
            }
        }
    }

    /// <summary>
    /// Clears the entire cache (call on app shutdown or memory pressure).
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            var count = _cache.Count;
            _cache.Clear();
            Logger.Info($"Map image cache cleared ({count} maps)");
        }
    }

    /// <summary>
    /// Preloads map images into cache (call during background loading).
    /// </summary>
    public void PreloadMaps(IEnumerable<int> mapIds)
    {
        foreach (var mapId in mapIds)
        {
            bool shouldLoad;
            lock (_lock)
            {
                shouldLoad = !_cache.ContainsKey(mapId) && _preloading.Add(mapId);
            }

            if (!shouldLoad) continue;

            try
            {
                LoadAndCacheMap(mapId);
            }
            finally
            {
                lock (_lock) { _preloading.Remove(mapId); }
            }
        }
    }

    /// <summary>
    /// Gets cache statistics for debugging.
    /// </summary>
    public (int CachedMaps, long TotalBytes) GetCacheStats()
    {
        lock (_lock)
        {
            var totalBytes = _cache.Values.Sum(c => c.FileSizeBytes);
            return (_cache.Count, totalBytes);
        }
    }

    private static DateTime GetFileModifiedTime(string filePath)
    {
        try
        {
            return File.Exists(filePath) ? File.GetLastWriteTimeUtc(filePath) : DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private class CachedMapImage
    {
        public string DataUrl { get; set; } = "";
        public DateTime FileModifiedTime { get; set; }
        public long FileSizeBytes { get; set; }
    }
}
