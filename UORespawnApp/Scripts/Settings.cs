using UORespawnApp.Scripts.Utilities;

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
        // Cache for frequently accessed settings to avoid repeated Preferences.Get() calls
        private static string? _cachedServUODataFolder;
        private static string? _cachedCurrentPackName;
        private static string? _cachedCurrentPackFolder;
        private static Color? _cachedBoxColor;
        private static double? _cachedBoxColorInc;
        private static int? _cachedBoxLineSize;

        // Initialize cache on first access
        static Settings()
        {
            LoadCache();
        }

        private const string DefaultBoxColorHex = "#8B0000";
        private const string DefaultPackName = "DefaultPack";
        private static readonly Color DefaultBoxColor = Color.FromArgb(DefaultBoxColorHex);

        private static void LoadCache()
        {
            _cachedServUODataFolder = Preferences.Get("ServUODataFolder", "");
            _cachedCurrentPackName = Preferences.Get("CurrentPackName", DefaultPackName);
            _cachedCurrentPackFolder = Preferences.Get("CurrentPackFolder", "");

            var colorString = Preferences.Get("BoxColor", DefaultBoxColorHex);
            _cachedBoxColor = colorString == DefaultBoxColorHex 
                ? DefaultBoxColor 
                : (Color.TryParse(colorString, out var parsed) ? parsed : DefaultBoxColor);

            _cachedBoxColorInc = Preferences.Get("BoxColorInc", 0.3);
            _cachedBoxLineSize = Preferences.Get("BoxLineSize", 2);
        }

        public static string ServUODataFolder
        {
            get => _cachedServUODataFolder ?? "";
            set
            {
                _cachedServUODataFolder = value;
                Preferences.Set("ServUODataFolder", value);
            }
        }

        /// <summary>
        /// The display name of the currently loaded spawn pack.
        /// Shown in UI to identify which pack is active.
        /// Defaults to "DefaultPack" for first-time users.
        /// </summary>
        public static string CurrentPackName
        {
            get => _cachedCurrentPackName ?? DefaultPackName;
            set
            {
                _cachedCurrentPackName = value;
                Preferences.Set("CurrentPackName", value);
            }
        }

        /// <summary>
        /// The folder path of the currently loaded spawn pack.
        /// Used to track which pack's data is in UOR_DATA and sync edits back to that pack.
        /// </summary>
        public static string CurrentPackFolder
        {
            get => _cachedCurrentPackFolder ?? string.Empty;
            set
            {
                _cachedCurrentPackFolder = value;
                Preferences.Set("CurrentPackFolder", value);
            }
        }

        public static Color BoxColor
        {
            get => _cachedBoxColor ?? Color.FromArgb("#8B0000");
            set
            {
                _cachedBoxColor = value;
                var hex = $"#{(int)(value.Red * 255):X2}{(int)(value.Green * 255):X2}{(int)(value.Blue * 255):X2}";
                Preferences.Set("BoxColor", hex);
            }
        }

        public static double BoxColorInc
        {
            get => _cachedBoxColorInc ?? 0.3;
            set
            {
                _cachedBoxColorInc = value;
                Preferences.Set("BoxColorInc", value);
            }
        }

        public static int BoxLineSize
        {
            get => _cachedBoxLineSize ?? 2;
            set
            {
                _cachedBoxLineSize = value;
                Preferences.Set("BoxLineSize", value);
            }
        }

        public static double TimedChance
        {
            get => Preferences.Get("TimedChance", 0.1);
            set => Preferences.Set("TimedChance", value);
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

        public static bool EnableVendorSpawn
        {
            get => Preferences.Get("EnableVendorSpawn", false);
            set => Preferences.Set("EnableVendorSpawn", value);
        }

        /// <summary>
        /// Whether vendors "sleep" at night (disabled = vendors out 24/7).
        /// When enabled, players won't find vendors out at night hours.
        /// </summary>
        public static bool EnableVendorNight
        {
            get => Preferences.Get("EnableVendorNight", false);
            set => Preferences.Set("EnableVendorNight", value);
        }

        /// <summary>
        /// Whether extra town NPCs spawn alongside vendors.
        /// Adds general TownNPC to boost town population and make it look like shoppers.
        /// </summary>
        public static bool EnableVendorExtra
        {
            get => Preferences.Get("EnableVendorExtra", false);
            set => Preferences.Set("EnableVendorExtra", value);
        }

        public static int MaxMobs
        {
            get => Preferences.Get("MaxMobs", 15);
            set => Preferences.Set("MaxMobs", value);
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

        public static bool IsScaleSpawn
        {
            get => Preferences.Get("IsScaleSpawn", false);
            set => Preferences.Set("IsScaleSpawn", value);
        }

        /// <summary>
        /// Whether debug mode is enabled (shows in-app log viewer panel)
        /// </summary>
        public static bool IsDebugMode
        {
            get => Preferences.Get("IsDebugMode", false);
            set => Preferences.Set("IsDebugMode", value);
        }

        /// <summary>
        /// List of favorite creature names from the bestiary.
        /// Stored as comma-separated values in preferences.
        /// </summary>
        public static List<string> BestiaryFavorites
        {
            get
            {
                var value = Preferences.Get("BestiaryFavorites", "");
                if (string.IsNullOrEmpty(value))
                    return [];
                return [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries)];
            }
            set => Preferences.Set("BestiaryFavorites", string.Join(",", value));
        }

        /// <summary>
        /// List of favorite vendor names.
        /// Stored as comma-separated values in preferences.
        /// </summary>
        public static List<string> VendorFavorites
        {
            get
            {
                var value = Preferences.Get("VendorFavorites", "");
                if (string.IsNullOrEmpty(value))
                    return [];
                return [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries)];
            }
            set => Preferences.Set("VendorFavorites", string.Join(",", value));
        }

        /// <summary>
        /// Helper method to toggle a creature as favorite
        /// </summary>
        public static void ToggleBestiaryFavorite(string name)
        {
            var favorites = BestiaryFavorites;
            if (favorites.Contains(name))
                favorites.Remove(name);
            else
                favorites.Add(name);
            BestiaryFavorites = favorites;
        }

        /// <summary>
        /// Helper method to toggle a vendor as favorite
        /// </summary>
        public static void ToggleVendorFavorite(string name)
        {
            var favorites = VendorFavorites;
            if (favorites.Contains(name))
                favorites.Remove(name);
            else
                favorites.Add(name);
            VendorFavorites = favorites;
        }

        /// <summary>
        /// Clears ALL MAUI Preferences to factory defaults.
        /// This is a complete reset - removes all stored settings including:
        /// - ServUO link, current pack info
        /// - All spawn chances and limits
        /// - All feature flags
        /// - All favorites
        /// - UI appearance settings
        /// Call this during a full app reset to ensure no corrupted data persists.
        /// </summary>
        public static void ClearAllPreferences()
        {
            // Use Preferences.Clear() to remove ALL preferences at once
            Preferences.Clear();

            // Reset the cache to default values
            _cachedServUODataFolder = "";
            _cachedCurrentPackName = DefaultPackName;
            _cachedCurrentPackFolder = "";
            _cachedBoxColor = DefaultBoxColor;
            _cachedBoxColorInc = 0.3;
            _cachedBoxLineSize = 2;

            Logger.Info("All MAUI Preferences cleared to factory defaults");
        }

        /// <summary>
        /// Resets spawn-related settings to defaults without clearing ServUO link or pack info.
        /// Use this for a "soft reset" that preserves server configuration.
        /// </summary>
        public static void ResetSpawnSettingsToDefaults()
        {
            // Spawn limits
            MaxMobs = 15;
            MinRange = 10;
            MaxRange = 50;
            MaxCrowd = 1;

            // Spawn chances
            WaterChance = 0.5;
            WeatherChance = 0.1;
            TimedChance = 0.1;
            CommonChance = 1.0;
            UnCommonChance = 0.5;
            RareChance = 0.1;

            // Feature flags
            IsScaleSpawn = false;
            EnableRiftSpawn = false;
            EnableDebugSpawn = false;
            EnableVendorSpawn = false;
            EnableVendorNight = false;
            EnableVendorExtra = false;

            // UI appearance
            BoxColor = DefaultBoxColor;
            BoxColorInc = 0.3;
            BoxLineSize = 2;

            Logger.Info("Spawn settings reset to defaults");
        }
    }
}