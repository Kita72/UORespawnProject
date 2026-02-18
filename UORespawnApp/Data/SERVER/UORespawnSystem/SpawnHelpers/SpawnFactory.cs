using System;
using System.IO;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Custom.UORespawnSystem.Mobiles;
using Server.Custom.UORespawnSystem.SpawnUtility;
using Server.Custom.UORespawnSystem.Interfaces;
using Server.Custom.UORespawnSystem.Enums;
using Server.Items;

namespace Server.Custom.UORespawnSystem.SpawnHelpers
{
    internal static class SpawnFactory
    {
        private static readonly string STAT_DIR = Path.Combine(UORespawnSettings.UOR_DATA, "UOR_STATS");

        private static List<(DateTime, string, string, Point2D, Point2D, string)> SpawnStats { get; set; }

        internal static void AddStats(string playerName, string mapName, Point2D playerLoc, Point2D creatureLocation, string creatureName)
        {
            if (SpawnStats == null) SpawnStats = new List<(DateTime, string, string, Point2D, Point2D, string)>();

            SpawnStats.Add((DateTime.Now, playerName, mapName, playerLoc, creatureLocation, creatureName));
        }

        internal static string GetSpawnName(PlayerMobile pm, Map map, Region region, Point3D location, bool isWater)
        {
            if (SpawnStats?.Count > 1000)
            {
                SpawnStats.RemoveAt(0);
            }

            string spawn;

            // Box
            spawn = BoxSpawner.TryBoxSpawn(map, location, isWater);

            if (!string.IsNullOrEmpty(spawn))
            {
                return spawn;
            }

            // Region

            spawn = RegionSpawner.TryRegionSpawn(map, region, location, isWater);

            if (!string.IsNullOrEmpty(spawn))
            {
                return spawn;
            }

            // Tile
            spawn = TileSpawner.TryTileSpawn(map, location, isWater);

            if (!string.IsNullOrEmpty(spawn))
            {
                return spawn;
            }

            // Staff - Debug
            if (pm.IsStaff() && UORespawnSettings.ENABLE_DEBUG)
            {
                return nameof(PlaceHolder);
            }

            return string.Empty;
        }

        internal static string GetSpawnEntity(ISpawnEntity entity, Map map, Point3D location, bool isWater)
        {
            string spawnName = string.Empty;

            if (isWater)
            {
                return UORespawnUtility.GetSpawnFromList(entity, Frequency.Water);
            }

            if (string.IsNullOrEmpty(spawnName) && entity.WeatherSpawn != WeatherTypes.None)
            {
                WeatherTypes weather = SpawnWeatherInfo.GetWeatherInfo(map, location);

                if (weather == entity.WeatherSpawn)
                {
                    if (UORespawnSettings.ENABLE_RIFT_SPAWN && (map == Map.Trammel || map == Map.Felucca))
                    {
                        if (UORespawnSettings.CHANCE_WEATHER > Utility.RandomDouble())
                        {
                            switch (weather)
                            {
                                case WeatherTypes.Rain:     return nameof(Jwilson);
                                case WeatherTypes.Snow:     return nameof(Jwilson);
                                case WeatherTypes.Storm:    return nameof(RiftMob);
                                case WeatherTypes.Blizzard: return nameof(RiftMob);
                            }
                        }
                    }

                    spawnName = UORespawnUtility.GetSpawnFromList(entity, Frequency.Weather);
                }
            }

            if (string.IsNullOrEmpty(spawnName) && entity.TimedSpawn != TimeNames.None)
            {
                Clock.GetTime(map, location.X, location.Y, out int hour, out int _);

                if (SpawnTimeInfo.IsSpawnTime(entity.TimedSpawn, hour))
                {
                    spawnName = UORespawnUtility.GetSpawnFromList(entity, Frequency.Timed);
                }
            }

            if (string.IsNullOrEmpty(spawnName))
            {
                spawnName = UORespawnUtility.GetSpawnFromList(entity, Frequency.Rare);
            }

            if (string.IsNullOrEmpty(spawnName))
            {
                spawnName = UORespawnUtility.GetSpawnFromList(entity, Frequency.UnCommon);
            }

            if (string.IsNullOrEmpty(spawnName))
            {
                spawnName = UORespawnUtility.GetSpawnFromList(entity, Frequency.Common);
            }

            return spawnName;
        }

        internal static void SaveStats()
        {
            if (!Directory.Exists(STAT_DIR))
            {
                Directory.CreateDirectory(STAT_DIR);
            }

            if (SpawnStats.Count > 0)
            {
                foreach (var spawn in SpawnStats)
                {
                    string converted = $"{spawn.Item1:t}|{spawn.Item2}|{spawn.Item3}|{spawn.Item4.X}|{spawn.Item4.Y}|{spawn.Item5.X}|{spawn.Item5.Y}|{spawn.Item6}";

                    File.AppendAllText(Path.Combine(STAT_DIR, $"{DateTime.Now.Year}_{DateTime.Now.DayOfYear}.txt"), converted + Environment.NewLine);
                }

                SpawnStats.Clear();
            }

            UORespawnUtility.CleanUpOldFiles(STAT_DIR);
        }
    }
}
