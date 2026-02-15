using System;

using Server.Misc;
using Server.Custom.UORespawnSystem.SpawnUtility;
using Server.Custom.UORespawnSystem.Enums;

namespace Server.Custom.UORespawnSystem.SpawnHelpers
{
    internal static class SpawnWeatherInfo
    {
        internal static WeatherTypes GetWeatherInfo(Map map, Point3D location)
        {
            if (Weather.GetWeatherList(map).Count > 0)
            {
                var position = new Rectangle2D(location.X - 1, location.Y - 1, 3, 3);

                try
                {
                    foreach (var front in Weather.GetWeatherList(map))
                    {
                        if (front.ChanceOfPercipitation == 100 && front.IntersectsWith(position))
                        {
                            if (front.ChanceOfExtremeTemperature >= 5)
                            {
                                if (front.Temperature > 10)
                                {
                                    return WeatherTypes.Storm;
                                }

                                if (front.Temperature < -10)
                                {
                                    return WeatherTypes.Blizzard;
                                }
                            }
                            else
                            {
                                if (front.Temperature > 10)
                                {
                                    return WeatherTypes.Rain;
                                }

                                if (front.Temperature < -10)
                                {
                                    return WeatherTypes.Snow;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkRed, "Weather Info => Error!");
                }
            }

            return WeatherTypes.None;
        }
    }
}
