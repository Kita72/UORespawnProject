using System;
using System.Collections;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Interfaces;

namespace Server.Custom.UORespawnServer.Entities
{
    internal class BoxEntity : ISpawnEntity
    {
        internal int Id { get; private set; } // Id of Box on Map
        internal int SpawnPriority { get; private set; } // Priority of SpawnBox
        internal Rectangle2D SpawnBox { get; private set; } // Bounding Box on Map

        public WeatherTypes WeatherType { get; set; }
        public TimeTypes TimedType { get; set; }

        public ArrayList WaterList { get; set; }
        public ArrayList WeatherList { get; set; }
        public ArrayList TimedList { get; set; }
        public ArrayList CommonList { get; set; }
        public ArrayList UnCommonList { get; set; }
        public ArrayList RareList { get; set; }

        internal BoxEntity(int id, int priority, Rectangle2D box, WeatherTypes weather, TimeTypes time)
        {
            Id = id;
            SpawnPriority = priority;
            SpawnBox = box;
            WeatherType = weather;
            TimedType = time;

        }
    }
}
