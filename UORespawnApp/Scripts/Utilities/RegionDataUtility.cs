using UORespawnApp.Scripts.Entities;

namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Utility for loading and managing region data from UOR_RegionList.txt
    /// Regions are predefined areas from the server that can be assigned spawns
    /// </summary>
    public static class RegionDataUtility
    {
        private static Dictionary<int, List<RegionInfo>>? _regionsByMap = null;
        private static bool _isLoaded = false;

        /// <summary>
        /// Get all regions for a specific map
        /// </summary>
        public static List<RegionInfo> GetRegionsForMap(int mapId)
        {
            EnsureLoaded();

            if (_regionsByMap != null && _regionsByMap.TryGetValue(mapId, out var regions))
            {
                return regions;
            }

            return [];
        }

        /// <summary>
        /// Get a specific region by name and map
        /// </summary>
        public static RegionInfo? GetRegion(int mapId, string regionName)
        {
            EnsureLoaded();

            var regions = GetRegionsForMap(mapId);
            return regions.FirstOrDefault(r => 
                r.Name.Equals(regionName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check if a point is within any region on a map
        /// If multiple regions overlap at this point, returns the smallest one (by total area)
        /// This allows selecting smaller regions that are inside larger regions
        /// </summary>
        public static RegionInfo? FindRegionAt(int mapId, int x, int y)
        {
            EnsureLoaded();

            var regions = GetRegionsForMap(mapId);

            // Find all regions that contain this point
            var matchingRegions = regions.Where(r => r.Contains(x, y)).ToList();

            if (matchingRegions.Count == 0)
                return null;

            // If only one match, return it
            if (matchingRegions.Count == 1)
                return matchingRegions[0];

            // Multiple regions overlap - return the smallest one (prioritize smaller regions)
            return matchingRegions.OrderBy(r => r.GetTotalArea()).First();
        }

        /// <summary>
        /// Clear region data to force reload from server-generated file
        /// </summary>
        public static void ClearRegionData()
        {
            _regionsByMap?.Clear();
            _isLoaded = false;
        }

        /// <summary>
        /// Async version of EnsureLoaded for DataWatcher reload scenarios
        /// </summary>
        public static async Task EnsureLoadedAsync()
        {
            await Task.Run(() => EnsureLoaded());
        }

        /// <summary>
        /// Load regions from UOR_RegionList.txt file
        /// Groups rectangles by region name (same name = multiple rects for one region)
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_isLoaded) return;

            _regionsByMap = [];

            try
            {
                // Try to load from Resources/Raw folder
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "UOR_RegionList.txt");

                if (!File.Exists(filePath))
                {
                    Logger.Warning($"UOR_RegionList.txt not found at: {filePath}");
                    _isLoaded = true;
                    return;
                }

                var lines = File.ReadAllLines(filePath);

                // Temporary dictionary to group rects by map and name
                var tempRegions = new Dictionary<int, Dictionary<string, List<Rect>>>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                        continue;

                    var parsed = ParseRegionLine(line);
                    if (parsed != null)
                    {
                        var (mapId, name, rect) = parsed.Value;

                        // Ensure map exists
                        if (!tempRegions.TryGetValue(mapId, out Dictionary<string, List<Rect>>? value))
                        {
                            value = new Dictionary<string, List<Rect>>(StringComparer.OrdinalIgnoreCase);
                            tempRegions[mapId] = value;
                        }

                        // Ensure region name exists
                        if (!value.TryGetValue(name, out List<Rect>? value1))
                        {
                            value1 = [];
                            value[name] = value1;
                        }

                        value1.Add(rect);
                    }
                }

                // Convert to RegionInfo list
                foreach (var mapKvp in tempRegions)
                {
                    _regionsByMap[mapKvp.Key] = [];

                    foreach (var regionKvp in mapKvp.Value)
                    {
                        _regionsByMap[mapKvp.Key].Add(new RegionInfo
                        {
                            Name = regionKvp.Key,
                            MapId = mapKvp.Key,
                            Rectangles = regionKvp.Value
                        });
                    }
                }

                var totalRegions = _regionsByMap.Values.Sum(list => list.Count);
                var totalRects = _regionsByMap.Values.Sum(list => list.Sum(r => r.Rectangles.Count));
                Logger.Info($"Loaded {totalRegions} unique regions with {totalRects} rectangles from UOR_RegionList.txt across {_regionsByMap.Count} maps");
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading region list", ex);
                _isLoaded = true; // Mark as loaded to prevent repeated attempts
            }
        }

        /// <summary>
        /// Parse a single region line from the file
        /// Format: MapID:RegionName:(X,Y,W,H)
        /// Example: 0:Britain:(1416,1498,324,279)
        /// Returns (mapId, regionName, rect)
        /// </summary>
        private static (int mapId, string name, Rect rect)? ParseRegionLine(string line)
        {
            try
            {
                // Split by colon
                var parts = line.Split(':');
                if (parts.Length < 3)
                    return null;

                // Parse MapID
                if (!int.TryParse(parts[0], out int mapId))
                    return null;

                // Parse Region Name (everything between first and last colon)
                var regionName = string.Join(":", parts.Skip(1).Take(parts.Length - 2));

                // Parse bounds (X,Y,W,H)
                var boundsStr = parts[^1].Trim('(', ')');
                var coords = boundsStr.Split(',');
                if (coords.Length != 4)
                    return null;

                if (!int.TryParse(coords[0], out int x) ||
                    !int.TryParse(coords[1], out int y) ||
                    !int.TryParse(coords[2], out int width) ||
                    !int.TryParse(coords[3], out int height))
                {
                    return null;
                }

                var rect = new Rect(x, y, width, height);
                return (mapId, regionName, rect);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to parse region line: {line} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reload regions from file (useful after updates)
        /// </summary>
        public static void Reload()
        {
            _isLoaded = false;
            _regionsByMap = null;
            EnsureLoaded();
        }

        /// <summary>
        /// Get unique region names for a map (for display/selection)
        /// </summary>
        public static List<string> GetRegionNames(int mapId)
        {
            var regions = GetRegionsForMap(mapId);
            return [.. regions
                .Select(r => r.Name)
                .Distinct()
                .OrderBy(n => n)];
        }

        /// <summary>
        /// Check if any regions are loaded
        /// </summary>
        public static bool HasRegions()
        {
            EnsureLoaded();
            return _regionsByMap != null && _regionsByMap.Count != 0;
        }
    }
}
