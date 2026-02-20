using System;
using System.IO;

using Server.Custom.UORespawnSystem.Enums;
using Server.Custom.UORespawnSystem.SpawnUtility;

namespace Server.Custom.UORespawnSystem
{
    internal static class UORespawnSettings
    {
        internal static readonly string UOR_DATA = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_DATA");

        internal static void VerifyDirectories()
        {
            if (!Directory.Exists(UOR_DATA))
            {
                Directory.CreateDirectory(UOR_DATA);
            }
        }

        private const string Version = "2.0.0.5";
        private static double SPAWN_MOD { get; set; } = 0.0;

        private static readonly string SettingsBinaryFile = Path.Combine(UOR_DATA, "UOR_SpawnSettings.bin");

        // Settings From File
        private static int maxMobs = 15;
        internal static int MAX_MOBS => GetSpawnMod(maxMobs);

        private static int minRange = 10;
        internal static int MIN_RANGE => GetSpawnMod(minRange);

        private static int maxRange = 50;
        internal static int MAX_RANGE => GetSpawnMod(maxRange);

        private static int maxCrowd = 1;
        internal static int MAX_CROWD => GetSpawnMod(maxCrowd);

        internal static double CHANCE_WATER { get; private set; } = 0.5;
        internal static double CHANCE_WEATHER { get; private set; } = 0.1;
        internal static double CHANCE_TIMED { get; private set; } = 0.1;
        internal static double CHANCE_COMMON { get; private set; } = 1.0;
        internal static double CHANCE_UNCOMMON { get; private set; } = 0.5;
        internal static double CHANCE_RARE { get; private set; } = 0.1;
        internal static bool ENABLE_SCALE_SPAWN { get; private set; } = false;
        internal static bool ENABLE_RIFT_SPAWN { get; private set; } = false;
        internal static bool ENABLE_DEBUG { get; set; } = false;
        internal static bool ENABLE_VENDOR_SPAWN { get; set; } = false;

        // Performance & System Settings (hardcoded for stability)
        internal static int INTERVAL { get; private set; } = 50; // ms Main System Timer
        internal static int BATCH_SIZE { get; set; } = 5; // Players processed per timer tick
        internal static int DISTANCE_INTERVAL { get; set; } = 1000; // ms Distance cleanup runs once per second
        internal static int CLEANUP_INTERVAL { get; set; } = 10; // Seconds : Cleanup service timer (separate from spawn timer)
        internal static int MAX_RECYCLE_TYPE { get; set; } = 20; // Max recycled mobs per type
        internal static int MAX_RECYCLE_TOTAL { get; set; } = 50000; // Max total recycled mobs
        internal static int MAX_SPAWN_CHECKS { get; set; } = 5; // Max mobs checked when searching
        internal static int MAX_QUEUE_SIZE { get; set; } = 5; // Max mobs qued
        internal static int MAX_STAT_SIZE { get; set; } = 1000; // Max stat points for stats data / heatmap in editor!

        internal static void UpdateStats(double mod)
        {
            SPAWN_MOD = mod;
        }

        internal static void LoadSpawnSettings()
        {
            if (File.Exists(SettingsBinaryFile))
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Cyan, "Loading settings...");

                LoadSpawnSettingsData();
            }
            else
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: Settings file not found - Use Editor to create UOR_SpawnSettings.bin (Using default values)");
            }
        }

        /// <summary>
        /// Load settings from Binary format using BinaryReader (matches App format)
        /// Format: Version(int), VersionString(string), MaxMobs, MinRange, MaxRange, MaxCrowd,
        ///         WaterChance, WeatherChance, TimedChance, CommonChance, UncommonChance, RareChance,
        ///         ScaleSpawn, RiftSpawn, Debug (all bools)
        /// </summary>
        private static void LoadSpawnSettingsData()
        {
            try
            {
                if (!File.Exists(SettingsBinaryFile))
                    return;

                using (BinaryReader reader = new BinaryReader(File.Open(SettingsBinaryFile, FileMode.Open, FileAccess.Read)))
                {
                    int fileVersion = reader.ReadInt32();
                    string versionString = reader.ReadString();

                    // Version validation (optional - just log warning)
                    if (string.IsNullOrWhiteSpace(versionString))
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: Settings binary has no version info");
                    }
                    else if (versionString != Version)
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                            $"WARNING: Settings version mismatch (File: {versionString}, Expected: {Version})");
                    }

                    // Basic spawn limits
                    maxMobs = reader.ReadInt32();
                    minRange = reader.ReadInt32();
                    maxRange = reader.ReadInt32();
                    maxCrowd = reader.ReadInt32();

                    // Spawn chances (doubles)
                    CHANCE_WATER = reader.ReadDouble();
                    CHANCE_WEATHER = reader.ReadDouble();
                    CHANCE_TIMED = reader.ReadDouble();
                    CHANCE_COMMON = reader.ReadDouble();
                    CHANCE_UNCOMMON = reader.ReadDouble();
                    CHANCE_RARE = reader.ReadDouble();

                    // Feature flags
                    ENABLE_SCALE_SPAWN = reader.ReadBoolean();
                    ENABLE_RIFT_SPAWN = reader.ReadBoolean();
                    ENABLE_DEBUG = reader.ReadBoolean();

                    if (fileVersion > 1)
                    {
                        ENABLE_VENDOR_SPAWN = reader.ReadBoolean();
                    }
                }

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Settings: Loaded successfully");
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                    $"ERROR: Failed to load settings binary - {ex.Message}");
            }
        }

        /// <summary>
        /// Placeholder: Binary save should be done from Editor
        /// TODO: Future feature for in-game editing
        /// </summary>
        internal static void SaveSpawnSettingsData()
        {
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "WARNING: Not implemented!");
            // TODO: Implement when in-game editing feature is added
        }

        internal static int GetSpawnMod(int stat)
        {
            if (SPAWN_MOD > 0)
            {
                double result = stat * SPAWN_MOD;

                return (int)result + stat;
            }

            return stat;
        }

        internal static Frequency GetFreq(double chance)
        {
            if (chance <= CHANCE_RARE)
            {
                return Frequency.Rare;
            }

            if (chance <= CHANCE_UNCOMMON)
            {
                return Frequency.UnCommon;
            }

            return Frequency.Common;
        }
    }
}
