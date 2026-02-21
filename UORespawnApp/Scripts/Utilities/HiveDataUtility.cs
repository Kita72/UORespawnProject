using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Hive location data: 3D coordinates for bee hive locations
    /// </summary>
    internal readonly struct HiveLocation
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public HiveLocation(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    /// <summary>
    /// Utility for loading and managing hive data for beekeeper vendor spawning.
    /// Hives indicate locations where beekeeper vendors can be spawned.
    /// 
    /// Data Flow:
    /// - Default: Loads from Resources/Raw/UOR_HiveData.txt (bundled with app)
    /// - Server Linked: DataWatcher copies server's hive data to Resources/Raw, then reloads
    /// 
    /// Format: MapID:X:Y:Z
    /// Example: 0:593:2123:6
    /// </summary>
    internal static class HiveDataUtility
    {
        /// <summary>
        /// Hive locations organized by map ID
        /// </summary>
        private static Dictionary<int, List<HiveLocation>>? _hivesByMap = null;
        private static bool _isLoaded = false;

        /// <summary>
        /// Get all hive locations for a specific map
        /// </summary>
        public static List<HiveLocation> GetHivesForMap(int mapId)
        {
            EnsureLoaded();

            if (_hivesByMap != null && _hivesByMap.TryGetValue(mapId, out var hives))
            {
                return hives;
            }

            return [];
        }

        /// <summary>
        /// Get all hive locations across all maps
        /// </summary>
        public static Dictionary<int, List<HiveLocation>> GetAllHives()
        {
            EnsureLoaded();
            return _hivesByMap ?? [];
        }

        /// <summary>
        /// Get total count of hives across all maps
        /// </summary>
        public static int GetTotalHiveCount()
        {
            EnsureLoaded();
            return _hivesByMap?.Values.Sum(list => list.Count) ?? 0;
        }

        /// <summary>
        /// Clear hive data to force reload from file.
        /// Called by DataWatcher when server updates the hive data file.
        /// </summary>
        public static void ClearHiveData()
        {
            _hivesByMap?.Clear();
            _isLoaded = false;
        }

        /// <summary>
        /// Async version of EnsureLoaded for DataWatcher reload scenarios
        /// </summary>
        public static async Task EnsureLoadedAsync()
        {
            await Task.Run(EnsureLoaded);
        }

        /// <summary>
        /// Load hive data from UOR_HiveData.txt file
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_isLoaded) return;

            _hivesByMap = [];

            try
            {
                var filePath = PathConstants.GetHiveDataFilePath();

                if (!File.Exists(filePath))
                {
                    Logger.Warning($"Hive data file not found at: {filePath}");
                    _isLoaded = true;
                    return;
                }

                var lines = File.ReadAllLines(filePath);
                int loadedCount = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                        continue;

                    var parsed = ParseHiveLine(line);
                    if (parsed != null)
                    {
                        var (mapId, hiveLocation) = parsed.Value;

                        if (!_hivesByMap.TryGetValue(mapId, out var mapHives))
                        {
                            mapHives = [];
                            _hivesByMap[mapId] = mapHives;
                        }

                        mapHives.Add(hiveLocation);
                        loadedCount++;
                    }
                }

                Logger.Info($"Loaded {loadedCount} hive locations across {_hivesByMap.Count} maps from UOR_HiveData.txt");
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading hive data", ex);
                _isLoaded = true; // Mark as loaded to prevent repeated attempts
            }
        }

        /// <summary>
        /// Parse a single hive data line from the file
        /// Format: MapID:X:Y:Z
        /// Example: 0:593:2123:6
        /// </summary>
        private static (int mapId, HiveLocation hiveLocation)? ParseHiveLine(string line)
        {
            try
            {
                var parts = line.Split(':');
                if (parts.Length < 4)
                    return null;

                if (!int.TryParse(parts[0], out int mapId))
                    return null;

                if (!int.TryParse(parts[1], out int x) ||
                    !int.TryParse(parts[2], out int y) ||
                    !int.TryParse(parts[3], out int z))
                    return null;

                return (mapId, new HiveLocation(x, y, z));
            }
            catch
            {
                return null;
            }
        }
    }
}
