using System;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Managers;

namespace Server.Custom.UORespawnServer
{
    internal static class RegionSpawner
    {
        internal static string TryRegionSpawn(SpawnEntity spawn)
        {
            if (spawn.RegionName == null || spawn.RegionName.Name == null) 
                return string.Empty;

            if (!SpawnManager.RegionLookup.TryGetValue(spawn.Facet, out var regionDict))
            {
                return string.Empty;
            }

            try
            {
                if (regionDict.TryGetValue(spawn.RegionName, out RegionEntity entity))
                {
                    string spawnName = UOR_Utility.GetSpawnName(entity, spawn);

                    return spawnName;
                }
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"REGION SPAWN: {ex.Message}");
            }

            return string.Empty;
        }
    }
}
