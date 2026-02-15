using System;
using System.Collections.Generic;

using Server.Custom.UORespawnSystem.Enums;

namespace Server.Custom.UORespawnSystem.Entities.BinaryModels
{
    /// <summary>
    /// Serializable DTO for RegionEntity
    /// </summary>
    [Serializable]
    internal class RegionModel
    {
        public string Name { get; set; } // Region name (only named regions)
        public int MapId { get; set; }  // Map ID for region lookup

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
    internal class MapRegionData
    {
        public int MapId { get; set; }
        public string MapName { get; set; }
        public List<RegionModel> RegionSpawns { get; set; }

        public MapRegionData()
        {
            RegionSpawns = new List<RegionModel>();
        }
    }

    /// <summary>
    /// Root container for all RegionData (multiple maps)
    /// </summary>
    [Serializable]
    internal class RegionContainer
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
