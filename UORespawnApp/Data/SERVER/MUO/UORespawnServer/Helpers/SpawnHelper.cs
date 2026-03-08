using Server.Mobiles;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Mobiles;

namespace Server.Custom.UORespawnServer.Helpers;
internal static class SpawnHelper
{
    internal static string GetUndeadSpawn()
    {
        return Utility.RandomList(
            nameof(Zombie),
            nameof(HeadlessOne),
            nameof(Shade),
            nameof(Skeleton));
    }

    internal static string GetWaterSpawn(WaterTypes waterType)
    {
        var spawn = nameof(SeaSerpent);

        switch (waterType)
        {
            case WaterTypes.Shallow:
                {
                    spawn = Utility.RandomList(
                        nameof(Dolphin),
                        nameof(BullFrog),
                        nameof(SeaSerpent),
                        nameof(WaterElemental));

                    break;
                }
            case WaterTypes.Deep:
                {
                    spawn = Utility.RandomList(
                        nameof(WaterElemental),
                        nameof(SeaSerpent),
                        nameof(DeepSeaSerpent),
                        nameof(Kraken));

                    break;
                }
        }

        return spawn;
    }

    internal static string GetWeatherSpawn(WeatherTypes weatherType)
    {
        return weatherType switch
        {
            WeatherTypes.Storm => nameof(WaterElemental),
            WeatherTypes.Blizzard => nameof(RiftMob),
            _ => string.Empty,
        };
    }
}
