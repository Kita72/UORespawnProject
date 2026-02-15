using System.Collections;
using Server.Custom.UORespawnSystem.Enums;
using Server.Custom.UORespawnSystem.Interfaces;

namespace Server.Custom.UORespawnSystem.Entities
{
    internal class BoxEntity : ISpawnEntity
    {
        internal int Id { get; private set; } // Id of Box on Map
        internal int SpawnPriority { get; private set; } // Priority of SpawnBox

        internal Rectangle2D SpawnBox { get; private set; } // Bounding Box on Map

        public WeatherTypes WeatherSpawn { get; set; }
        public TimeNames TimedSpawn { get; set; }

        public ArrayList WaterSpawnList { get; set; }
        public ArrayList WeatherSpawnList { get; set; }
        public ArrayList TimedSpawnList { get; set; }
        public ArrayList CommonSpawnList { get; set; }
        public ArrayList UnCommonSpawnList { get; set; }
        public ArrayList RareSpawnList { get; set; }

        internal BoxEntity(int id, int priority, Rectangle2D box, WeatherTypes weather, TimeNames time)
        {
            Id = id;
            SpawnPriority = priority;
            SpawnBox = box;
            WeatherSpawn = weather;
            TimedSpawn = time;
        }
    }
}
