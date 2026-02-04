namespace UORespawnApp
{
    public enum WorldTile
    {
        acid,
        blood,
        brick,
        cave,
        cave_exit,
        cave_floor,
        cloud,
        cobblestones,
        dirt,
        embank,
        flagstone,
        forest,
        furrows,
        grass,
        jungle,
        leaves,
        marble,
        obsidian,
        planks,
        rain_event,
        sand,
        sand_stone,
        snow,
        snow_event,
        stone,
        stone_moss,
        swamp,
        tile,
        tree,
        _void,
        voiddestruction,
        water,
        wooden_floor
    }

    public class WorldEntity
    {
        public GameMap MapHandle { get; private set; }

        public Dictionary<WorldTile, List<TileEntity>> WorldSpawn { get; private set; } = [];

        public WorldEntity(GameMap map)
        {
            MapHandle = map;

            for (int i = 0; i < Enum.GetValues<WorldTile>().Length; i++)
            {
                WorldSpawn.Add((WorldTile)i, []);
            }

            WorldSpawnUtility.AddWorldEntity(this);
        }

        public void AddSpawn(WorldTile tile, TileEntity spawn)
        {
            if (WorldSpawn.TryGetValue(tile, out List<TileEntity>? value) && !value.Contains(spawn))
            {
                value.Add(spawn);
            }
        }

        public void RemoveSpawn(WorldTile tile, TileEntity spawn)
        {
            WorldSpawn[tile].Remove(spawn);
        }
    }
}
