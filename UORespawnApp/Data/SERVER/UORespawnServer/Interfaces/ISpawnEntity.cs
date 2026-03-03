using Server.Custom.UORespawnServer.Enums;
using System.Collections.Generic;

namespace Server.Custom.UORespawnServer.Interfaces
{
    internal interface ISpawnEntity
    {
        List<string> WaterList { get; set; }
        List<string> WeatherList { get; set; }
        List<string> TimedList { get; set; }
        List<string> CommonList { get; set; }
        List<string> UnCommonList { get; set; }
        List<string> RareList { get; set; }

        WeatherTypes WeatherType { get; set; }
        TimeTypes TimedType { get; set; }
    }
}
