using System;
using System.Linq;
using System.Collections.Generic;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Spawners;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Handles on-map spawn queries for relocation and trimming.
    /// Replaces RecycleService with live map queries - no internal storage needed.
    /// Spawn stays "warm" on map until relocated or trimmed.
    /// </summary>
    internal class SpawnQueryService
    {
        // Track total relocations for stats display
        private int _TotalRelocated;
        private int _TotalTrimmed;

        internal SpawnQueryService()
        {
            _TotalRelocated = 0;
            _TotalTrimmed = 0;
        }

        /// <summary>
        /// Gets total relocations performed this session.
        /// </summary>
        internal int GetTotalRelocated() => _TotalRelocated;

        /// <summary>
        /// Gets total spawns trimmed this session.
        /// </summary>
        internal int GetTotalTrimmed() => _TotalTrimmed;

        /// <summary>
        /// Finds a relocatable spawn of the given type that is out-of-sight of all players.
        /// Prefers the furthest spawn from any player for seamless relocation.
        /// </summary>
        /// <param name="typeName">The type name to search for (e.g., "Orc")</param>
        /// <returns>Mobile ready to relocate, or null if none available</returns>
        internal Mobile FindRelocatable(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var allSpawn = UOR_MobSpawner.GetAllSpawn();

            if (allSpawn == null || allSpawn.Count == 0)
                return null;

            // Filter to matching type, out-of-sight, valid for relocation
            var candidates = allSpawn
                .Where(bc => bc != null && 
                             !bc.Deleted && 
                             bc.Alive &&
                             bc.GetType().Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                             IsOutOfSight(bc))
                .ToList();

            if (candidates.Count == 0)
                return null;

            // Find furthest from any player (best candidate for relocation)
            BaseCreature furthest = null;
            int maxDistSq = 0;

            foreach (var candidate in candidates)
            {
                int distSq = GetMinPlayerDistanceSquared(candidate);

                if (distSq > maxDistSq)
                {
                    maxDistSq = distSq;
                    furthest = candidate;
                }
            }

            if (furthest != null)
            {
                _TotalRelocated++;
            }

            return furthest;
        }

        /// <summary>
        /// Gets the count of a specific spawn type currently on all maps.
        /// </summary>
        internal int GetTypeCount(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return 0;

            var allSpawn = UOR_MobSpawner.GetAllSpawn();

            return allSpawn.Count(bc => bc != null && 
                                        !bc.Deleted && 
                                        bc.GetType().Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets spawns of a type that exceed the limit, sorted by furthest from players first.
        /// Only returns spawns that are out-of-sight for safe trimming.
        /// </summary>
        /// <param name="typeName">The type name to check</param>
        /// <param name="limit">Max allowed count (from MAX_RECYCLE_TYPE)</param>
        /// <returns>List of excess spawns to trim, furthest first</returns>
        internal List<BaseCreature> GetExcessSpawn(string typeName, int limit)
        {
            var result = new List<BaseCreature>();

            if (string.IsNullOrEmpty(typeName))
                return result;

            var allSpawn = UOR_MobSpawner.GetAllSpawn();

            // Get all of this type
            var typeSpawn = allSpawn
                .Where(bc => bc != null && 
                             !bc.Deleted && 
                             bc.Alive &&
                             bc.GetType().Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            int excess = typeSpawn.Count - limit;

            if (excess <= 0)
                return result;

            // Get out-of-sight candidates, sorted by furthest from players
            var candidates = typeSpawn
                .Where(bc => IsOutOfSight(bc))
                .Select(bc => new { Creature = bc, DistSq = GetMinPlayerDistanceSquared(bc) })
                .OrderByDescending(x => x.DistSq)
                .Take(excess)
                .Select(x => x.Creature)
                .ToList();

            return candidates;
        }

        /// <summary>
        /// Trims excess spawn of a specific type. Deletes furthest out-of-sight first.
        /// </summary>
        /// <returns>Number of spawns trimmed</returns>
        internal int TrimExcess(string typeName, int limit)
        {
            var excess = GetExcessSpawn(typeName, limit);
            int trimmed = 0;

            foreach (var spawn in excess)
            {
                if (spawn != null && !spawn.Deleted)
                {
                    spawn.Delete();
                    trimmed++;
                    _TotalTrimmed++;
                }
            }

            return trimmed;
        }

        /// <summary>
        /// Gets total spawn count across all maps (for MAX_RECYCLE_TOTAL check).
        /// </summary>
        internal int GetTotalSpawnCount()
        {
            return UOR_MobSpawner.GetCount();
        }

        /// <summary>
        /// Checks if spawn count exceeds the total limit.
        /// </summary>
        internal bool IsAtTotalLimit()
        {
            return GetTotalSpawnCount() >= UOR_Settings.MAX_RECYCLE_TOTAL;
        }

        /// <summary>
        /// Checks if a creature is out of sight of all players.
        /// Uses MAX_RANGE as the visibility threshold.
        /// </summary>
        internal bool IsOutOfSight(BaseCreature creature)
        {
            if (creature == null || creature.Deleted || creature.Map == null || creature.Map == Map.Internal)
                return false;

            // No players within MAX_RANGE means out of sight
            return !UOR_Utility.PlayersInRange(creature.Map, creature.Location, UOR_Settings.MAX_RANGE);
        }

        /// <summary>
        /// Gets the squared distance to the nearest player.
        /// Returns int.MaxValue if no players on same map.
        /// </summary>
        private int GetMinPlayerDistanceSquared(BaseCreature creature)
        {
            if (creature == null || creature.Map == null || creature.Map == Map.Internal)
                return int.MaxValue;

            int minDistSq = int.MaxValue;
            var players = creature.Map.GetClientsInRange(creature.Location, UOR_Settings.MAX_RANGE * 4);

            foreach (var ns in players)
            {
                if (ns?.Mobile != null)
                {
                    int dx = creature.X - ns.Mobile.X;
                    int dy = creature.Y - ns.Mobile.Y;
                    int distSq = (dx * dx) + (dy * dy);

                    if (distSq < minDistSq)
                        minDistSq = distSq;
                }
            }

            players.Free();

            return minDistSq;
        }

        /// <summary>
        /// Gets all unique type names currently spawned.
        /// Used by ValidateService for trimming passes.
        /// </summary>
        internal HashSet<string> GetAllSpawnTypes()
        {
            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allSpawn = UOR_MobSpawner.GetAllSpawn();

            foreach (var spawn in allSpawn)
            {
                if (spawn != null && !spawn.Deleted)
                {
                    types.Add(spawn.GetType().Name);
                }
            }

            return types;
        }
    }
}
