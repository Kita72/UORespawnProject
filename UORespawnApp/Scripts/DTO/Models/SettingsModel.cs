using System;

namespace UORespawnApp.Scripts.DTO.Models
{
    /// <summary>
    /// Serializable DTO for Settings matching server-side SettingsModel
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

        public SettingsModel()
        {
            Version = "2.0.0.1";
            MaxMobs = 15;
            MinRange = 10;
            MaxRange = 50;
            MaxCrowd = 1;
            ChanceWater = 0.5;
            ChanceWeather = 0.1;
            ChanceTimed = 0.1;
            ChanceCommon = 1.0;
            ChanceUncommon = 0.5;
            ChanceRare = 0.1;
            ScaleSpawn = false;
            EnableRiftSpawn = false;
            EnableDebug = false;
        }
    }
}
