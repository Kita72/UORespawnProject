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
        private static string VERSION => Utility.Version;

        /// <summary>
        /// Writes all settings to a CSV file in server-compatible format.
        /// </summary>
        public static void WriteSettings(string filePath)
        {
            try
            {
                var sb = new System.Text.StringBuilder();

                // Header comments
                sb.AppendLine("# UORespawn Settings File");
                sb.AppendLine($"# Version: {VERSION}");
                sb.AppendLine("# Format: SettingName,Value");
                sb.AppendLine("# Lines starting with # are comments");
                sb.AppendLine();

                // Scale Modifier
                sb.AppendLine("# Scale Modifier");
                sb.AppendLine($"SCALE_MOD,{Settings.ScaleMod}");
                sb.AppendLine();

                // System Intervals
                sb.AppendLine("# System Intervals (milliseconds/seconds/minutes as noted)");
                sb.AppendLine($"SEARCH_INTERVAL,{Settings.SearchInterval}");
                sb.AppendLine($"PROCESS_INTERVAL,{Settings.ProcessInterval}");
                sb.AppendLine($"VALIDATE_INTERVAL,{Settings.ValidateInterval}");
                sb.AppendLine($"TIMED_INTERVAL,{Settings.TimedInterval}");
                sb.AppendLine();

                // System Limits
                sb.AppendLine("# System Limits");
                sb.AppendLine($"MAX_RECYCLE_TYPE,{Settings.MaxRecycleType}");
                sb.AppendLine($"MAX_SPAWN_CHECKS,{Settings.MaxSpawnChecks}");
                sb.AppendLine($"MAX_QUEUE_SIZE,{Settings.MaxQueueSize}");
                sb.AppendLine($"MAX_STAT_SIZE,{Settings.MaxStatSize}");
                sb.AppendLine();

                // Spawn Limits
                sb.AppendLine("# Spawn Limits");
                sb.AppendLine($"MAX_SPAWN,{Settings.MaxMobs}");
                sb.AppendLine($"MIN_RANGE,{Settings.MinRange}");
                sb.AppendLine($"MAX_RANGE,{Settings.MaxRange}");
                sb.AppendLine($"MAX_CROWD,{Settings.MaxCrowd}");
                sb.AppendLine();

                // Spawn Chances
                sb.AppendLine("# Spawn Chances (0.0 to 1.0)");
                sb.AppendLine($"CHANCE_WATER,{Settings.WaterChance}");
                sb.AppendLine($"CHANCE_WEATHER,{Settings.WeatherChance}");
                sb.AppendLine($"CHANCE_TIMED,{Settings.TimedChance}");
                sb.AppendLine($"CHANCE_COMMON,{Settings.CommonChance}");
                sb.AppendLine($"CHANCE_UNCOMMON,{Settings.UnCommonChance}");
                sb.AppendLine($"CHANCE_RARE,{Settings.RareChance}");
                sb.AppendLine();

                // Spawn Toggles
                sb.AppendLine("# Spawn Toggles (True/False)");
                sb.AppendLine($"ENABLE_SCALE_SPAWN,{Settings.IsScaleSpawn}");
                sb.AppendLine($"ENABLE_RIFT_SPAWN,{Settings.EnableRiftSpawn}");
                sb.AppendLine($"ENABLE_TOWN_SPAWN,{Settings.EnableTownSpawn}");
                sb.AppendLine($"ENABLE_GRAVE_SPAWN,{Settings.EnableGraveSpawn}");
                sb.AppendLine();

                // Vendor Toggles
                sb.AppendLine("# Vendor Toggles (True/False)");
                sb.AppendLine($"ENABLE_VENDOR_SPAWN,{Settings.EnableVendorSpawn}");
                sb.AppendLine($"ENABLE_VENDOR_NIGHT,{Settings.EnableVendorNight}");
                sb.AppendLine($"ENABLE_VENDOR_EXTRA,{Settings.EnableVendorExtra}");
                sb.AppendLine();

                // Effects Toggle
                sb.AppendLine("# Effects Toggle");
                sb.AppendLine($"ENABLE_SPAWN_EFFECTS,{Settings.EnableSpawnEffects}");
                sb.AppendLine();

                // Debug Toggle
                sb.AppendLine("# Debug Toggle");
                sb.AppendLine($"ENABLE_DEBUG,{Settings.EnableDebugSpawn}");

                FileUtility.WriteAllText(filePath, sb.ToString());
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
                var lines = FileUtility.ReadAllLines(filePath);

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
