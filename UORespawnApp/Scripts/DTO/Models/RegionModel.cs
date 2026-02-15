using System;
using System.Collections.Generic;
using UORespawnApp.Scripts.DTO.Enums;

namespace UORespawnApp.Scripts.DTO.Models
{
    /// <summary>
    /// Serializable DTO for RegionEntity matching server-side RegionModel
    /// </summary>
    [Serializable]
    public class RegionModel
    {
        public string Name { get; set; }
        public int MapId { get; set; }

        public WeatherTypes WeatherSpawn { get; set; }
        public TimeNames TimedSpawn { get; set; }

        public List<string> WaterSpawns { get; set; }
        public List<string> WeatherSpawns { get; set; }
        public List<string> TimedSpawns { get; set; }
        public List<string> CommonSpawns { get; set; }
        public List<string> UncommonSpawns { get; set; }
        public List<string> RareSpawns { get; set; }

        public RegionModel()
        {
            Name = string.Empty;
            WaterSpawns = new List<string>();
            WeatherSpawns = new List<string>();
            TimedSpawns = new List<string>();
            CommonSpawns = new List<string>();
            UncommonSpawns = new List<string>();
            RareSpawns = new List<string>();
        }
    }

    /// <summary>
    /// Container for all region spawns on a single map
    /// </summary>
    [Serializable]
    public class MapRegionData
    {
        public int MapId { get; set; }
        public string MapName { get; set; }
        public List<RegionModel> RegionSpawns { get; set; }

        public MapRegionData()
        {
            MapName = string.Empty;
            RegionSpawns = new List<RegionModel>();
        }
    }

    /// <summary>
    /// Root container for all RegionData (multiple maps)
    /// </summary>
    [Serializable]
    public class RegionContainer
    {
        public string Version { get; set; }
        public List<MapRegionData> RegionData { get; set; }

        public RegionContainer()
        {
            Version = "2.0";
            RegionData = new List<MapRegionData>();
        }
    }
}
