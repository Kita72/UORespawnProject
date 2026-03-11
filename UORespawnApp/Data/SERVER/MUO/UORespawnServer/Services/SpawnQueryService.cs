using System;
using System.Linq;
using System.Collections.Generic;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Spawners;

namespace Server.Custom.UORespawnServer.Services;
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
    /// Single-pass validation and trimming: groups all spawn by type in one GetAllSpawn() call,
    /// then trims excess per-type (furthest out-of-sight first).
    /// </summary>
    /// <param name="typeLimit">Max allowed count per type (MAX_RECYCLE_TYPE)</param>
    /// <returns>Total number of spawns trimmed</returns>
    internal int ValidateAndTrim(int typeLimit)
    {
        var allSpawn = UOR_MobSpawner.GetAllSpawn();

        if (allSpawn.Count == 0)
            return 0;

        var byType = new Dictionary<string, List<BaseCreature>>(StringComparer.OrdinalIgnoreCase);

        foreach (var bc in allSpawn)
        {
            if (bc == null || bc.Deleted)
                continue;

            string name = bc.GetType().Name;

            if (!byType.TryGetValue(name, out var typeList))
            {
                typeList = [];
                byType[name] = typeList;
            }

            typeList.Add(bc);
        }

        int totalTrimmed = 0;

        foreach (var (typeName, typeSpawn) in byType)
        {
            int typeCount = typeSpawn.Count;

            if (typeCount <= typeLimit)
                continue;

            int excess = typeCount - typeLimit;

            var candidates = typeSpawn
                .Where(bc => bc.Alive && IsOutOfSight(bc))
                .Select(bc => new { Creature = bc, DistSq = GetMinPlayerDistanceSquared(bc) })
                .OrderByDescending(x => x.DistSq)
                .Take(excess)
                .Select(x => x.Creature);

            int typeTrimmed = 0;

            foreach (var spawn in candidates)
            {
                if (spawn != null && !spawn.Deleted)
                {
                    spawn.Delete();
                    typeTrimmed++;
                    _TotalTrimmed++;
                }
            }

            if (typeTrimmed > 0)
            {
                totalTrimmed += typeTrimmed;

                if (UOR_Settings.ENABLE_DEBUG)
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[{typeName}: {typeCount}->{typeCount - typeTrimmed}]");
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

        return minDistSq;
    }

    }
