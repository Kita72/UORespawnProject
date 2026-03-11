using System;
using System.IO;

using Server.Custom.UORespawnServer.Managers;

namespace Server.Custom.UORespawnServer
{
    internal static class UOR_Settings
    {
        internal const string VERSION = "2.0.1.4";

        // System Scaler (exposed for ControlService)
        internal static double SCALE_MOD { get; private set; } = 1.0;

        // System Intervals
        internal static int SEARCH_INTERVAL { get; set; } = 125; // Millisecons : Per Player, Location Search
        internal static int PROCESS_INTERVAL { get; set; } = 250; // Milliseconds : Spawn Processed (Created/Recycled)
        internal static int VALIDATE_INTERVAL { get; set; } = 5; // Seconds : Spawn Validation
        internal static int TIMED_INTERVAL { get; set; } = 1; // Minutes : Check Timed - IsNight

        // System Limits
        internal static int MAX_RECYCLE_TYPE { get; set; } = 20; // Max mobs cached per type

        internal static int MAX_SPAWN_CHECKS { get; set; } = 3; // Max mobs checked when searching
        internal static int MAX_QUEUE_SIZE { get; set; } = 5; // Max mobs qued
        internal static int MAX_STAT_SIZE { get; set; } = 10000; // Max stat points collected

        // Spawn Limits (backing fields exposed for ControlService)
        internal static int MAX_SPAWN_VAL { get; set; } = 25;
        internal static int MAX_SPAWN => SetScaleMod(MAX_SPAWN_VAL);

        internal static int MIN_RANGE_VAL { get; set; } = 30;
        internal static int MIN_RANGE => SetScaleMod(MIN_RANGE_VAL);

        internal static int MAX_RANGE_VAL { get; set; } = 80;
        internal static int MAX_RANGE => SetScaleMod(MAX_RANGE_VAL);

        internal static int MAX_CROWD_VAL { get; set; } = 3;
        internal static int MAX_CROWD => SetScaleMod(MAX_CROWD_VAL);

        // Spawn Chances
        internal static double CHANCE_WATER { get; set; } = 0.05;
        internal static double CHANCE_WEATHER { get; set; } = 0.01;
        internal static double CHANCE_TIMED { get; set; } = 0.01;
        internal static double CHANCE_COMMON { get; set; } = 1.0;
        internal static double CHANCE_UNCOMMON { get; set; } = 0.1;
        internal static double CHANCE_RARE { get; set; } = 0.01;

        // Spawn Toggles
        internal static bool ENABLE_SCALE_SPAWN { get; set; } = false;
        internal static bool ENABLE_RIFT_SPAWN { get; set; } = false;
        internal static bool ENABLE_TOWN_SPAWN { get; set; } = true;
        internal static bool ENABLE_GRAVE_SPAWN { get; set; } = true;

        // Vendor Toggles
        internal static bool ENABLE_VENDOR_SPAWN { get; set; } = false;
        internal static bool ENABLE_VENDOR_NIGHT { get; set; } = false;
        internal static bool ENABLE_VENDOR_EXTRA { get; set; } = false;

        // Effects Toggle
        internal static bool ENABLE_SPAWN_EFFECTS { get; set; } = true;

        // Debug Toggle
        internal static bool ENABLE_DEBUG { get; set; } = false;

        /// <summary>
        /// Loads settings from CSV file. Format: SettingName,Value (one per line, # for comments)
        /// </summary>
        internal static void LoadSpawnSettings()
        {
            if (!File.Exists(UOR_DIR.SETTINGS_DATA_FILE)) return;

            var lines = File.ReadAllLines(UOR_DIR.SETTINGS_DATA_FILE);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                // Split on comma: SettingName,Value
                int commaIndex = line.IndexOf(',');
                if (commaIndex <= 0) continue;

                string key = line.Substring(0, commaIndex).Trim();
                string value = line.Substring(commaIndex + 1).Trim();

                ApplySetting(key, value);
            }

            ValidateSettings();
        }

