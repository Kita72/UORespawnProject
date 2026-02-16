namespace UORespawnApp
{
    /// <summary>
    /// Session state manager for in-memory application state
    /// Tracks map selection and UI component states during runtime
    /// State is lost on app restart (not persisted)
    /// </summary>
    public class Session()
    {
        // ==================== MAP STATE ====================

        /// <summary>
        /// Currently selected map (0-5 for standard maps, 6+ for custom)
        /// </summary>
        public int Current_Map { get; set; } = 0; // Default to Map0 (Felucca)

        internal void SetMap(int mapId)
        {
            Current_Map = mapId;
        }

        // ==================== UI STATE (In-Memory Only) ====================

        /// <summary>
        /// Last selected tile in Tile Spawn page (null = not yet visited)
        /// Persists during session to maintain user's position
        /// </summary>
        public TileNames? LastSelectedTile { get; set; } = null;

        /// <summary>
        /// Check if Tile Spawn page has been visited in this session
        /// </summary>
        public bool HasVisitedTileSpawnPage => LastSelectedTile != null;
    }
}
