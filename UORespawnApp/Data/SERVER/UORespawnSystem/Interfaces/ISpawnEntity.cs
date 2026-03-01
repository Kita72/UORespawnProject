using Server.Custom.UORespawnServer.Enums;
using System.Collections;

namespace Server.Custom.UORespawnServer.Interfaces
{
    internal interface ISpawnEntity
    {
        ArrayList WaterList { get; set; }
        ArrayList WeatherList { get; set; }
        ArrayList TimedList { get; set; }
        ArrayList CommonList { get; set; }
        ArrayList UnCommonList { get; set; }
        ArrayList RareList { get; set; }

        WeatherTypes WeatherType { get; set; }
        TimeTypes TimedType { get; set; }
    }
}
