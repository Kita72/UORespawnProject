using System;
using Server.Mobiles;

namespace Server.Custom.UORespawnSystem.SpawnHelpers
{
    internal class SpawnTimer : Timer
    {
        private int _Index = 0;

        public SpawnTimer() : base(TimeSpan.FromMilliseconds(UORespawnSettings.INTERVAL), TimeSpan.FromMilliseconds(UORespawnSettings.INTERVAL))
        {
            Priority = TimerPriority.TwentyFiveMS;
        }

        protected override void OnTick()
        {
            var players = UORespawnCore._Players;

            if (players.Count == 0) return;

            for (int i = 0; i < UORespawnSettings.BATCH_SIZE; i++)
            {
                if (_Index >= players.Count) _Index = 0;

                if (players.Count == 0) break;

                PlayerMobile pm = players[_Index];

                if (pm != null && pm.Map != Map.Internal)
                {
                    UORespawnCore.UpdatePlayerSpawn(pm);

                    UORespawnCore.ProcessQueue(pm);
                }

                _Index++;
            }
        }
    }
}
