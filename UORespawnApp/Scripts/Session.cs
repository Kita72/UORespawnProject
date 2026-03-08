namespace UORespawnApp
{
    /// <summary>
    /// Session state manager for in-memory application state.
    /// Tracks map selection and UI component states during runtime.
    /// State is lost on app restart (not persisted).
    /// Thread-safe: all property access is synchronized via lock.
    /// </summary>
    public class Session
    {
        private readonly Lock _lock = new();

        // ==================== MAP STATE ====================

        private int _currentMap = 0; // Default to Map0 (Felucca)

        /// <summary>
        /// Currently selected map (0-5 for standard maps, 6+ for custom).
        /// Thread-safe property.
        /// </summary>
        public int CurrentMap
        {
            get { lock (_lock) { return _currentMap; } }
            set { lock (_lock) { _currentMap = value; } }
        }

        // ==================== UI STATE (In-Memory Only) ====================

        private string? _lastSelectedTile = null;

        /// <summary>
        /// Last selected tile name in Tile Spawn page (null = not yet visited).
        /// Uses string to match file-based tile list from UOR_TileList.txt.
        /// Persists during session to maintain user's position.
        /// Thread-safe property.
        /// </summary>
        public string? LastSelectedTile
        {
            get { lock (_lock) { return _lastSelectedTile; } }
            set { lock (_lock) { _lastSelectedTile = value; } }
        }

        /// <summary>
        /// Check if Tile Spawn page has been visited in this session.
        /// Thread-safe property.
        /// </summary>
        public bool HasVisitedTileSpawnPage
        {
            get { lock (_lock) { return _lastSelectedTile != null; } }
        }
    }
}
