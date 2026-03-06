using System;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Entities;

namespace Server.Custom.UORespawnServer.Timers
{
    /// <summary>
    /// Per-player timer that searches for valid spawn locations ahead of the player.
    /// Queues SpawnEntity objects for later processing.
    /// </summary>
    internal class SearchTimer : Timer
    {
        private readonly PlayerMobile _Player;
        private readonly RespawnerEntity _Entity;
        private int _LocationsPushed;

        public SearchTimer(PlayerMobile player, RespawnerEntity entity, TimeSpan ts) : base(ts, ts)
        {
            _Player = player;
            _Entity = entity;

            UOR_Utility.SendMsg(ConsoleColor.Green, $"SEARCH-[{ts.TotalSeconds} Started]");
        }

        protected override void OnTick()
        {
            if (UOR_Core.IsPaused) return;

            if (_Entity == null)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"SEARCH-[Null]");

                Stop();

                return;
            }
            else
            {
                if (_Entity.GetQueCount() < UOR_Settings.MAX_QUEUE_SIZE)
                {
                    _Entity.Push(UOR_Utility.Locate(_Entity, new LocationEntity(_Player)));

                    _LocationsPushed++;

                    if (_LocationsPushed % 50 == 0)
                    {
                        UOR_Utility.SendMsg(ConsoleColor.Yellow, $"SEARCH-[{_Player.Name}: {_LocationsPushed} locations pushed, queue: {_Entity.GetQueCount()}]");
                    }
                }
            }
        }
    }
}
