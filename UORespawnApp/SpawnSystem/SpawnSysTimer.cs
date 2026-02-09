using System;
using Server.Mobiles;

namespace Server.Custom.SpawnSystem
{
    internal class SpawnSysTimer : Timer
    {
        private int _Index = 0;

        public SpawnSysTimer() : base(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50))
        {
            Priority = TimerPriority.TwentyFiveMS;
        }

        protected override void OnTick()
        {
            var players = SpawnSysCore._Players;

            if (players.Count == 0) return;

            int batchSize = 5;

            for (int i = 0; i < batchSize; i++)
            {
                if (_Index >= players.Count) _Index = 0;
                if (players.Count == 0) break;

                PlayerMobile currentPlayer = players[_Index];

                if (currentPlayer != null && currentPlayer.Map != Map.Internal)
                {
                    SpawnSysCore.UpdatePlayerSpawn(currentPlayer, currentPlayer.Map, currentPlayer.Location, false);
                }

                _Index++;
            }

            if (_Index == 0)
            {
                SpawnSysCore.RunSpawnCleanUp();
            }
        }
    }
}
