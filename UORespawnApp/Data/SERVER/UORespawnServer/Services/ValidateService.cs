using System;

using Server.Custom.UORespawnServer.Timers;
using Server.Custom.UORespawnServer.Spawners;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Validates spawn proximity to players.
    /// Spawn too far from players gets sent to recycle.
    /// Uses ISpawner on-demand query - no tracking list needed.
    /// </summary>
    internal class ValidateService
    {
        private readonly ValidateTimer _ValidateTimer;

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
        /// Check all mob spawn - send those far from players to recycle.
        /// </summary>
        internal void Validate()
        {
            // On-demand ISpawner query - no tracking list
            var allSpawn = UOR_MobSpawner.GetAllSpawn();

            int recycledThisPass = 0;
            int maxDistance = UOR_Settings.MAX_RANGE + (UOR_Settings.MAX_RANGE / 2);

            foreach (var creature in allSpawn)
            {
                if (creature == null || creature.Deleted)
                    continue;

                if (!UOR_Utility.PlayersInRange(creature.Map, creature.Location, maxDistance))
                {
                    UOR_Core.SendToRecycled(creature.Serial);
                    recycledThisPass++;
                }
            }

            if (recycledThisPass > 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[{recycledThisPass} Recycled]");
            }
        }
    }
}
