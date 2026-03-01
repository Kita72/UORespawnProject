using System;
using System.Collections.Generic;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Services;

namespace Server.Custom.UORespawnServer.Timers
{
    internal class ProcessTimer : Timer
    {
        private readonly ProcessService _Service;
        private int _SpawnAttempts;

        public ProcessTimer(ProcessService service, TimeSpan ts) : base(ts, ts)
        {
            _Service = service;

            UOR_Utility.SendMsg(ConsoleColor.Green, $"PROCESS-[{ts.TotalSeconds} Started]");
        }

        protected override void OnTick()
        {
            if (UOR_Core.IsPaused) return;

            if (_Service == null)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"PROCESS-[Null]");

                Stop();
                return;
            }

            if (UOR_Core.GetRespawners(out List<RespawnerEntity> list))
            {
                int spawnedThisTick = 0;

                for (int i = 0; i < list.Count; i++)
                {
                    RespawnerEntity entity = list[i];

                    var spawn = UOR_Utility.SpawnInRange(entity._Player.Map, entity._Player.Location, UOR_Settings.MAX_RANGE);

                    if (spawn < UOR_Settings.MAX_SPAWN)
                    {
                        _Service.Spawn(entity._Player, list[i].Pop());

                        spawnedThisTick++;

                        _SpawnAttempts++;
                    }
                }

                if (spawnedThisTick > 0 && _SpawnAttempts % 25 == 0)
                {
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"PROCESS-[Spawned {spawnedThisTick} for {list.Count} players (total: {_SpawnAttempts})]");
                }
            }
        }
    }
}
