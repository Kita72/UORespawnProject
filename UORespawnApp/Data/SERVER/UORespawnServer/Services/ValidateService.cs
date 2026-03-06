using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Custom.UORespawnServer.Commands;
using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Timers;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Validates spawn counts and trims excess.
    /// Spawn is only trimmed if: out-of-sight AND type count exceeds limit.
    /// Deletes furthest away first to minimize player impact.
    /// </summary>
    internal class ValidateService
    {
        private readonly ValidateTimer _ValidateTimer;

        internal bool IsCalling { get; set; }

        public ValidateService()
        {
            _ValidateTimer = new ValidateTimer(this, TimeSpan.FromSeconds(UOR_Settings.VALIDATE_INTERVAL));

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[{UOR_Settings.VALIDATE_INTERVAL}s Interval]");
        }

        internal void Start()
        {
            if (!_ValidateTimer.Running)
            {
                _ValidateTimer?.Start();

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[Started]");
            }
        }

        internal void Stop()
        {
            if (_ValidateTimer.Running)
            {
                _ValidateTimer?.Stop();

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[Stopped]");
            }
        }

        /// <summary>
        /// Validate spawn counts and trim excess per type.
        /// Only trims spawn that is out-of-sight of all players.
        /// Uses MAX_RECYCLE_TYPE as the per-type limit.
        /// </summary>
        internal void Validate()
        {
            if (IsCalling && UOR_Core.GetRespawners(out List<RespawnerEntity> list))
            {
                foreach (var spawner in list)
                {
                    UOR_Commands.RunCallOut(spawner._Player);
                }
            }

            var queryService = UOR_Core.GetSpawnQueryService();

            if (queryService == null)
                return;

            // Check total spawn limit first
            int totalSpawn = queryService.GetTotalSpawnCount();
            int totalLimit = UOR_Settings.MAX_RECYCLE_TOTAL;

            if (totalSpawn > totalLimit)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[Total: {totalSpawn}/{totalLimit} - Trimming excess]");
            }

            // Get all unique spawn types currently in world
            var spawnTypes = queryService.GetAllSpawnTypes();
            int totalTrimmed = 0;
            int typeLimit = UOR_Settings.MAX_RECYCLE_TYPE;

            foreach (var typeName in spawnTypes)
            {
                int typeCount = queryService.GetTypeCount(typeName);

                // Only trim if over limit
                if (typeCount > typeLimit)
                {
                    int trimmed = queryService.TrimExcess(typeName, typeLimit);

                    if (trimmed > 0)
                    {
                        totalTrimmed += trimmed;

                        if (UOR_Settings.ENABLE_DEBUG)
                        {
                            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[{typeName}: {typeCount}->{typeCount - trimmed}]");
                        }
                    }
                }
            }

            if (totalTrimmed > 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[Trimmed {totalTrimmed} excess spawn]");
            }
        }
    }
}
