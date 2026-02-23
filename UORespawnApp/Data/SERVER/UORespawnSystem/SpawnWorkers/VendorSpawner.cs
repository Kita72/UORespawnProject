using Server.Custom.UORespawnSystem.Entities;
using Server.Custom.UORespawnSystem.Mobiles;
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

                Point3D spawnPoint = entity.Location;

                int count = UORespawnSettings.MAX_SPAWN_CHECKS;

                while (!map.CanSpawnMobile(spawnPoint) && count > 0)
                {
                    spawnPoint = UORespawnUtility.GetSpawnPoint(entity.Location, 1, 6, map);

                    count--;
                }

                if (count == 0)
                {
                    spawnPoint = entity.Location;
                }

                Spawn(map, vendor, spawnPoint);

                if (UORespawnSettings.ENABLE_VENDOR_EXTRA)
                {
                    Spawn(map, new TownNPC(), spawnPoint);
                }
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
