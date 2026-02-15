using Server.Custom.UORespawnSystem.Enums;
using System.Collections;

namespace Server.Custom.UORespawnSystem.Interfaces
{
    internal interface ISpawnEntity
    {
        ArrayList WaterSpawnList { get; set; }
        ArrayList WeatherSpawnList { get; set; }
        ArrayList TimedSpawnList { get; set; }
        ArrayList CommonSpawnList { get; set; }
        ArrayList UnCommonSpawnList { get; set; }
        ArrayList RareSpawnList { get; set; }

        WeatherTypes WeatherSpawn { get; set; }
        TimeNames TimedSpawn { get; set; }
    }
}
