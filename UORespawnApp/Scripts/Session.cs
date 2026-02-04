namespace UORespawnApp
{
    public class Session()
    {
        public GameMap Current_Map { get; set; } = GameMap.Map0;

        internal void SetMap(GameMap map)
        {
            Current_Map = map;
        }

        public SpawnEntity Spawn_Entity { get; private set; } = new();

        internal void SetSpawn(SpawnEntity entity)
        {
            Spawn_Entity = entity;
        }

        public WorldEntity World_Entity { get; private set; } = new(GameMap.Map0);

        internal void SetWorldSpawn(WorldEntity entity)
        {
            World_Entity = entity;
        }

        public WorldTile World_Tile { get; private set; } = new();

        internal void SetWorldTile(WorldTile tile)
        {
            World_Tile = tile;
        }

        public string World_Static { get; private set; } = string.Empty;

        internal void SetWorldStatic(string? name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                World_Static = name;
            }
        }
    }
}
