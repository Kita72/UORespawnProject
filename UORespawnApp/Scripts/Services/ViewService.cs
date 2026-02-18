using UORespawnApp.Scripts.Entities;
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
        /// Currently active spawn pack (the last pack that was applied).
        /// When spawn edits are saved, they're synced back to this pack's folder.
        /// </summary>
        public SpawnPackInfo? ActivePack { get; private set; }

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
            var previousView = CurrentView;
            CurrentView = view;
            Logger.Info($"[Navigation] View changed: {previousView} → {view}");
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
                var previousMap = CurrentMapId;
                CurrentMapId = mapId;
                Logger.Info($"[Navigation] Map changed: {MapUtility.GetMapName(previousMap)} (ID:{previousMap}) → {MapUtility.GetMapName(mapId)} (ID:{mapId})");

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
            Logger.Info($"[Overlay] XML Spawners visibility: {(ShowXMLSpawners ? "ON" : "OFF")}");
            OnXMLSpawnersChanged?.Invoke();
        }

        /// <summary>
        /// Toggle server spawns visibility
        /// </summary>
        public void ToggleServerSpawns()
        {
            ShowServerSpawns = !ShowServerSpawns;
            Logger.Info($"[Overlay] Server Spawns visibility: {(ShowServerSpawns ? "ON" : "OFF")}");
            OnServerSpawnsChanged?.Invoke();
        }

        /// <summary>
        /// Set the currently active spawn pack
        /// </summary>
        /// <param name="pack">The pack that was applied</param>
        public void SetActivePack(SpawnPackInfo? pack)
        {
            var previousPack = ActivePack?.Metadata.Name ?? "(none)";
            ActivePack = pack;
            Logger.Info($"[Pack] Active pack changed: {previousPack} → {pack?.Metadata.Name ?? "(none)"}");
        }
    }
}