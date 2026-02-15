using UORespawnApp.Scripts.DTO.Enums;

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

        // ==================== FUTURE REFACTORING OPPORTUNITIES ====================

        // The following UI state can be moved to Session for consistency:

        // Box Spawn Page:
        // - BoxSpawnComponent: static Dictionary<int, (double panX, double panY, double zoom)> mapPositions
        //   Could become: Dictionary<int, MapViewState> MapViewStates in Session

        // Region Spawn Page:
        // - RegionSpawnComponent: static Dictionary<int, (double panX, double panY, double zoom)> mapPositions
        //   Same pattern as Box Spawn - could share MapViewStates
        // - Last selected region name

        // Settings Page:
        // - Last selected bestiary entry
        // - Search text state

        // General UI:
        // - Mini-map collapsed/expanded state (currently static bool showXMLSpawners, showServerSpawns)
        // - These could become properties on Session for per-session persistence

        // Benefits of refactoring to Session:
        // - Centralized state management (single source of truth)
        // - Easier debugging (all state in one place)
        // - Consistent pattern across all pages
        // - No static variables (better testability)
    }
}
