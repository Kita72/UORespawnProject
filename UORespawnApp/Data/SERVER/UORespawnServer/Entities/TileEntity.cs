using System.Collections;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Interfaces;

namespace Server.Custom.UORespawnServer.Entities
{
    internal class TileEntity : ISpawnEntity
    {
        public string Name { get; private set; } // Name of Tile

        public WeatherTypes WeatherType { get; set; }
        public TimeTypes TimedType { get; set; }

        public ArrayList WaterList { get; set; }
        public ArrayList WeatherList { get; set; }
        public ArrayList TimedList { get; set; }
        public ArrayList CommonList { get; set; }
        public ArrayList UnCommonList { get; set; }
        public ArrayList RareList { get; set; }

        public TileEntity(string name, WeatherTypes weather, TimeTypes time)
        {
            Name = name;
            WeatherType = weather;
            TimedType = time;
        }
    }
}
