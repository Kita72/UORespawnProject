using System.Collections.Generic;

using Server.Custom.UORespawnSystem.SpawnUtility;
using Server.Custom.UORespawnSystem.SpawnHelpers;
using Server.Custom.UORespawnSystem.Entities;

namespace Server.Custom.UORespawnSystem
{
    internal static class RegionSpawner
    {
        internal static string TryRegionSpawn(Map map, Region region, Point3D location, bool isWater)
        {

            if (region == null || region.Name == null) return string.Empty;


            List<RegionEntity> spawnList = new List<RegionEntity>();

            if (UORespawnDataBase.RegionSpawns.Count > 0)
            {
                spawnList.AddRange(UORespawnDataBase.RegionSpawns[map]);
            }

            try
            {
                if (spawnList.Count > 0)
                {
                    RegionEntity entity = spawnList.Find(e => e.RegionHandle == region);

                    if (entity != null)
                    {
                        return SpawnFactory.GetSpawnEntity(entity, map, location, isWater);
                    }
                }
            }
            catch
            {
                UORespawnUtility.SendConsoleMsg(System.ConsoleColor.DarkRed, "Factory => Regions Error!");
            }

            return string.Empty;
        }
    }
}
