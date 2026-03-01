using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    /// <summary>
    /// Service for reading and writing UOR_SpawnSettings.csv files.
    /// Format matches server 2.0.0.7 UOR_Settings.cs CSV format:
    /// - Lines starting with # are comments
    /// - Format: SettingName,Value
    /// </summary>
    public static class CsvSettingsService
    {
        private const string VERSION = "2.0.0.7";

        /// <summary>
        /// Writes all settings to a CSV file in server-compatible format.
        /// </summary>
        public static void WriteSettings(string filePath)
        {
            try
            {
                using var writer = new StreamWriter(filePath);

                // Header comments
                writer.WriteLine("# UORespawn Settings File");
                writer.WriteLine($"# Version: {VERSION}");
                writer.WriteLine("# Format: SettingName,Value");
                writer.WriteLine("# Lines starting with # are comments");
                writer.WriteLine();

                // Scale Modifier
                writer.WriteLine("# Scale Modifier");
                writer.WriteLine($"SCALE_MOD,{Settings.ScaleMod}");
                writer.WriteLine();

                // System Intervals
                writer.WriteLine("# System Intervals (milliseconds/seconds/minutes as noted)");
                writer.WriteLine($"SEARCH_INTERVAL,{Settings.SearchInterval}");
                writer.WriteLine($"PROCESS_INTERVAL,{Settings.ProcessInterval}");
                writer.WriteLine($"VALIDATE_INTERVAL,{Settings.ValidateInterval}");
                writer.WriteLine($"TIMED_INTERVAL,{Settings.TimedInterval}");
                writer.WriteLine();

                // System Limits
                writer.WriteLine("# System Limits");
                writer.WriteLine($"MAX_RECYCLE_TYPE,{Settings.MaxRecycleType}");
                writer.WriteLine($"MAX_RECYCLE_TOTAL,{Settings.MaxRecycleTotal}");
                writer.WriteLine($"MAX_SPAWN_CHECKS,{Settings.MaxSpawnChecks}");
                writer.WriteLine($"MAX_QUEUE_SIZE,{Settings.MaxQueueSize}");
                writer.WriteLine($"MAX_STAT_SIZE,{Settings.MaxStatSize}");
                writer.WriteLine();

                // Spawn Limits
                writer.WriteLine("# Spawn Limits");
                writer.WriteLine($"MAX_SPAWN,{Settings.MaxMobs}");
                writer.WriteLine($"MIN_RANGE,{Settings.MinRange}");
                writer.WriteLine($"MAX_RANGE,{Settings.MaxRange}");
                writer.WriteLine($"MAX_CROWD,{Settings.MaxCrowd}");
                writer.WriteLine();

                // Spawn Chances
                writer.WriteLine("# Spawn Chances (0.0 to 1.0)");
                writer.WriteLine($"CHANCE_WATER,{Settings.WaterChance}");
                writer.WriteLine($"CHANCE_WEATHER,{Settings.WeatherChance}");
                writer.WriteLine($"CHANCE_TIMED,{Settings.TimedChance}");
                writer.WriteLine($"CHANCE_COMMON,{Settings.CommonChance}");
                writer.WriteLine($"CHANCE_UNCOMMON,{Settings.UnCommonChance}");
                writer.WriteLine($"CHANCE_RARE,{Settings.RareChance}");
                writer.WriteLine();

                // Spawn Toggles
                writer.WriteLine("# Spawn Toggles (True/False)");
                writer.WriteLine($"ENABLE_SCALE_SPAWN,{Settings.IsScaleSpawn}");
                writer.WriteLine($"ENABLE_RIFT_SPAWN,{Settings.EnableRiftSpawn}");
                writer.WriteLine($"ENABLE_TOWN_SPAWN,{Settings.EnableTownSpawn}");
                writer.WriteLine($"ENABLE_GRAVE_SPAWN,{Settings.EnableGraveSpawn}");
                writer.WriteLine();

                // Vendor Toggles
                writer.WriteLine("# Vendor Toggles (True/False)");
                writer.WriteLine($"ENABLE_VENDOR_SPAWN,{Settings.EnableVendorSpawn}");
                writer.WriteLine($"ENABLE_VENDOR_NIGHT,{Settings.EnableVendorNight}");
                writer.WriteLine($"ENABLE_VENDOR_EXTRA,{Settings.EnableVendorExtra}");
                writer.WriteLine();

                // Effects Toggle
                writer.WriteLine("# Effects Toggle");
                writer.WriteLine($"ENABLE_SPAWN_EFFECTS,{Settings.EnableSpawnEffects}");
                writer.WriteLine();

                // Debug Toggle
                writer.WriteLine("# Debug Toggle");
                writer.WriteLine($"ENABLE_DEBUG,{Settings.EnableDebugSpawn}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error writing settings CSV to {filePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// Reads settings from a CSV file and populates Settings properties.
        /// </summary>
        public static void ReadSettings(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Logger.Warning($"Settings CSV file not found: {filePath}");
                return;
            }

            try
            {
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                        continue;

                    // Parse SettingName,Value
                    var commaIndex = trimmed.IndexOf(',');
                    if (commaIndex <= 0)
                        continue;

                    var key = trimmed[..commaIndex].Trim();
                    var value = trimmed[(commaIndex + 1)..].Trim();

                    ApplySetting(key, value);
                }

                Logger.Info($"Settings loaded from CSV: {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reading settings CSV from {filePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// Applies a single setting by key/value pair.
        /// Keys match server UOR_Settings.cs format (uppercase with underscores).
        /// </summary>
        private static void ApplySetting(string key, string value)
        {
            switch (key.ToUpperInvariant())
            {
                // Scale Modifier
                case "SCALE_MOD":
                    if (double.TryParse(value, out var scaleMod))
                        Settings.ScaleMod = scaleMod;
                    break;

                // System Intervals
                case "SEARCH_INTERVAL":
                    if (int.TryParse(value, out var searchInt))
                        Settings.SearchInterval = searchInt;
                    break;
                case "PROCESS_INTERVAL":
                    if (int.TryParse(value, out var processInt))
                        Settings.ProcessInterval = processInt;
                    break;
                case "VALIDATE_INTERVAL":
                    if (int.TryParse(value, out var validateInt))
                        Settings.ValidateInterval = validateInt;
                    break;
                case "TIMED_INTERVAL":
                    if (int.TryParse(value, out var timedInt))
                        Settings.TimedInterval = timedInt;
                    break;

                // System Limits
                case "MAX_RECYCLE_TYPE":
                    if (int.TryParse(value, out var recycleType))
                        Settings.MaxRecycleType = recycleType;
                    break;
                case "MAX_RECYCLE_TOTAL":
                    if (int.TryParse(value, out var recycleTotal))
                        Settings.MaxRecycleTotal = recycleTotal;
                    break;
                case "MAX_SPAWN_CHECKS":
                    if (int.TryParse(value, out var spawnChecks))
                        Settings.MaxSpawnChecks = spawnChecks;
                    break;
                case "MAX_QUEUE_SIZE":
                    if (int.TryParse(value, out var queueSize))
                        Settings.MaxQueueSize = queueSize;
                    break;
                case "MAX_STAT_SIZE":
                    if (int.TryParse(value, out var statSize))
                        Settings.MaxStatSize = statSize;
                    break;

                // Spawn Limits
                case "MAX_SPAWN":
                    if (int.TryParse(value, out var maxSpawn))
                        Settings.MaxMobs = maxSpawn;
                    break;
                case "MIN_RANGE":
                    if (int.TryParse(value, out var minRange))
                        Settings.MinRange = minRange;
                    break;
                case "MAX_RANGE":
                    if (int.TryParse(value, out var maxRange))
                        Settings.MaxRange = maxRange;
                    break;
                case "MAX_CROWD":
                    if (int.TryParse(value, out var maxCrowd))
                        Settings.MaxCrowd = maxCrowd;
                    break;

                // Spawn Chances
                case "CHANCE_WATER":
                    if (double.TryParse(value, out var chanceWater))
                        Settings.WaterChance = ClampAndRound(chanceWater);
                    break;
                case "CHANCE_WEATHER":
                    if (double.TryParse(value, out var chanceWeather))
                        Settings.WeatherChance = ClampAndRound(chanceWeather);
                    break;
                case "CHANCE_TIMED":
                    if (double.TryParse(value, out var chanceTimed))
                        Settings.TimedChance = ClampAndRound(chanceTimed);
                    break;
                case "CHANCE_COMMON":
                    if (double.TryParse(value, out var chanceCommon))
                        Settings.CommonChance = ClampAndRound(chanceCommon);
                    break;
                case "CHANCE_UNCOMMON":
                    if (double.TryParse(value, out var chanceUncommon))
                        Settings.UnCommonChance = ClampAndRound(chanceUncommon);
                    break;
                case "CHANCE_RARE":
                    if (double.TryParse(value, out var chanceRare))
                        Settings.RareChance = ClampAndRound(chanceRare);
                    break;

                // Spawn Toggles
                case "ENABLE_SCALE_SPAWN":
                    if (bool.TryParse(value, out var enableScale))
                        Settings.IsScaleSpawn = enableScale;
                    break;
                case "ENABLE_RIFT_SPAWN":
                    if (bool.TryParse(value, out var enableRift))
                        Settings.EnableRiftSpawn = enableRift;
                    break;
                case "ENABLE_TOWN_SPAWN":
                    if (bool.TryParse(value, out var enableTown))
                        Settings.EnableTownSpawn = enableTown;
                    break;
                case "ENABLE_GRAVE_SPAWN":
                    if (bool.TryParse(value, out var enableGrave))
                        Settings.EnableGraveSpawn = enableGrave;
                    break;

                // Vendor Toggles
                case "ENABLE_VENDOR_SPAWN":
                    if (bool.TryParse(value, out var enableVendor))
                        Settings.EnableVendorSpawn = enableVendor;
                    break;
                case "ENABLE_VENDOR_NIGHT":
                    if (bool.TryParse(value, out var enableNight))
                        Settings.EnableVendorNight = enableNight;
                    break;
                case "ENABLE_VENDOR_EXTRA":
                    if (bool.TryParse(value, out var enableExtra))
                        Settings.EnableVendorExtra = enableExtra;
                    break;

                // Effects Toggle
                case "ENABLE_SPAWN_EFFECTS":
                    if (bool.TryParse(value, out var enableEffects))
                        Settings.EnableSpawnEffects = enableEffects;
                    break;

                // Debug Toggle
                case "ENABLE_DEBUG":
                    if (bool.TryParse(value, out var enableDebug))
                        Settings.EnableDebugSpawn = enableDebug;
                    break;
            }
        }

        /// <summary>
        /// Clamps a double to 0.0-1.0 range and rounds to 2 decimal places.
        /// Ensures clean display values like 0.01, 0.25, 1.0.
        /// </summary>
        private static double ClampAndRound(double value)
        {
            value = Math.Max(0.0, Math.Min(1.0, value));
            return Math.Round(value, 2);
        }
    }
}
