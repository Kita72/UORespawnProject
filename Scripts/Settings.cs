namespace UORespawnApp
{
    /// <summary>
    /// Settings class manages UORespawn configuration using a two-tier persistence model:
    /// 
    /// 1. PREFERENCES (Microsoft.Maui.Storage.Preferences):
    ///    - All settings stored as key/value pairs
    ///    - Immediate persistence on every property set
    ///    - Used for editor-specific settings (UI appearance, server folder path, custom bestiary)
    /// 
    /// 2. BINARY SERIALIZATION (UOR_SpawnSettings.bin):
    ///    - Spawn-related settings saved to binary file via Utility.SaveSettings()
    ///    - Syncs to server folder when configured
    ///    - Loaded on app startup by BackgroundDataLoader.LoadSettingsAsync()
    /// 
    /// PROPERTIES IN BINARY (.bin file):
    ///    MaxMobs, MinRange, MaxRange, MaxCrowd,
    ///    WaterChance, WeatherChance, TimedChance,
    ///    CommonChance, UnCommonChance, RareChance,
    ///    IsScaleSpawn, EnableRiftSpawn, EnableDebugSpawn
    /// 
    /// PROPERTIES PREFERENCES-ONLY (NOT in binary):
    ///    ServUODataFolder (editor config),
    ///    BoxColor, BoxColorInc, BoxLineSize (UI appearance),
    ///    Bestiary (editor custom creature list)
    /// 
    /// SAVE TRIGGERS:
    ///    - SettingsComponent.Dispose() (page navigation)
    ///    - ResetToDefaults() (explicit save after reset)
    /// </summary>
    internal static class Settings
    {
        /// <summary>
        /// Path to ServUO Data folder for server integration
        /// </summary>
        public static string ServUODataFolder
        {
            get => Preferences.Get("ServUODataFolder", "");
            set => Preferences.Set("ServUODataFolder", value);
        }

        /// <summary>
        /// Base color for spawn box visualization on map
        /// </summary>
        public static Color BoxColor
        {
            get
            {
                var colorString = Preferences.Get("BoxColor", "#8B0000");
                try
                {
                    return Color.FromArgb(colorString);
                }
                catch
                {
                    return Color.FromArgb("#8B0000");
                }
            }
            set
            {
                var hex = $"#{(int)(value.Red * 255):X2}{(int)(value.Green * 255):X2}{(int)(value.Blue * 255):X2}";
                Preferences.Set("BoxColor", hex);
            }
        }

        /// <summary>
        /// Color increment for overlapping spawn boxes (brightness adjustment)
        /// </summary>
        public static double BoxColorInc
        {
            get => Preferences.Get("BoxColorInc", 0.3);
            set => Preferences.Set("BoxColorInc", value);
        }

        /// <summary>
        /// Line thickness for spawn box borders on map
        /// </summary>
        public static int BoxLineSize
        {
            get => Preferences.Get("BoxLineSize", 2);
            set => Preferences.Set("BoxLineSize", value);
        }

        /// <summary>
        /// Spawn chance for timed spawn list (0.0-1.0)
        /// </summary>
        public static double TimedChance
        {
            get => Preferences.Get("TimedChance", 0.1);
            set => Preferences.Set("TimedChance", value);
        }

        /// <summary>
        /// Spawn chance for common spawn list (0.0-1.0)
        /// </summary>
        public static double CommonChance
        {
            get => Preferences.Get("CommonChance", 1.0);
            set => Preferences.Set("CommonChance", value);
        }

        /// <summary>
        /// Spawn chance for uncommon spawn list (0.0-1.0)
        /// </summary>
        public static double UnCommonChance
        {
            get => Preferences.Get("UnCommonChance", 0.5);
            set => Preferences.Set("UnCommonChance", value);
        }

        /// <summary>
        /// Spawn chance for rare spawn list (0.0-1.0)
        /// </summary>
        public static double RareChance
        {
            get => Preferences.Get("RareChance", 0.1);
            set => Preferences.Set("RareChance", value);
        }

        /// <summary>
        /// Custom bestiary creature list (editor-only, not synced to server)
        /// </summary>
        public static System.Collections.Specialized.StringCollection Bestiary
        {
            get
            {
                var list = new System.Collections.Specialized.StringCollection();
                var value = Preferences.Get("Bestiary", "");

                if (!string.IsNullOrEmpty(value))
                {
                    list.AddRange(value.Split(','));
                }
                return list;
            }
            set => Preferences.Set("Bestiary", string.Join(",", value.Cast<string>()));
        }

        /// <summary>
        /// Enable rift spawn feature (experimental)
        /// </summary>
        public static bool EnableRiftSpawn
        {
            get => Preferences.Get("EnableRiftSpawn", false);
            set => Preferences.Set("EnableRiftSpawn", value);
        }

        /// <summary>
        /// Enable debug spawn logging on server
        /// </summary>
        public static bool EnableDebugSpawn
        {
            get => Preferences.Get("EnableDebugSpawn", false);
            set => Preferences.Set("EnableDebugSpawn", value);
        }

        /// <summary>
        /// Maximum number of mobs per spawner
        /// </summary>
        public static int MaxMobs
        {
            get => Preferences.Get("MaxMobs", 15);
            set => Preferences.Set("MaxMobs", value);
        }

        /// <summary>
        /// Minimum spawn range from spawner center
        /// </summary>
        public static int MinRange
        {
            get => Preferences.Get("MinRange", 10);
            set => Preferences.Set("MinRange", value);
        }

        /// <summary>
        /// Maximum spawn range from spawner center
        /// </summary>
        public static int MaxRange
        {
            get => Preferences.Get("MaxRange", 50);
            set => Preferences.Set("MaxRange", value);
        }

        /// <summary>
        /// Maximum number of same creature type in spawn range
        /// </summary>
        public static int MaxCrowd
        {
            get => Preferences.Get("MaxCrowd", 1);
            set => Preferences.Set("MaxCrowd", value);
        }

        /// <summary>
        /// Spawn chance for water spawn list (0.0-1.0)
        /// </summary>
        public static double WaterChance
        {
            get => Preferences.Get("WaterChance", 0.5);
            set => Preferences.Set("WaterChance", value);
        }

        /// <summary>
        /// Spawn chance for weather spawn list (0.0-1.0)
        /// </summary>
        public static double WeatherChance
        {
            get => Preferences.Get("WeatherChance", 0.1);
            set => Preferences.Set("WeatherChance", value);
        }

        /// <summary>
        /// Enable dynamic spawn scaling based on player activity
        /// </summary>
        public static bool IsScaleSpawn
        {
            get => Preferences.Get("IsScaleSpawn", false);
            set => Preferences.Set("IsScaleSpawn", value);
        }
    }
}
