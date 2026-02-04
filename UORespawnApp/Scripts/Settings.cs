namespace UORespawnApp
{
    internal static class Settings
    {
        public static string ServUODataFolder
        {
            get => Preferences.Get("ServUODataFolder", "");
            set => Preferences.Set("ServUODataFolder", value);
        }

        public static Color BoxColor
        {
            get
            {
                var colorString = Preferences.Get("BoxColor", "#8B0000"); // Dark Red default
                try
                {
                    return Color.FromArgb(colorString);
                }
                catch
                {
                    return Color.FromArgb("#8B0000"); // Fallback to dark red
                }
            }
            set
            {
                var hex = $"#{(int)(value.Red * 255):X2}{(int)(value.Green * 255):X2}{(int)(value.Blue * 255):X2}";
                Preferences.Set("BoxColor", hex);
            }
        }

        public static double BoxColorInc
        {
            get => Preferences.Get("BoxColorInc", 0.3);
            set => Preferences.Set("BoxColorInc", value);
        }

        public static int BoxLineSize
        {
            get => Preferences.Get("BoxLineSize", 2);
            set => Preferences.Set("BoxLineSize", value);
        }

        public static double CreatureChance
        {
            get => Preferences.Get("CreatureChance", 0.1);
            set => Preferences.Set("CreatureChance", value);
        }

        public static double CommonChance
        {
            get => Preferences.Get("CommonChance", 1.0);
            set => Preferences.Set("CommonChance", value);
        }

        public static double UnCommonChance
        {
            get => Preferences.Get("UnCommonChance", 0.5);
            set => Preferences.Set("UnCommonChance", value);
        }

        public static double RareChance
        {
            get => Preferences.Get("RareChance", 0.1);
            set => Preferences.Set("RareChance", value);
        }

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

        public static bool EnableRiftSpawn
        {
            get => Preferences.Get("EnableRiftSpawn", false);
            set => Preferences.Set("EnableRiftSpawn", value);
        }

        public static bool EnableDebugSpawn
        {
            get => Preferences.Get("EnableDebugSpawn", false);
            set => Preferences.Set("EnableDebugSpawn", value);
        }

        public static int MaxMob
        {
            get => Preferences.Get("MaxMob", 15);
            set => Preferences.Set("MaxMob", value);
        }

        public static int MinRange
        {
            get => Preferences.Get("MinRange", 10);
            set => Preferences.Set("MinRange", value);
        }

        public static int MaxRange
        {
            get => Preferences.Get("MaxRange", 50);
            set => Preferences.Set("MaxRange", value);
        }

        public static int MaxCrowd
        {
            get => Preferences.Get("MaxCrowd", 1);
            set => Preferences.Set("MaxCrowd", value);
        }

        public static double WaterChance
        {
            get => Preferences.Get("WaterChance", 0.5);
            set => Preferences.Set("WaterChance", value);
        }

        public static double WeatherChance
        {
            get => Preferences.Get("WeatherChance", 0.1);
            set => Preferences.Set("WeatherChance", value);
        }

        public static double StaticChance
        {
            get => Preferences.Get("StaticChance", 0.1);
            set => Preferences.Set("StaticChance", value);
        }

        public static bool IsScaleSpawn
        {
            get => Preferences.Get("IsScaleSpawn", false);
            set => Preferences.Set("IsScaleSpawn", value);
        }
    }
}