using System;
using Server.Mobiles;
using System.Collections.Generic;
using Server.Custom.SpawnSystem.Mobiles;

namespace Server.Custom.SpawnSystem
{
    internal static class SpawnSysFactory
    {
        internal static List<(DateTime, PlayerMobile, Map, Point2D, Point2D)> SpawnStats { get; private set; } = new List<(DateTime, PlayerMobile, Map, Point2D, Point2D)>();

        internal static int NightMod = 1;

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
            if (pm.IsStaff() && SpawnSysDataBase.EnableDebugSpawn)
            {
                pm.SendMessage(53, $"Spawned PH: [{location}]");

                return nameof(PlaceHolder);
            }

            return string.Empty;
        }
    }
}
