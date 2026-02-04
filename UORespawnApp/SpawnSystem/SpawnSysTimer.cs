using System;
using Server.Mobiles;

namespace Server.Custom.SpawnSystem
{
    internal class SpawnSysTimer : Timer
    {
        int counter = 0;

        public SpawnSysTimer() : base(TimeSpan.FromMilliseconds(SpawnSysSettings.INTERVAL), TimeSpan.FromMilliseconds(SpawnSysSettings.INTERVAL))
        {
            Priority = TimerPriority.TwentyFiveMS;
        }

        protected override void OnTick()
        {
            if (SpawnSysCore._Players.Count > 0)
            {
                if (SpawnSysCore.HasChanged)
                {
                    SpawnSysCore.HasChanged = false;

                    Interval = TimeSpan.FromMilliseconds(SpawnSysSettings.INTERVAL * SpawnSysCore._Players.Count);
                }

                if (SpawnSysCore._Players.Count > counter)
                {
                    PlayerMobile currentPlayer = SpawnSysCore._Players[counter];

                    if (currentPlayer != null)
                    {
                        SpawnSysCore.UpdatePlayerSpawn(currentPlayer, currentPlayer.Map, currentPlayer.Location, true);
                    }
                }

                counter++;

                if (counter >= SpawnSysCore._Players.Count)
                {
                    counter = 0;
                }
            }
            else
            {
                counter = 0;

                Interval = TimeSpan.FromMilliseconds(SpawnSysSettings.INTERVAL);
            }
        }
    }
}
