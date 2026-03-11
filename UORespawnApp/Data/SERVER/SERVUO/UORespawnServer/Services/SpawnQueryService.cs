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
        /// Single-pass validate and trim.
        /// Groups all spawn by type in one scan, then trims out-of-sight excess
        /// for any type that exceeds <paramref name="typeLimit"/>.
        /// Returns total number of spawns deleted.
        /// </summary>
        internal int ValidateAndTrim(int typeLimit)
        {
            var allSpawn = UOR_MobSpawner.GetAllSpawn();

            if (allSpawn == null || allSpawn.Count == 0)
                return 0;

            // Group spawn by type in a single pass
            var byType = new Dictionary<string, List<BaseCreature>>(StringComparer.OrdinalIgnoreCase);

            foreach (var bc in allSpawn)
            {
                if (bc == null || bc.Deleted || !bc.Alive)
                    continue;

                string typeName = bc.GetType().Name;

                if (!byType.TryGetValue(typeName, out var list))
                {
                    list = new List<BaseCreature>();
                    byType[typeName] = list;
                }

                list.Add(bc);
            }

            int totalTrimmed = 0;

            foreach (var kvp in byType)
            {
                int excess = kvp.Value.Count - typeLimit;

                if (excess <= 0)
                    continue;

                // Collect out-of-sight candidates with pre-projected distance
                var candidates = new List<(BaseCreature Creature, int DistSq)>();

                foreach (var bc in kvp.Value)
                {
                    if (IsOutOfSight(bc))
                        candidates.Add((bc, GetMinPlayerDistanceSquared(bc)));
                }

                // Sort furthest-from-players first
                candidates.Sort((a, b) => b.DistSq.CompareTo(a.DistSq));

                int toTrim = Math.Min(excess, candidates.Count);

                for (int i = 0; i < toTrim; i++)
                {
                    if (!candidates[i].Creature.Deleted)
                    {
                        candidates[i].Creature.Delete();
                        totalTrimmed++;
                        _TotalTrimmed++;
                    }
                }

                if (UOR_Settings.ENABLE_DEBUG && toTrim > 0)
                {
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[{kvp.Key}: {kvp.Value.Count}->{kvp.Value.Count - toTrim}]");
                }
            }

            return totalTrimmed;
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

            }
        }
