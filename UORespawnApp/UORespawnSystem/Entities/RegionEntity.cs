using System.Collections;

using Server.Custom.UORespawnSystem.Enums;
using Server.Custom.UORespawnSystem.Interfaces;

namespace Server.Custom.UORespawnSystem.Entities
{
    internal class RegionEntity : ISpawnEntity
    {
        internal string Name { get; private set; } // Name of Region
        internal Region RegionHandle { get; private set; }

        public WeatherTypes WeatherSpawn { get; set; }
        public TimeNames TimedSpawn { get; set; }

        public ArrayList WaterSpawnList { get; set; }
        public ArrayList WeatherSpawnList { get; set; }
        public ArrayList TimedSpawnList { get; set; }
        public ArrayList CommonSpawnList { get; set; }
        public ArrayList UnCommonSpawnList { get; set; }
        public ArrayList RareSpawnList { get; set; }

        public RegionEntity(string name, Region handle, WeatherTypes weather, TimeNames time)
        {
            Name = name;
            RegionHandle = handle;
            WeatherSpawn = weather;
            TimedSpawn = time;
        }
    }
}
