using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Utility for loading and managing the tile list (land/tile type names).
    /// The tile list is a READ-ONLY list of valid tile type names from the server.
    /// 
    /// Data Flow:
    /// - Default: Loads from Resources/Raw/UOR_TileList.txt (bundled with app)
    /// - Server Linked: DataWatcher copies server's tile list to Resources/Raw, then reloads
    /// - Users cannot edit the tile list - it's a verified list from the server
    /// </summary>
    internal static class TileListUtility
    {
        internal static List<string>? TileNameList { get; private set; }
        private static bool _isLoaded = false;

        /// <summary>
        /// Clear tile list to force reload from file.
        /// Called by DataWatcher when server updates the tile list file.
        /// </summary>
        internal static void ClearTileList()
        {
            TileNameList?.Clear();
            _isLoaded = false;
        }

        /// <summary>
        /// Load the tile list from Resources/Raw/UOR_TileList.txt
        /// </summary>
        internal static async Task LoadTileList(CancellationToken cancellationToken = default)
        {
            if (_isLoaded && TileNameList != null && TileNameList.Count > 0)
            {
                return; // Already loaded
            }

            TileNameList = [];

            try
            {
                var filePath = PathConstants.GetTileListFilePath();

                if (!File.Exists(filePath))
                {
                    Logger.Warning($"Tile list file not found at: {filePath}");
                    _isLoaded = true;
                    return;
                }

                var lines = await FileUtility.ReadAllLinesAsync(filePath, cancellationToken: cancellationToken);

                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
                    {
                        TileNameList.Add(line.Trim());
                    }
                }

                TileNameList.Sort();
                _isLoaded = true;

                Logger.Info($"Loaded {TileNameList.Count} tile types from tile list file");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading tile list", ex);
                _isLoaded = true; // Mark as loaded to prevent repeated attempts
            }
        }

        /// <summary>
        /// Check if a tile name exists in the loaded list.
        /// </summary>
        internal static bool IsValidTileName(string tileName)
        {
            if (TileNameList == null || string.IsNullOrWhiteSpace(tileName))
                return false;

            return TileNameList.Contains(tileName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get the tile list for UI binding (e.g., dropdown).
        /// Returns empty list if not loaded.
        /// </summary>
        internal static IReadOnlyList<string> GetTileList()
        {
            return TileNameList ?? [];
        }
    }
}
