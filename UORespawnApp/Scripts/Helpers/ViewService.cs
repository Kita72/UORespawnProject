namespace UORespawnApp
{
    public class ViewService
    {
        public string CurrentView { get; set; } = "home";
        public GameMap CurrentMap { get; set; } = GameMap.Map0;

        public event Action? OnViewChanged;
        public event Action? OnMapChanged;

        public void SetView(string view)
        {
            CurrentView = view;
            OnViewChanged?.Invoke();
        }

        public void SetMap(GameMap map)
        {
            if (CurrentMap != map)
            {
                CurrentMap = map;

                Utility.SESSION?.SetMap(map);

                OnMapChanged?.Invoke();
            }
        }
    }
}