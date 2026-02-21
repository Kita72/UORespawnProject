using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Enums;

namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Sign location data: SignType, Facing direction, and 3D coordinates
    /// </summary>
    internal readonly struct SignLocation
    {
        public SignTypes SignType { get; }
        public FacingTypes Facing { get; }
        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public SignLocation(SignTypes signType, FacingTypes facing, int x, int y, int z)
        {
            SignType = signType;
            Facing = facing;
            X = x;
            Y = y;
            Z = z;
        }
    }

    /// <summary>
    /// Utility for loading and managing sign data for vendor spawning.
    /// Signs indicate shop locations where vendors can be spawned.
    /// 
    /// Data Flow:
    /// - Default: Loads from Resources/Raw/UOR_SignData.txt (bundled with app)
    /// - Server Linked: DataWatcher copies server's sign data to Resources/Raw, then reloads
    /// 
    /// Format: MapID:SignType:FacingType:X:Y:Z
    /// Example: 0:Bank:North:595:2152:0
    /// </summary>
    internal static class SignDataUtility
    {
        /// <summary>
        /// Sign locations organized by map ID
        /// </summary>
        private static Dictionary<int, List<SignLocation>>? _signsByMap = null;
        private static bool _isLoaded = false;

        /// <summary>
        /// Get all sign locations for a specific map
        /// </summary>
        public static List<SignLocation> GetSignsForMap(int mapId)
        {
            EnsureLoaded();

            if (_signsByMap != null && _signsByMap.TryGetValue(mapId, out var signs))
            {
                return signs;
            }

            return [];
        }

        /// <summary>
        /// Get all sign locations across all maps
        /// </summary>
        public static Dictionary<int, List<SignLocation>> GetAllSigns()
        {
            EnsureLoaded();
            return _signsByMap ?? [];
        }

        /// <summary>
        /// Get total count of signs across all maps
        /// </summary>
        public static int GetTotalSignCount()
        {
            EnsureLoaded();
            return _signsByMap?.Values.Sum(list => list.Count) ?? 0;
        }

        /// <summary>
        /// Clear sign data to force reload from file.
        /// Called by DataWatcher when server updates the sign data file.
        /// </summary>
        public static void ClearSignData()
        {
            _signsByMap?.Clear();
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
        /// Load sign data from UOR_SignData.txt file
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_isLoaded) return;

            _signsByMap = [];

            try
            {
                var filePath = PathConstants.GetSignDataFilePath();

                if (!File.Exists(filePath))
                {
                    Logger.Warning($"Sign data file not found at: {filePath}");
                    _isLoaded = true;
                    return;
                }

                var lines = File.ReadAllLines(filePath);
                int loadedCount = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                        continue;

                    var parsed = ParseSignLine(line);
                    if (parsed != null)
                    {
                        var (mapId, signLocation) = parsed.Value;

                        if (!_signsByMap.TryGetValue(mapId, out var mapSigns))
                        {
                            mapSigns = [];
                            _signsByMap[mapId] = mapSigns;
                        }

                        mapSigns.Add(signLocation);
                        loadedCount++;
                    }
                }

                Logger.Info($"Loaded {loadedCount} sign locations across {_signsByMap.Count} maps from UOR_SignData.txt");
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading sign data", ex);
                _isLoaded = true; // Mark as loaded to prevent repeated attempts
            }
        }

        /// <summary>
        /// Parse a single sign data line from the file
        /// Format: MapID:SignType:FacingType:X:Y:Z
        /// Example: 0:Bank:North:595:2152:0
        /// </summary>
        private static (int mapId, SignLocation signLocation)? ParseSignLine(string line)
        {
            try
            {
                var parts = line.Split(':');
                if (parts.Length < 6)
                    return null;

                if (!int.TryParse(parts[0], out int mapId))
                    return null;

                if (!Enum.TryParse<SignTypes>(parts[1], ignoreCase: true, out var signType))
                    return null;

                if (!Enum.TryParse<FacingTypes>(parts[2], ignoreCase: true, out var facing))
                    return null;

                if (!int.TryParse(parts[3], out int x) ||
                    !int.TryParse(parts[4], out int y) ||
                    !int.TryParse(parts[5], out int z))
                    return null;

                return (mapId, new SignLocation(signType, facing, x, y, z));
            }
            catch
            {
                return null;
            }
        }
    }
}
