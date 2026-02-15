using System;
using System.Collections.Generic;

using Server.Custom.UORespawnSystem.Enums;

namespace Server.Custom.UORespawnSystem.Entities.BinaryModels
{
    /// <summary>
    /// Serializable DTO for TileEntity
    /// </summary>
    [Serializable]
    internal class TileModel
    {
        public string Name { get; set; } // Tile name (already converted)
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
    internal class MapTileData
    {
        public int MapId { get; set; }
        public string MapName { get; set; }
        public List<TileModel> TileSpawns { get; set; }

        public MapTileData()
        {
            TileSpawns = new List<TileModel>();
        }
    }

    /// <summary>
    /// Root container for all TileData (multiple maps)
    /// </summary>
    [Serializable]
    internal class TileContainer
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
