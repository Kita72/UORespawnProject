using Server.Custom.UORespawnSystem.Entities;
using Server.Custom.UORespawnSystem.SpawnHelpers;
using Server.Custom.UORespawnSystem.SpawnUtility;
using Server.Mobiles;

namespace Server.Custom.UORespawnSystem.SpawnWorkers
{
    internal static class VendorSpawner
    {
        internal static void TryToSpawn(Map map, VendorEntity entity)
        {
            if (entity?.VendorList?.Count == 0) return;

            for (int i = 0; i < entity.VendorList.Count; i++)
            {
                BaseCreature vendor = UORespawnUtility.CreateSpawn(entity.VendorList[i]) as BaseCreature;

                Spawn(map, vendor, entity.Location);
            }
        }

        private static void Spawn(Map map, BaseCreature vendor, Point3D location)
        {
            if (vendor != null)
            {
                vendor.Home = location;
                vendor.RangeHome = 15;

                vendor.OnBeforeSpawn(location, map);
                vendor.MoveToWorld(location, map);
                vendor.OnAfterSpawn();

                SpawnVendors.VendorSpawnList.Add(vendor.Serial.Value);
            }
        }
    }
}
