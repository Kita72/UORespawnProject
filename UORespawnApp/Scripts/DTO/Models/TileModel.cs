using System;
using System.Collections.Generic;
using UORespawnApp.Scripts.DTO.Enums;

namespace UORespawnApp.Scripts.DTO.Models
{
    /// <summary>
    /// Serializable DTO for TileEntity matching server-side TileModel
    /// </summary>
    [Serializable]
    public class TileModel
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

        public TileModel()
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
    /// Container for all tile spawns on a single map
    /// </summary>
    [Serializable]
    public class MapTileData
    {
        public int MapId { get; set; }
        public string MapName { get; set; }
        public List<TileModel> TileSpawns { get; set; }

        public MapTileData()
        {
            MapName = string.Empty;
            TileSpawns = new List<TileModel>();
        }
    }

    /// <summary>
    /// Root container for all TileData (multiple maps)
    /// </summary>
    [Serializable]
    public class TileContainer
    {
        public string Version { get; set; }
        public List<MapTileData> TileData { get; set; }

        public TileContainer()
        {
            Version = "2.0";
            TileData = new List<MapTileData>();
        }
    }
}
