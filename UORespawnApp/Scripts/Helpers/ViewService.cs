using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp
{
    public class ViewService
    {
        public string CurrentView { get; set; } = "home";
        public int CurrentMapId { get; set; } = 0; // Default to Map0 (Felucca)

        public event Action? OnViewChanged;
        public event Action? OnMapChanged;

        public void SetView(string view)
        {
            CurrentView = view;
            OnViewChanged?.Invoke();
        }

        public void SetMap(int mapId)
        {
            if (CurrentMapId != mapId)
            {
                CurrentMapId = mapId;

                Utility.SESSION?.SetMap(mapId);

                OnMapChanged?.Invoke();
            }
        }
    }
}