        private static void ApplySetting(string key, string value)
        {
            switch (key.ToUpperInvariant())
            {
                // Scale Modifier
                case "SCALE_MOD":
                    if (double.TryParse(value, out double scaleMod)) SCALE_MOD = scaleMod;
                    break;

                // System Intervals
                case "SEARCH_INTERVAL":
                    if (int.TryParse(value, out int searchInt)) SEARCH_INTERVAL = searchInt;
                    break;
                case "PROCESS_INTERVAL":
                    if (int.TryParse(value, out int processInt)) PROCESS_INTERVAL = processInt;
                    break;
                case "VALIDATE_INTERVAL":
                    if (int.TryParse(value, out int validateInt)) VALIDATE_INTERVAL = validateInt;
                    break;
                case "TIMED_INTERVAL":
                    if (int.TryParse(value, out int timedInt)) TIMED_INTERVAL = timedInt;
                    break;

                // System Limits
                case "MAX_RECYCLE_TYPE":
                    if (int.TryParse(value, out int recycleType)) MAX_RECYCLE_TYPE = recycleType;
                    break;
                case "MAX_SPAWN_CHECKS":
                    if (int.TryParse(value, out int spawnChecks)) MAX_SPAWN_CHECKS = spawnChecks;
                    break;
                case "MAX_QUEUE_SIZE":
                    if (int.TryParse(value, out int queueSize)) MAX_QUEUE_SIZE = queueSize;
                    break;
                case "MAX_STAT_SIZE":
                    if (int.TryParse(value, out int statSize)) MAX_STAT_SIZE = statSize;
                    break;

                // Spawn Limits
                case "MAX_SPAWN":
                    if (int.TryParse(value, out int maxSpawnVal)) MAX_SPAWN_VAL = maxSpawnVal;
                    break;
                case "MIN_RANGE":
                    if (int.TryParse(value, out int minRangeVal)) MIN_RANGE_VAL = minRangeVal;
                    break;
                case "MAX_RANGE":
                    if (int.TryParse(value, out int maxRangeVal)) MAX_RANGE_VAL = maxRangeVal;
                    break;
                case "MAX_CROWD":
                    if (int.TryParse(value, out int maxCrowdVal)) MAX_CROWD_VAL = maxCrowdVal;
                    break;

                // Spawn Chances
                case "CHANCE_WATER":
                    if (double.TryParse(value, out double chanceWater)) CHANCE_WATER = chanceWater;
                    break;
                case "CHANCE_WEATHER":
                    if (double.TryParse(value, out double chanceWeather)) CHANCE_WEATHER = chanceWeather;
                    break;
                case "CHANCE_TIMED":
                    if (double.TryParse(value, out double chanceTimed)) CHANCE_TIMED = chanceTimed;
                    break;
                case "CHANCE_COMMON":
                    if (double.TryParse(value, out double chanceCommon)) CHANCE_COMMON = chanceCommon;
                    break;
                case "CHANCE_UNCOMMON":
                    if (double.TryParse(value, out double chanceUncommon)) CHANCE_UNCOMMON = chanceUncommon;
                    break;
                case "CHANCE_RARE":
                    if (double.TryParse(value, out double chanceRare)) CHANCE_RARE = chanceRare;
                    break;

                // Spawn Toggles
                case "ENABLE_SCALE_SPAWN":
                    if (bool.TryParse(value, out bool enableScale)) ENABLE_SCALE_SPAWN = enableScale;
                    break;
                case "ENABLE_RIFT_SPAWN":
                    if (bool.TryParse(value, out bool enableRift)) ENABLE_RIFT_SPAWN = enableRift;
                    break;
                case "ENABLE_TOWN_SPAWN":
                    if (bool.TryParse(value, out bool enableTown)) ENABLE_TOWN_SPAWN = enableTown;
                    break;
                case "ENABLE_GRAVE_SPAWN":
                    if (bool.TryParse(value, out bool enableGrave)) ENABLE_GRAVE_SPAWN = enableGrave;
                    break;

                // Vendor Toggles
                case "ENABLE_VENDOR_SPAWN":
                    if (bool.TryParse(value, out bool enableVendor)) ENABLE_VENDOR_SPAWN = enableVendor;
                    break;
                case "ENABLE_VENDOR_NIGHT":
                    if (bool.TryParse(value, out bool enableNight)) ENABLE_VENDOR_NIGHT = enableNight;
                    break;
                case "ENABLE_VENDOR_EXTRA":
                    if (bool.TryParse(value, out bool enableExtra)) ENABLE_VENDOR_EXTRA = enableExtra;
                    break;

                // Effects Toggle
                case "ENABLE_SPAWN_EFFECTS":
                    if (bool.TryParse(value, out bool enableEffects)) ENABLE_SPAWN_EFFECTS = enableEffects;
                    break;

                // Debug Toggle
                case "ENABLE_DEBUG":
                    if (bool.TryParse(value, out bool enableDebug)) ENABLE_DEBUG = enableDebug;
                    break;
            }
        }

