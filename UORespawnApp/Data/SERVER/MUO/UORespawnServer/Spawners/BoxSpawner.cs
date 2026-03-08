using System;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Managers;

namespace Server.Custom.UORespawnServer;
internal static class BoxSpawner
{
    private static int _LookupCount;
    private static int _HitCount;

    /// <summary>
    /// Try to get a spawn name from a box at the spawn location.
    /// Uses SpatialGridManager for O(1) lookup instead of iterating all boxes.
    /// </summary>
    internal static string TryBoxSpawn(SpawnEntity spawn)
    {
        _LookupCount++;

        // O(1) lookup via spatial grid
        BoxEntity entity = SpatialGridManager.GetBoxAt(spawn.Facet, spawn.Location);

        if (entity != null)
        {
            _HitCount++;

            string spawnName = UOR_Utility.GetSpawnName(entity, spawn);

            if (!string.IsNullOrEmpty(spawnName))
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"BOX SPAWN-[Found '{spawnName}' at {spawn.Location} (hits: {_HitCount}/{_LookupCount})]");
            }

            return spawnName;
        }

        return string.Empty;
    }
}
