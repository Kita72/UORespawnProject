using System.Collections;

using Server.Custom.UORespawnSystem.Enums;
using Server.Custom.UORespawnSystem.Interfaces;
using Server.Custom.UORespawnSystem.SpawnUtility;

namespace Server.Custom.UORespawnSystem.Entities
{
    internal class TileEntity : ISpawnEntity
    {
        public string Name { get; private set; } // Name of Tile

        public WeatherTypes WeatherSpawn { get; set; }
        public TimeNames TimedSpawn { get; set; }

        public ArrayList WaterSpawnList { get; set; }
        public ArrayList WeatherSpawnList { get; set; }
        public ArrayList TimedSpawnList { get; set; }
        public ArrayList CommonSpawnList { get; set; }
        public ArrayList UnCommonSpawnList { get; set; }
        public ArrayList RareSpawnList { get; set; }

        public TileEntity(string name, WeatherTypes weather, TimeNames time)
        {
            Name = UORespawnUtility.ConvertTileName(name);
            WeatherSpawn = weather;
            TimedSpawn = time;
        }
    }
}
