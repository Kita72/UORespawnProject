using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Utility for loading and managing the map list (map IDs and names).
    /// The map list is a READ-ONLY list of valid maps from the server.
    /// 
    /// Data Flow:
    /// - Default: Loads from Resources/Raw/UOR_MapList.txt (bundled with app)
    /// - Server Linked: DataWatcher copies server's map list to Resources/Raw, then reloads
    /// - Server provides verified map IDs including custom maps (6+)
    /// 
    /// File Format:
    /// (MapId, MapName)
    /// Example: (0, Felucca), (1, Trammel), (6, CustomMap)
    /// </summary>
    internal static class MapListUtility
    {
        /// <summary>
        /// Dictionary of MapId -> MapName
        /// </summary>
        internal static Dictionary<int, string>? MapList { get; private set; }
        
        private static bool _isLoaded = false;

        /// <summary>
        /// Clear map list to force reload from file.
        /// Called by DataWatcher when server updates the map list file.
        /// </summary>
        internal static void ClearMapList()
        {
            MapList?.Clear();
            _isLoaded = false;
        }

        /// <summary>
        /// Load the map list from Resources/Raw/UOR_MapList.txt
        /// </summary>
        internal static async Task LoadMapList(CancellationToken cancellationToken = default)
        {
            if (_isLoaded && MapList != null && MapList.Count > 0)
            {
                return; // Already loaded
            }

            MapList = [];

            try
            {
                var filePath = PathConstants.GetMapListFilePath();

                if (!File.Exists(filePath))
                {
                    Logger.Warning($"Map list file not found at: {filePath}");
                    LoadDefaultMaps();
                    _isLoaded = true;
                    return;
                }

                var lines = await FileUtility.ReadAllLinesAsync(filePath, cancellationToken: cancellationToken);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                        continue;

                    // Parse format: (MapId, MapName)
                    var parsed = ParseMapLine(line);
                    if (parsed.HasValue)
                    {
                        MapList[parsed.Value.MapId] = parsed.Value.MapName;
                    }
                }

                if (MapList.Count == 0)
                {
                    Logger.Warning("Map list file was empty, loading defaults");
                    LoadDefaultMaps();
                }

                _isLoaded = true;
                Logger.Info($"Loaded {MapList.Count} maps from map list file");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading map list", ex);
                LoadDefaultMaps();
                _isLoaded = true;
            }
        }

        /// <summary>
        /// Parse a map line in format: (MapId, MapName)
        /// </summary>
        private static (int MapId, string MapName)? ParseMapLine(string line)
        {
            try
            {
                // Remove parentheses and split
                var trimmed = line.Trim().TrimStart('(').TrimEnd(')');
                var parts = trimmed.Split(',', 2);

                if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0].Trim(), out int mapId))
                    {
                        var mapName = parts[1].Trim();
                        if (!string.IsNullOrEmpty(mapName))
                        {
                            return (mapId, mapName);
                        }
                    }
                }
            }
            catch
            {
                // Parsing failed
            }

            return null;
        }

        /// <summary>
        /// Load default maps as fallback when file not available.
        /// </summary>
        private static void LoadDefaultMaps()
        {
            MapList ??= [];
            MapList[0] = "Felucca";
            MapList[1] = "Trammel";
            MapList[2] = "Ilshenar";
            MapList[3] = "Malas";
            MapList[4] = "Tokuno";
            MapList[5] = "Ter Mur";
        }

        /// <summary>
        /// Get the map name for a given map ID.
        /// Returns "Unknown" if not found.
        /// </summary>
        internal static string GetMapName(int mapId)
        {
            if (MapList != null && MapList.TryGetValue(mapId, out var name))
            {
                return name;
            }
            return "Unknown";
        }

        /// <summary>
        /// Get all valid map IDs.
        /// </summary>
        internal static IEnumerable<int> GetMapIds()
        {
            if (MapList == null || MapList.Count == 0)
                return [];

            return MapList.Keys.OrderBy(k => k);
        }

        /// <summary>
        /// Get the highest valid map ID.
        /// Used to determine if custom maps exist (> 5).
        /// </summary>
        internal static int GetMaxMapId()
        {
            if (MapList == null || MapList.Count == 0)
                return 5;

            return MapList.Keys.Max();
        }

        /// <summary>
        /// Check if a map ID is valid.
        /// </summary>
        internal static bool IsValidMapId(int mapId)
        {
            return MapList?.ContainsKey(mapId) ?? false;
        }

        /// <summary>
        /// Get the total number of maps available.
        /// </summary>
        internal static int GetMapCount()
        {
            return MapList?.Count ?? 0;
        }

        /// <summary>
        /// Check if custom maps exist (map ID > 5).
        /// </summary>
        internal static bool HasCustomMaps()
        {
            return GetMaxMapId() > 5;
        }
    }
}
