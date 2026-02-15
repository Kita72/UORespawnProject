using System;
using System.Collections.Generic;
using UORespawnApp.Scripts.DTO.Enums;

namespace UORespawnApp.Scripts.DTO.Models
{
    /// <summary>
    /// Serializable DTO for BoxEntity matching server-side BoxModel
    /// </summary>
    [Serializable]
    public class BoxModel
    {
        public int Id { get; set; }
        public int SpawnPriority { get; set; }
        public int MapId { get; set; }

        // Rectangle2D components
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public WeatherTypes WeatherSpawn { get; set; }
        public TimeNames TimedSpawn { get; set; }

        // 6 spawn lists as List<string> (mob type names)
        public List<string> WaterSpawns { get; set; }
        public List<string> WeatherSpawns { get; set; }
        public List<string> TimedSpawns { get; set; }
        public List<string> CommonSpawns { get; set; }
        public List<string> UncommonSpawns { get; set; }
        public List<string> RareSpawns { get; set; }

        public BoxModel()
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
    /// Container for all box spawns on a single map
    /// </summary>
    [Serializable]
    public class MapBoxData
    {
        public int MapId { get; set; }
        public string MapName { get; set; }
        public List<BoxModel> BoxSpawns { get; set; }

        public MapBoxData()
        {
            MapName = string.Empty;
            BoxSpawns = new List<BoxModel>();
        }
    }

    /// <summary>
    /// Root container for all BoxData (multiple maps)
    /// </summary>
    [Serializable]
    public class BoxContainer
    {
        public string Version { get; set; }
        public List<MapBoxData> BoxData { get; set; }

        public BoxContainer()
        {
            Version = "2.0";
            BoxData = new List<MapBoxData>();
        }
    }
}
