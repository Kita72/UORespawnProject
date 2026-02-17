using System.Linq;
using System.Collections.Generic;

using Server.Custom.UORespawnSystem.Entities;
using Server.Custom.UORespawnSystem.SpawnUtility;
using Server.Custom.UORespawnSystem.SpawnHelpers;

namespace Server.Custom.UORespawnSystem
{
    internal static class BoxSpawner
    {
        internal static string TryBoxSpawn(Map map, Point3D location, bool isWater)
        {
            List<BoxEntity> spawnList = new List<BoxEntity>();

            if (UORespawnDataBase.BoxSpawns.Count > 0)
            {
                spawnList.AddRange(UORespawnDataBase.BoxSpawns[map]);
            }

            try
            {
                if (spawnList.Count > 0)
                {
                    var allLocs = spawnList.Where(se => se.SpawnBox.Contains(location)).ToList();

                    if (allLocs != null && allLocs.Count > 0)
                    {
                        BoxEntity entity = allLocs.OrderByDescending(s => s.SpawnPriority).Reverse().First();

                        if (entity != null)
                        {
                            return SpawnFactory.GetSpawnEntity(entity, map, location, isWater);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(System.ConsoleColor.Red, $"ERROR: BoxSpawn failed - {ex.Message}");
            }

            return string.Empty;
        }
    }
}
