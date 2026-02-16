using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp
{
    /// <summary>
    /// Service for managing view navigation and map selection state.
    /// Acts as a mediator between navigation controls and page components.
    /// </summary>
    public class ViewService
    {
        /// <summary>
        /// Currently active view identifier (e.g., "home", "boxspawn", "tilespawn", "regionspawn", "settings", "instructions")
        /// </summary>
        public string CurrentView { get; set; } = "home";

        /// <summary>
        /// Currently selected map ID (0=Felucca, 1=Trammel, 2=Ilshenar, 3=Malas, 4=Tokuno, 5=Ter Mur)
        /// </summary>
        public int CurrentMapId { get; set; } = 0;

        /// <summary>
        /// Whether XML spawners overlay is currently visible (shared across Box/Region spawn pages)
        /// </summary>
        public bool ShowXMLSpawners { get; set; } = false;

        /// <summary>
        /// Whether server spawn statistics overlay is currently visible (shared across Box/Region spawn pages)
        /// </summary>
        public bool ShowServerSpawns { get; set; } = false;

        /// <summary>
        /// Event raised when the current view changes
        /// </summary>
        public event Action? OnViewChanged;

        /// <summary>
        /// Event raised when the current map selection changes
        /// </summary>
        public event Action? OnMapChanged;

        /// <summary>
        /// Event raised when XML spawners visibility changes
        /// </summary>
        public event Action? OnXMLSpawnersChanged;

        /// <summary>
        /// Event raised when server spawns visibility changes
        /// </summary>
        public event Action? OnServerSpawnsChanged;

        /// <summary>
        /// Navigate to a different view
        /// </summary>
        /// <param name="view">View identifier to navigate to</param>
        public void SetView(string view)
        {
            CurrentView = view;
            OnViewChanged?.Invoke();
        }

        /// <summary>
        /// Change the currently selected map
        /// </summary>
        /// <param name="mapId">Map ID to switch to</param>
        public void SetMap(int mapId)
        {
            if (CurrentMapId != mapId)
            {
                CurrentMapId = mapId;

                Utility.SESSION?.SetMap(mapId);

                OnMapChanged?.Invoke();
            }
        }

        /// <summary>
        /// Toggle XML spawners visibility
        /// </summary>
        public void ToggleXMLSpawners()
        {
            ShowXMLSpawners = !ShowXMLSpawners;
            OnXMLSpawnersChanged?.Invoke();
        }

        /// <summary>
        /// Toggle server spawns visibility
        /// </summary>
        public void ToggleServerSpawns()
        {
            ShowServerSpawns = !ShowServerSpawns;
            OnServerSpawnsChanged?.Invoke();
        }
    }
}