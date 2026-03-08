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
    ///    ScriptsCustomFolder (Custom/ folder where UORespawnServer/ is installed),
    ///    ServerDataFolder (Data/ folder where UORespawn/ data exchange lives),
    ///    ServerType (\"ServUO\" or \"MUO\" - which bundled scripts to install),
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
        private static string? _cachedScriptsCustomFolder;
        private static string? _cachedServerDataFolder;
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

        // ==================== VALIDATION CONSTANTS ====================

        /// <summary>Minimum allowed value for range settings</summary>
        public const int MinRangeValue = 5;

        /// <summary>Maximum allowed value for MinRange</summary>
        public const int MaxMinRangeValue = 125;

        /// <summary>Maximum allowed value for MaxRange</summary>
        public const int MaxMaxRangeValue = 250;

        /// <summary>Minimum allowed value for MaxMobs</summary>
        public const int MinMobsValue = 5;

        /// <summary>Maximum allowed value for MaxMobs</summary>
        public const int MaxMobsValue = 75;

        /// <summary>Minimum allowed value for MaxCrowd</summary>
        public const int MinCrowdValue = 1;

        /// <summary>Maximum allowed value for MaxCrowd</summary>
        public const int MaxCrowdValue = 10;

        /// <summary>Minimum allowed value for interval settings (ms)</summary>
        public const int MinIntervalValue = 50;

        /// <summary>Maximum allowed value for interval settings (ms)</summary>
        public const int MaxIntervalValue = 2000;

        private static void LoadCache()
        {
            // Migration: Clear old single-path ServerFolder setting (pre-2.0.2)
            // Users must re-link using the new two-path approach (Custom folder + Data folder)
            if (Preferences.ContainsKey("ServerFolder"))
            {
                Preferences.Remove("ServerFolder");
                Logger.Info("Cleared legacy ServerFolder setting - server must be re-linked with new two-path setup");
            }

            if (Preferences.ContainsKey("ServUODataFolder"))
            {
                Preferences.Remove("ServUODataFolder");
            }

            _cachedScriptsCustomFolder = Preferences.Get("ScriptsCustomFolder", "");
            _cachedServerDataFolder = Preferences.Get("ServerDataFolder", "");
            _cachedCurrentPackName = Preferences.Get("CurrentPackName", DefaultPackName);
            _cachedCurrentPackFolder = Preferences.Get("CurrentPackFolder", "");

            var colorString = Preferences.Get("BoxColor", DefaultBoxColorHex);
            _cachedBoxColor = colorString == DefaultBoxColorHex 
                ? DefaultBoxColor 
                : (Color.TryParse(colorString, out var parsed) ? parsed : DefaultBoxColor);

            _cachedBoxColorInc = Preferences.Get("BoxColorInc", 0.3);
            _cachedBoxLineSize = Preferences.Get("BoxLineSize", 2);
        }

        /// <summary>
        /// The server's Custom folder path (e.g., C:\ServUO\Scripts\Custom\).
        /// UORespawnServer/ scripts are installed directly inside this folder.
        /// Empty string if not linked.
        /// </summary>
        public static string ScriptsCustomFolder
        {
            get => _cachedScriptsCustomFolder ?? "";
            set
            {
                _cachedScriptsCustomFolder = value;
                Preferences.Set("ScriptsCustomFolder", value);
            }
        }

        /// <summary>
        /// The server's Data folder path (e.g., C:\ServUO\Data\).
        /// UORespawn/ data exchange folder is created directly inside this folder.
        /// Empty string if not linked.
        /// </summary>
        public static string ServerDataFolder
        {
            get => _cachedServerDataFolder ?? "";
            set
            {
                _cachedServerDataFolder = value;
                Preferences.Set("ServerDataFolder", value);
            }
        }

        /// <summary>
        /// The selected server type for script installation ("ServUO" or "MUO").
        /// Used to pick the correct bundled server scripts from Data/SERVER/.
        /// </summary>
        public static string ServerType
        {
            get => Preferences.Get("ServerType", "ServUO");
            set => Preferences.Set("ServerType", value);
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
                _cachedBoxColorInc = Math.Clamp(value, 0.0, 1.0);
                Preferences.Set("BoxColorInc", _cachedBoxColorInc.Value);
            }
        }

        public static int BoxLineSize
        {
            get => _cachedBoxLineSize ?? 2;
            set
            {
                _cachedBoxLineSize = Math.Clamp(value, 1, 10);
                Preferences.Set("BoxLineSize", _cachedBoxLineSize.Value);
            }
        }

        public static double TimedChance
        {
            get => Preferences.Get("TimedChance", 0.01);
            set => Preferences.Set("TimedChance", Math.Clamp(value, 0.0, 1.0));
        }

        public static double CommonChance
        {
            get => Preferences.Get("CommonChance", 1.0);
            set => Preferences.Set("CommonChance", Math.Clamp(value, 0.0, 1.0));
        }

        public static double UnCommonChance
        {
            get => Preferences.Get("UnCommonChance", 0.1);
            set => Preferences.Set("UnCommonChance", Math.Clamp(value, 0.0, 1.0));
        }

        public static double RareChance
        {
            get => Preferences.Get("RareChance", 0.01);
            set => Preferences.Set("RareChance", Math.Clamp(value, 0.0, 1.0));
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
            get => Preferences.Get("MaxMobs", 25);
            set => Preferences.Set("MaxMobs", Math.Clamp(value, MinMobsValue, MaxMobsValue));
        }

        public static int MinRange
        {
            get => Preferences.Get("MinRange", 30);
            set => Preferences.Set("MinRange", Math.Clamp(value, MinRangeValue, MaxMinRangeValue));
        }

        public static int MaxRange
        {
            get => Preferences.Get("MaxRange", 80);
            set => Preferences.Set("MaxRange", Math.Clamp(value, MinRangeValue, MaxMaxRangeValue));
        }

        public static int MaxCrowd
        {
            get => Preferences.Get("MaxCrowd", 3);
            set => Preferences.Set("MaxCrowd", Math.Clamp(value, MinCrowdValue, MaxCrowdValue));
        }

        // ==================== NEW SETTINGS (Server 2.0.0.7) ====================

        /// <summary>
        /// Scale modifier for spawn limits when ENABLE_SCALE_SPAWN is true.
        /// Applied as multiplier to MAX_SPAWN, MIN_RANGE, MAX_RANGE, MAX_CROWD.
        /// </summary>
        public static double ScaleMod
        {
            get => Preferences.Get("ScaleMod", 1.0);
            set => Preferences.Set("ScaleMod", Math.Clamp(value, 0.1, 3.0));
        }

        // System Intervals
        /// <summary>How often to search for spawn locations per player (milliseconds)</summary>
        public static int SearchInterval
        {
            get => Preferences.Get("SearchInterval", 125);
            set => Preferences.Set("SearchInterval", Math.Clamp(value, MinIntervalValue, MaxIntervalValue));
        }

        /// <summary>How often to process the spawn queue (milliseconds)</summary>
        public static int ProcessInterval
        {
            get => Preferences.Get("ProcessInterval", 250);
            set => Preferences.Set("ProcessInterval", Math.Clamp(value, MinIntervalValue, MaxIntervalValue));
        }

        /// <summary>How often to validate existing spawns (seconds)</summary>
        public static int ValidateInterval
        {
            get => Preferences.Get("ValidateInterval", 5);
            set => Preferences.Set("ValidateInterval", Math.Clamp(value, 1, 60));
        }

        /// <summary>How often to check time-based spawns (minutes)</summary>
        public static int TimedInterval
        {
            get => Preferences.Get("TimedInterval", 1);
            set => Preferences.Set("TimedInterval", Math.Clamp(value, 1, 60));
        }

        // System Limits
        /// <summary>Max mobs cached per type in recycle pool</summary>
        public static int MaxRecycleType
        {
            get => Preferences.Get("MaxRecycleType", 20);
            set => Preferences.Set("MaxRecycleType", Math.Clamp(value, 1, 100));
        }

        /// <summary>Max attempts to find valid spawn point</summary>
        public static int MaxSpawnChecks
        {
            get => Preferences.Get("MaxSpawnChecks", 3);
            set => Preferences.Set("MaxSpawnChecks", Math.Clamp(value, 1, 10));
        }

        /// <summary>Max locations queued per player</summary>
        public static int MaxQueueSize
        {
            get => Preferences.Get("MaxQueueSize", 5);
            set => Preferences.Set("MaxQueueSize", Math.Clamp(value, 1, 10));
        }

        /// <summary>Max statistics entries</summary>
        public static int MaxStatSize
        {
            get => Preferences.Get("MaxStatSize", 10000);
            set => Preferences.Set("MaxStatSize", Math.Clamp(value, 100, 10000));
        }

        // New Spawn Toggles
        /// <summary>Allow spawns in town regions</summary>
        public static bool EnableTownSpawn
        {
            get => Preferences.Get("EnableTownSpawn", true);
            set => Preferences.Set("EnableTownSpawn", value);
        }

        /// <summary>Enable grave spawn effects</summary>
        public static bool EnableGraveSpawn
        {
            get => Preferences.Get("EnableGraveSpawn", true);
            set => Preferences.Set("EnableGraveSpawn", value);
        }

        /// <summary>Show spawn visual effects</summary>
        public static bool EnableSpawnEffects
        {
            get => Preferences.Get("EnableSpawnEffects", true);
            set => Preferences.Set("EnableSpawnEffects", value);
        }

        public static double WaterChance
        {
            get => Preferences.Get("WaterChance", 0.05);
            set => Preferences.Set("WaterChance", Math.Clamp(value, 0.0, 1.0));
        }

        public static double WeatherChance
        {
            get => Preferences.Get("WeatherChance", 0.01);
            set => Preferences.Set("WeatherChance", Math.Clamp(value, 0.0, 1.0));
        }

        /// <summary>
        /// Whether spawn scaling is enabled (adjusts spawn rates based on player population)
        /// </summary>
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
        /// Whether the intro splash animation plays on app launch.
        /// Default is true. User can toggle via nav bar button.
        /// </summary>
        public static bool SplashAnimationEnabled
        {
            get => Preferences.Get("SplashAnimationEnabled", true);
            set => Preferences.Set("SplashAnimationEnabled", value);
        }

        /// <summary>
        /// Version string to skip server update prompts for.
        /// When set, the editor won't prompt to update server scripts until the editor version changes.
        /// This allows users to keep custom server modifications without repeated prompts.
        /// Reset to empty when user accepts an update or when editor version changes.
        /// </summary>
        public static string SkipServerUpdateUntilVersion
        {
            get => Preferences.Get("SkipServerUpdateUntilVersion", "");
            set => Preferences.Set("SkipServerUpdateUntilVersion", value);
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
            if (string.IsNullOrEmpty(name) || BestiaryFavorites == null) return;

            BestiaryFavorites = ToggleFavorite(name, BestiaryFavorites);
        }

        /// <summary>
        /// Helper method to toggle a vendor as favorite
        /// </summary>
        public static void ToggleVendorFavorite(string name)
        {
            if (string.IsNullOrEmpty(name) || VendorFavorites == null) return;

            VendorFavorites = ToggleFavorite(name, VendorFavorites);
        }

        /// <summary>
        /// Helper method to toggle a vendor as favorite
        /// </summary>
        private static List<string> ToggleFavorite(string name, List<string> list)
        {
            if (!list.Remove(name))
            {
                list.Add(name);
            }

            return list;
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
            _cachedScriptsCustomFolder = "";
            _cachedServerDataFolder = "";
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
        /// Matches server 2.0.0.7 default values.
        /// </summary>
        public static void ResetSpawnSettingsToDefaults()
        {
            // Scale modifier
            ScaleMod = 1.0;

            // System intervals (matches server 2.0.1.2)
            SearchInterval = 125;
            ProcessInterval = 250;
            ValidateInterval = 5;
            TimedInterval = 1;

            // System limits
            MaxRecycleType = 20;
            MaxSpawnChecks = 3;
            MaxQueueSize = 5;
            MaxStatSize = 10000;

            // Spawn limits
            MaxMobs = 25;
            MinRange = 30;
            MaxRange = 80;
            MaxCrowd = 3;

            // Spawn chances (matches server 2.0.1.2)
            WaterChance = 0.05;
            WeatherChance = 0.01;
            TimedChance = 0.01;
            CommonChance = 1.0;
            UnCommonChance = 0.1;
            RareChance = 0.01;

            // Spawn toggles
            IsScaleSpawn = false;
            EnableRiftSpawn = false;
            EnableTownSpawn = true;
            EnableGraveSpawn = true;

            // Vendor toggles
            EnableVendorSpawn = false;
            EnableVendorNight = false;
            EnableVendorExtra = false;

            // Effects toggle
            EnableSpawnEffects = true;

            // Debug toggle
            EnableDebugSpawn = false;

            // UI appearance
            BoxColor = DefaultBoxColor;
            BoxColorInc = 0.3;
            BoxLineSize = 2;

            Logger.Info("Spawn settings reset to defaults");
        }

        /// <summary>
        /// Validates that MinRange is not greater than MaxRange.
        /// If invalid, adjusts MaxRange to match MinRange.
        /// Call this after loading settings or when user changes range values.
        /// </summary>
        /// <returns>True if ranges were valid, false if adjustment was made</returns>
        public static bool ValidateRanges()
        {
            if (MinRange > MaxRange)
            {
                MaxRange = MinRange;
                Logger.Warning($"Range validation: MaxRange adjusted to {MaxRange} (was less than MinRange)");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets a summary of all current settings for debugging/logging.
        /// </summary>
        public static string GetSettingsSummary()
        {
            return $"Settings Summary:\n" +
                   $"  Spawn Limits: MaxMobs={MaxMobs}, MinRange={MinRange}, MaxRange={MaxRange}, MaxCrowd={MaxCrowd}\n" +
                   $"  Chances: Common={CommonChance:P0}, Uncommon={UnCommonChance:P0}, Rare={RareChance:P0}\n" +
                   $"  Toggles: Scale={IsScaleSpawn}, Rift={EnableRiftSpawn}, Vendor={EnableVendorSpawn}\n" +
                   $"  Pack: {CurrentPackName}";
        }
    }
}