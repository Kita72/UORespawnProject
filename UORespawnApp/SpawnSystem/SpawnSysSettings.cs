using System;
using System.IO;
using System.Linq;

namespace Server.Custom.SpawnSystem
{
    internal static class SpawnSysSettings
    {
        private const string Version = "2.0.0.1";

        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_SpawnSettings.csv");

        private static int maxMobs = 15;
        internal static int MAX_MOBS => GetSpawnMod(maxMobs);

        private static int minRange = 10;
        internal static int MIN_RANGE => GetSpawnMod(minRange);

        private static int maxRange = 50;
        internal static int MAX_RANGE => GetSpawnMod(maxRange);

        private static int maxCrowd = 1;
        internal static int MAX_CROWD => GetSpawnMod(maxCrowd);

        internal static int INTERVAL { get; private set; } = 50;
        internal static int MIN_QUE { get; private set; } = 1;
        internal static double CHANCE_WATER { get; private set; } = 0.5;
        internal static double CHANCE_WEATHER { get; private set; } = 0.1;
        internal static double CHANCE_STATIC { get; private set; } = 0.1;
        internal static double CHANCE_CREATURE { get; private set; } = 0.1;
        internal static double CHANCE_COMMON { get; private set; } = 1.0;
        internal static double CHANCE_UNCOMMON { get; private set; } = 0.5;
        internal static double CHANCE_RARE { get; private set; } = 0.1;
        internal static double SPAWN_MOD { get; private set; } = 0.0;
        internal static bool SCALE_SPAWN { get; private set; } = false;
        internal static bool ENABLE_RIFTSPAWN { get; private set; } = false;
        internal static bool ENABLE_DEBUG { get; set; } = false;

        internal static void UpdateStats(double mod)
        {
            SPAWN_MOD = mod;
        }

        internal static void LoadSpawnSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    var lines = File.ReadLines(SettingsFile).ToArray();

                    if (lines.Length == 15)
                    {
                        if (int.TryParse(lines[0].Split(':').Last(), out int val))
                        {
                            maxMobs = val;
                        }

                        if (int.TryParse(lines[1].Split(':').Last(), out val))
                        {
                            minRange = val;
                        }

                        if (int.TryParse(lines[2].Split(':').Last(), out val))
                        {
                            maxRange = val;
                        }

                        if (int.TryParse(lines[3].Split(':').Last(), out val))
                        {
                            maxCrowd = val;
                        }

                        if (double.TryParse(lines[4].Split(':').Last(), out double chance))
                        {
                            CHANCE_WATER = chance;
                        }

                        if (double.TryParse(lines[5].Split(':').Last(), out chance))
                        {
                            CHANCE_WEATHER = chance;
                        }

                        if (double.TryParse(lines[6].Split(':').Last(), out chance))
                        {
                            CHANCE_STATIC = chance;
                        }

                        if (bool.TryParse(lines[7].Split(':').Last(), out bool enable))
                        {
                            SCALE_SPAWN = enable;
                        }

                        if (double.TryParse(lines[8].Split(':').Last(), out chance))
                        {
                            CHANCE_CREATURE = chance;
                        }

                        if (double.TryParse(lines[9].Split(':').Last(), out chance))
                        {
                            CHANCE_COMMON = chance;
                        }

                        if (double.TryParse(lines[10].Split(':').Last(), out chance))
                        {
                            CHANCE_UNCOMMON = chance;
                        }

                        if (double.TryParse(lines[11].Split(':').Last(), out chance))
                        {
                            CHANCE_RARE = chance;
                        }

                        if (bool.TryParse(lines[12].Split(':').Last(), out enable))
                        {
                            ENABLE_RIFTSPAWN = enable;
                        }

                        if (bool.TryParse(lines[13].Split(':').Last(), out enable))
                        {
                            ENABLE_DEBUG = enable;
                        }

                        string version = lines[14].Split(':').Last();

                        if (!string.IsNullOrEmpty(version))
                        {
                            if (Version != version)
                            {
                                SpawnSysUtility.SendConsoleMsg(ConsoleColor.Yellow, "Update Needed => Update Scripts!");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkRed, $"Loading Spawn Chance Error: {ex.Message}");
            }
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
