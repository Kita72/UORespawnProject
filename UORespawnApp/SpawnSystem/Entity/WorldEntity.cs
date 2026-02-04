using System;
using System.Linq;
using System.Collections.Generic;

namespace Server.Custom.SpawnSystem
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
        public Map MapHandle { get; private set; }

        public Dictionary<WorldTile, List<TileEntity>> WorldSpawn { get; private set; } = new Dictionary<WorldTile, List<TileEntity>>();

        public WorldEntity(Map map)
        {
            MapHandle = map;

            for (int i = 0; i < Enum.GetValues(typeof(WorldTile)).Length; i++)
            {
                WorldSpawn.Add((WorldTile)i, new List<TileEntity>());
            }

            SpawnSysDataBase.AddWorldEntity(this);
        }

        public string GetRandomSpawn(WorldTile tile, Frequency freq)
        {
            var spawn = WorldSpawn[tile].Where(t => t.Freq == freq).ToList();

            if (spawn.Count > 0)
            {
                return spawn[Utility.Random(spawn.Count - 1)].Name;
            }

            return string.Empty;
        }

        public void AddSpawn(WorldTile tile, TileEntity spawn)
        {
            if (WorldSpawn.TryGetValue(tile, out List<TileEntity> value) && !value.Contains(spawn))
            {
                value.Add(spawn);
            }
        }

        public void RemoveSpawn(WorldTile tile, TileEntity spawn)
        {
            WorldSpawn[tile].Remove(spawn);
        }

        public List<TileEntity> GetSpawnList(string tileName, Frequency freq)
        {
            if (Enum.TryParse(ConvertedTileName(tileName), out WorldTile result))
            {
                return WorldSpawn[result].Where(t => t.Freq == freq).ToList();
            }

            return new List<TileEntity>();
        }

        private string ConvertedTileName(string tileName)
        {
            if (tileName.ToLower() == "void")
            {
                tileName = "_void";
            }
            else
            {
                tileName = tileName.Replace(' ', '_');
            }

            return tileName;
        }
    }
}
