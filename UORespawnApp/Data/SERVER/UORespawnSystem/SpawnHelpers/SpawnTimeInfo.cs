using System;
using System.Collections.Generic;
using System.Linq;
using Server.Custom.UORespawnSystem.Enums;
using Server.Items;

namespace Server.Custom.UORespawnSystem.SpawnHelpers
{
    internal static class SpawnTimeInfo
    {
        private static readonly List<int> nightLabels = new List<int>() { 1042957, 1042950, 1042951, 1042952 };

        private static Timer _CheckClockTimer;

        public static void Initialize()
        {
            _CheckClockTimer = Timer.DelayCall(
                TimeSpan.FromSeconds(UORespawnSettings.CHECK_TIME_INTERVAL),
                TimeSpan.FromSeconds(UORespawnSettings.CHECK_TIME_INTERVAL),
                OnTimerTick);
        }

        private static void OnTimerTick()
        {
            if (_CheckClockTimer != null)
            {
                // Toggle Day/Night
                SpawnVendors.ToggleVendorWorking();
            }
        }

        internal static bool IsNight(Mobile from)
        {
            Clock.GetTime(from, out int label, out string time);

            if (nightLabels.Contains(label))
            {
                if (int.TryParse(time.Split(':').First(), out var hour) && (hour > 8 || hour < 6))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsSpawnTime(TimeNames timedSpawn, int hours)
        {
            // 00:00 AM - 00:59 AM : Witching hour
            // 01:00 AM - 03:59 AM : Middle of night
            // 04:00 AM - 07:59 AM : Early morning
            // 08:00 AM - 11:59 AM : Late morning
            // 12:00 PM - 12:59 PM : Noon
            // 01:00 PM - 03:59 PM : Afternoon
            // 04:00 PM - 07:59 PM : Early evening
            // 08:00 PM - 11:59 AM : Late at night

            if (hours >= 20)
            {
                return timedSpawn == TimeNames.Late_at_Night;
            }
            else if (hours >= 16)
            {
                return timedSpawn == TimeNames.Early_Evening;
            }
            else if (hours >= 13)
            {
                return timedSpawn == TimeNames.Afternoon;
            }
            else if (hours >= 12)
            {
                return timedSpawn == TimeNames.None;
            }
            else if (hours >= 08)
            {
                return timedSpawn == TimeNames.Late_Morning;
            }
            else if (hours >= 04)
            {
                return timedSpawn == TimeNames.Early_Morning;
            }
            else if (hours >= 01)
            {
                return timedSpawn == TimeNames.Middle_of_Night;
            }
            else
            {
                return timedSpawn == TimeNames.Witching_Hour;
            }
        }
    }
}
