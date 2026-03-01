using System;
using System.Collections;

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

        public ArrayList WaterList { get; set; }
        public ArrayList WeatherList { get; set; }
        public ArrayList TimedList { get; set; }
        public ArrayList CommonList { get; set; }
        public ArrayList UnCommonList { get; set; }
        public ArrayList RareList { get; set; }

        public RegionEntity(string name, Region handle, WeatherTypes weather, TimeTypes time)
        {
            Name = name;
            RegionHandle = handle;
            WeatherType = weather;
            TimedType = time;
        }
    }
}
