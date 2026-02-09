using Server.Misc;
using Server.Mobiles;
using Server.Custom.SpawnSystem.Mobiles;

using static Server.Custom.SpawnSystem.SpawnSysSettings;

namespace Server.Custom.SpawnSystem
{
    internal static class WeatherSpawn
    {
        internal static string TryWeatherSpawn(Map map, Point3D location)
        {
            if (Weather.GetWeatherList(map).Count > 0)
            {
                try
                {
                    foreach (var front in Weather.GetWeatherList(map))
                    {
                        if (front.IntersectsWith(new Rectangle2D(location.X, location.Y, 5, 5)))
                        {
                            if (front.ChanceOfPercipitation == 100 && front.ChanceOfExtremeTemperature >= 5)
                            {
                                if ((map == Map.Trammel || map == Map.Felucca) && ENABLE_RIFTSPAWN && Utility.RandomDouble() < 0.1)
                                {
                                    return nameof(RiftMob);
                                }

                                if (front.Temperature > 10)
                                {
                                    if (Utility.RandomDouble() < (CHANCE_WEATHER * SpawnSysFactory.NightMod))
                                    {
                                        return TileSpawn.TryTileSpawn(map, "rain_event", Frequency.Rare);
                                    }
                                }

                                if (front.Temperature < -10)
                                {
                                    if (Utility.RandomDouble() < (CHANCE_WEATHER * SpawnSysFactory.NightMod))
                                    {
                                        return TileSpawn.TryTileSpawn(map, "snow_event", Frequency.Rare);
                                    }
                                }
                            }
                            else if (front.ChanceOfPercipitation == 100)
                            {
                                if (front.Temperature > 10)
                                {
                                    if (Utility.RandomDouble() < (CHANCE_WEATHER * SpawnSysFactory.NightMod))
                                    {
                                        if (Utility.RandomDouble() < 0.1)
                                        {
                                            return nameof(Jwilson);
                                        }

                                        return TileSpawn.TryTileSpawn(map, "rain_event", Utility.RandomList(Frequency.Common, Frequency.UnCommon));
                                    }
                                }

                                if (front.Temperature < -10)
                                {
                                    if (Utility.RandomDouble() < (CHANCE_WEATHER * SpawnSysFactory.NightMod))
                                    {
                                        return TileSpawn.TryTileSpawn(map, "snow_event", Utility.RandomList(Frequency.Common, Frequency.UnCommon));
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    SpawnSysUtility.SendConsoleMsg(System.ConsoleColor.DarkRed, "Factory => Weather Error!");
                }
            }

            return string.Empty;
        }
    }
}
