using System;
using System.Linq;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Custom.UORespawnSystem.SpawnUtility;

namespace Server.Custom.UORespawnSystem.Services
{
    /// <summary>
    /// Manages the recycle pool for spawned mobs.
    /// Stores up to MAX_RECYCLE_PER_TYPE (20) mobs per type.
    /// Recycled mobs are moved to Map.Internal to hide them until respawn.
    /// </summary>
    internal static class SpawnRecycleService
    {
        private static Dictionary<string, Queue<Mobile>> _recyclePool;

        /// <summary>
        /// Initialize the recycle pool
        /// </summary>
        internal static void Initialize()
        {
            _recyclePool = new Dictionary<string, Queue<Mobile>>();

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Recycle Manager - Initialized");
        }

        /// <summary>
        /// Try to get a recycled mob of the specified type.
        /// Returns null if no recycled mobs of this type exist.
        /// </summary>
        internal static Mobile TryGetRecycled(string mobType)
        {
            if (string.IsNullOrEmpty(mobType))
                return null;

            if (_recyclePool.TryGetValue(mobType, out Queue<Mobile> queue) && queue.Count > 0)
            {
                Mobile mob = queue.Dequeue();

                // Validate mob is still valid
                if (mob == null || mob.Deleted)
                {
                    // Try next in queue
                    if (queue.Count > 0)
                        return TryGetRecycled(mobType);
                    
                    return null;
                }

                UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkYellow, $"RECYCLE POOL: Retrieved {mob.Name} ({mobType}), Remaining: {queue.Count}/{UORespawnSettings.MAX_RECYCLE_TYPE}");

                SpawnMetricsService.RecordRecyclePoolRetrieval();

                return mob;
            }

            return null;
        }

        /// <summary>
        /// Try to add a mob to the recycle pool.
        /// Returns true if accepted, false if pool is full for this type.
        /// </summary>
        internal static bool TryAddToRecycle(Mobile mob)
        {
            if (mob == null || mob.Deleted)
                return false;

            string mobType = mob.GetType().Name;

            // Ensure queue exists for this type
            if (!_recyclePool.ContainsKey(mobType))
            {
                _recyclePool[mobType] = new Queue<Mobile>();
            }

            Queue<Mobile> queue = _recyclePool[mobType];

            // Check if type limit reached
            if (queue.Count >= UORespawnSettings.MAX_RECYCLE_TYPE)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"WARNING: {mobType} recycle pool full ({queue.Count}/{UORespawnSettings.MAX_RECYCLE_TYPE}), cannot add {mob.Name}");
                return false;
            }

            // Check if total limit reached
            if (GetTotalRecycled() >= UORespawnSettings.MAX_RECYCLE_TOTAL)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"WARNING: Total recycle pool full ({GetTotalRecycled()}/{UORespawnSettings.MAX_RECYCLE_TOTAL}), cannot add {mob.Name}");
                return false;
            }

            // Reset mob state for recycling
            if (mob is BaseCreature bc)
            {
                bc.Combatant = null;
                bc.Warmode = false;
                bc.Blessed = true; // Protect while in storage
            }

            // Move to Map.Internal (hidden storage)
            mob.MoveToWorld(Point3D.Zero, Map.Internal);

            queue.Enqueue(mob);

            UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkYellow, $"RECYCLE POOL: Added {mob.Name} ({mobType}), Pool: {queue.Count}/{UORespawnSettings.MAX_RECYCLE_TYPE}");

            return true;
        }

        /// <summary>
        /// Get the count of recycled mobs for a specific type
        /// </summary>
        internal static int GetRecycledCount(string mobType)
        {
            if (string.IsNullOrEmpty(mobType))
                return 0;

            if (_recyclePool.TryGetValue(mobType, out Queue<Mobile> queue))
            {
                return queue.Count;
            }

            return 0;
        }

        /// <summary>
        /// Get total count of all recycled mobs across all types
        /// </summary>
        internal static int GetTotalRecycled()
        {
            int total = 0;

            foreach (Queue<Mobile> queue in _recyclePool.Values)
            {
                total += queue.Count;
            }

            return total;
        }

        /// <summary>
        /// Get recycle stats for all mob types (for commands/gumps)
        /// Returns dictionary of mobType -> count
        /// </summary>
        internal static Dictionary<string, int> GetRecycleStats()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();

            foreach (var kvp in _recyclePool)
            {
                if (kvp.Value.Count > 0)
                {
                    stats[kvp.Key] = kvp.Value.Count;
                }
            }

            return stats;
        }

        /// <summary>
        /// Clear all recycled mobs (for shutdown/reset)
        /// Deletes all mobs in the recycle pool
        /// </summary>
        internal static void ClearAll()
        {
            int deletedCount = 0;

            foreach (Queue<Mobile> queue in _recyclePool.Values)
            {
                while (queue.Count > 0)
                {
                    Mobile mob = queue.Dequeue();

                    if (mob != null && !mob.Deleted)
                    {
                        mob.Delete();
                        deletedCount++;
                    }
                }
            }

            _recyclePool.Clear();

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"RECYCLE POOL: Cleared all pools - Deleted: {deletedCount} mobs");
        }

        /// <summary>
        /// Get formatted stats string for display
        /// </summary>
        internal static string GetStatsString()
        {
            if (_recyclePool.Count == 0)
            {
                return "Recycle pool is empty.";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine($"Recycle Pool Stats (Total: {GetTotalRecycled()}/{UORespawnSettings.MAX_RECYCLE_TOTAL}):");
            sb.AppendLine("─────────────────────────────────────────");

            var sortedStats = GetRecycleStats().OrderByDescending(kvp => kvp.Value);

            foreach (var kvp in sortedStats)
            {
                string mobType = kvp.Key;
                int count = kvp.Value;
                double percentage = (count / (double)UORespawnSettings.MAX_RECYCLE_TYPE) * 100.0;

                sb.AppendLine($"  {mobType,-25} {count,2}/{UORespawnSettings.MAX_RECYCLE_TYPE} ({percentage,5:F1}%)");
            }

            return sb.ToString();
        }
    }
}
