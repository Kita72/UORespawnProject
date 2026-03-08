using System.Collections.Generic;
using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Interfaces;

namespace Server.Custom.UORespawnServer.Entities
{
    internal class RegionEntity : ISpawnEntity
    {
        internal string Name { get; private set; } // Name of Region
        internal Region RegionHandle { get; private set; }

        public WeatherTypes WeatherType { get; set; }
        public TimeTypes TimedType { get; set; }

        public List<string> WaterList { get; set; }
        public List<string> WeatherList { get; set; }
        public List<string> TimedList { get; set; }
        public List<string> CommonList { get; set; }
        public List<string> UnCommonList { get; set; }
        public List<string> RareList { get; set; }

        public RegionEntity(string name, Region handle, WeatherTypes weather, TimeTypes time)
        {
            Name = name;
            RegionHandle = handle;
            WeatherType = weather;
            TimedType = time;
        }
    }
}
