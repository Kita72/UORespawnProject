using System;
using System.Collections.Generic;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Timers;

namespace Server.Custom.UORespawnServer.Entities
{
    /// <summary>
    /// Per-player spawner entity. Handles searching for valid spawn points ahead of the player
    /// and queuing SpawnEntity objects for the ProcessService to spawn.
    /// </summary>
    internal class RespawnerEntity
    {
        internal readonly PlayerMobile _Player;

        private readonly Queue<SpawnEntity> _SpawnQueue;

        private readonly SearchTimer _SearchTimer;

        private int _WaterCooldown = 0;

        internal RespawnerEntity(PlayerMobile pm)
        {
            _Player = pm;

            _SpawnQueue = new Queue<SpawnEntity>();

            _SearchTimer = new SearchTimer(pm, this, TimeSpan.FromMilliseconds(UOR_Settings.SEARCH_INTERVAL));

            _SearchTimer.Start();
        }

        internal int GetQueCount()
        {
            return _SpawnQueue.Count;
        }

        internal void Push(SpawnEntity entity)
        {
            if (entity != null)
            {
                if (entity.IsWater && _WaterCooldown > 0)
                {
                    _WaterCooldown--;

                    return;
                }

                _SpawnQueue?.Enqueue(entity);

                if (entity.IsWater && _WaterCooldown == 0)
                {
                    _WaterCooldown = UOR_Settings.MAX_QUEUE_SIZE;
                }
            }
        }

        internal SpawnEntity Pop()
        {
            if (_SearchTimer.Running)
            {
                if (_SpawnQueue.Count > 0)
                {
                    return _SpawnQueue.Dequeue();
                }
            }

            return null;
        }

        internal void Stop()
        {
            if (_SearchTimer.Running)
            {
                _SearchTimer?.Stop();
            }

            _SpawnQueue?.Clear();
        }
    }
}
