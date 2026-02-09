using System;
using System.IO;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Custom.SpawnSystem.Mobiles;

using static Server.Custom.SpawnSystem.SpawnSysSettings;

namespace Server.Custom.SpawnSystem
{
    internal static class SpawnSysFactory
    {
        private static readonly string STAT_DIR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_Stats");

        private static List<(DateTime, PlayerMobile, Map, Point2D, Point2D)> SpawnStats { get; set; } = new List<(DateTime, PlayerMobile, Map, Point2D, Point2D)>();

        internal static int NightMod = 1;

        internal static int DebugMessageTrottle = 0;

        private const int MAX_DEBUG_MESSAGES = 25;

        internal static string GetSpawnName(PlayerMobile pm, Map map, Region region, Point3D location, bool isWater)
        {
            SpawnStats.Add((DateTime.Now, pm, map, new Point2D(pm.Location.X, pm.Location.Y), new Point2D(location.X, location.Y)));

            NightMod = SpawnSysUtility.IsNight(pm) ? 2 : 1;

            // Water
            string spawn = WaterSpawn.TryWaterSpawn(map, region, location, isWater);

            if (!string.IsNullOrEmpty(spawn))
            {
                return spawn;
            }

            // Weather
            spawn = WeatherSpawn.TryWeatherSpawn(map, location);

            if (!string.IsNullOrEmpty(spawn))
            {
                return spawn;
            }

            // Static
            spawn = StaticSpawn.TryStaticSpawn(map, location);

            if (!string.IsNullOrEmpty(spawn))
            {
                return spawn;
            }

            // Box
            spawn = BoxSpawn.TryBoxSpawn(map, location);

            if (!string.IsNullOrEmpty(spawn))
            {
                return spawn;
            }

            // World
            spawn = TileSpawn.TryTileSpawn(map, location);

            if (!string.IsNullOrEmpty(spawn))
            {
                return spawn;
            }

            // Staff - Debug
            if (pm.IsStaff() && ENABLE_DEBUG)
            {
                if ( DebugMessageTrottle < MAX_DEBUG_MESSAGES)
                {
                    DebugMessageTrottle++;
                }
                else
                {
                    DebugMessageTrottle = 0;

                    pm.SendMessage(53, $"{MAX_DEBUG_MESSAGES} Placeholders Queried");
                }

                return nameof(PlaceHolder);
            }

            return string.Empty;
        }

        internal static void SaveStats()
        {

            if (!Directory.Exists(STAT_DIR))
            {
                Directory.CreateDirectory(STAT_DIR);
            }

            foreach (var spawn in SpawnStats)
            {
                string converted = $"{spawn.Item1:t}|{spawn.Item2.Name}|{spawn.Item3}|{spawn.Item4.X}|{spawn.Item4.Y}|{spawn.Item5.X}|{spawn.Item5.Y}";

                File.AppendAllText(Path.Combine(STAT_DIR, $"{DateTime.Now.Year}_{DateTime.Now.DayOfYear}.txt"), converted + Environment.NewLine);
            }

            SpawnStats.Clear();

            SpawnSysUtility.CleanUpOldFiles(STAT_DIR);
        }
    }
}