        private static void ValidateSettings()
        {
            SCALE_MOD = ClampDouble(SCALE_MOD, 0.1, 3.0, "SCALE_MOD");

            SEARCH_INTERVAL = ClampInt(SEARCH_INTERVAL, 50, 2000, "SEARCH_INTERVAL");
            PROCESS_INTERVAL = ClampInt(PROCESS_INTERVAL, 50, 2000, "PROCESS_INTERVAL");
            VALIDATE_INTERVAL = ClampInt(VALIDATE_INTERVAL, 1, 60, "VALIDATE_INTERVAL");
            TIMED_INTERVAL = ClampInt(TIMED_INTERVAL, 1, 60, "TIMED_INTERVAL");

            MAX_RECYCLE_TYPE = ClampInt(MAX_RECYCLE_TYPE, 1, 100, "MAX_RECYCLE_TYPE");
            MAX_SPAWN_CHECKS = ClampInt(MAX_SPAWN_CHECKS, 1, 10, "MAX_SPAWN_CHECKS");
            MAX_QUEUE_SIZE = ClampInt(MAX_QUEUE_SIZE, 1, 10, "MAX_QUEUE_SIZE");
            MAX_STAT_SIZE = ClampInt(MAX_STAT_SIZE, 100, 10000, "MAX_STAT_SIZE");

            MAX_SPAWN_VAL = ClampInt(MAX_SPAWN_VAL, 5, 75, "MAX_SPAWN");
            MIN_RANGE_VAL = ClampInt(MIN_RANGE_VAL, 5, 125, "MIN_RANGE");
            MAX_RANGE_VAL = ClampInt(MAX_RANGE_VAL, 5, 250, "MAX_RANGE");
            MAX_CROWD_VAL = ClampInt(MAX_CROWD_VAL, 1, 10, "MAX_CROWD");

            if (MIN_RANGE_VAL > MAX_RANGE_VAL)
            {
                MIN_RANGE_VAL = MAX_RANGE_VAL;
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"SETTINGS-[Clamped MIN_RANGE to MAX_RANGE ({MAX_RANGE_VAL})]");
            }

            CHANCE_WATER = ClampDouble(CHANCE_WATER, 0.0, 1.0, "CHANCE_WATER");
            CHANCE_WEATHER = ClampDouble(CHANCE_WEATHER, 0.0, 1.0, "CHANCE_WEATHER");
            CHANCE_TIMED = ClampDouble(CHANCE_TIMED, 0.0, 1.0, "CHANCE_TIMED");
            CHANCE_COMMON = ClampDouble(CHANCE_COMMON, 0.0, 1.0, "CHANCE_COMMON");
            CHANCE_UNCOMMON = ClampDouble(CHANCE_UNCOMMON, 0.0, 1.0, "CHANCE_UNCOMMON");
            CHANCE_RARE = ClampDouble(CHANCE_RARE, 0.0, 1.0, "CHANCE_RARE");
        }

        private static int ClampInt(int value, int min, int max, string name)
        {
            int clamped = Math.Max(min, Math.Min(max, value));

            if (value != clamped)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"SETTINGS-[Clamped {name} to {clamped}]");
            }

            return clamped;
        }

        private static double ClampDouble(double value, double min, double max, string name)
        {
            double clamped = Math.Max(min, Math.Min(max, value));

            if (Math.Abs(value - clamped) > double.Epsilon)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"SETTINGS-[Clamped {name} to {clamped:0.###}]");
            }

            return clamped;
        }

        // System Spawn Scaler
        private static int SetScaleMod(int stat)
        {
            if (ENABLE_SCALE_SPAWN)
            {
                return (int)(stat * SCALE_MOD);
            }

            return stat;
        }

        internal static void UpdateScaleMod(double mod)
        {
            SCALE_MOD = mod;
        }

        /// <summary>
        /// Applies a setting from a command (used by CommandManager).
        /// Returns true if the setting was recognized and applied.
        /// </summary>
        internal static bool ApplySettingCommand(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return false;

            string normalizedKey = key.ToUpperInvariant();

            // Check if this is a known setting
            switch (normalizedKey)
            {
                case "SCALE_MOD":
                case "SEARCH_INTERVAL":
                case "PROCESS_INTERVAL":
                case "VALIDATE_INTERVAL":
                case "TIMED_INTERVAL":
                case "MAX_RECYCLE_TYPE":
                case "MAX_RECYCLE_TOTAL":
                case "MAX_SPAWN_CHECKS":
                case "MAX_QUEUE_SIZE":
                case "MAX_STAT_SIZE":
                case "MAX_SPAWN":
                case "MIN_RANGE":
                case "MAX_RANGE":
                case "MAX_CROWD":
                case "CHANCE_WATER":
                case "CHANCE_WEATHER":
                case "CHANCE_TIMED":
                case "CHANCE_COMMON":
                case "CHANCE_UNCOMMON":
                case "CHANCE_RARE":
                case "ENABLE_SCALE_SPAWN":
                case "ENABLE_RIFT_SPAWN":
                case "ENABLE_TOWN_SPAWN":
                case "ENABLE_GRAVE_SPAWN":
                case "ENABLE_VENDOR_SPAWN":
                case "ENABLE_VENDOR_NIGHT":
                case "ENABLE_VENDOR_EXTRA":
                case "ENABLE_SPAWN_EFFECTS":
                case "ENABLE_DEBUG":
                    ApplySetting(key, value);
                    return true;
                default:
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"SETTINGS COMMAND-[Unknown setting: {key}]");
                    return false;
            }
        }
    }
}
