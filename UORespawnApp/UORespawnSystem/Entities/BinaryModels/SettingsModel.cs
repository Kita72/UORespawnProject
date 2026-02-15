using System;

namespace Server.Custom.UORespawnSystem.Entities.BinaryModels
{
    /// <summary>
    /// Serializable DTO for Settings
    /// </summary>
    [Serializable]
    public class SettingsModel
    {
        public string Version { get; set; }

        // Basic spawn limits
        public int MaxMobs { get; set; }
        public int MinRange { get; set; }
        public int MaxRange { get; set; }
        public int MaxCrowd { get; set; }

        // Spawn chances
        public double ChanceWater { get; set; }
        public double ChanceWeather { get; set; }
        public double ChanceTimed { get; set; }
        public double ChanceCommon { get; set; }
        public double ChanceUncommon { get; set; }
        public double ChanceRare { get; set; }

        // Features
        public bool ScaleSpawn { get; set; }
        public bool EnableRiftSpawn { get; set; }
        public bool EnableDebug { get; set; }
    }
}
