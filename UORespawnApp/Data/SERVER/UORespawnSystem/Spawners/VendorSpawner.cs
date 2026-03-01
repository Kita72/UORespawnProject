using System;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Mobiles;
using Server.Custom.UORespawnServer.Services;

namespace Server.Custom.UORespawnServer.Spawners
{
    internal static class VendorSpawner
    {
        private static VendorService _Service;

        private static int _SpawnAttempts;
        private static int _SpawnSuccesses;
        private static int _SpawnFailures;

        internal static void TryToSpawn(Map map, VendorEntity entity, VendorService service)
        {
            if (entity?.VendorList == null || entity.VendorList.Count == 0)
                return;

            if (service == null) return;

            _Service = service;

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDOR SPAWN-[Attempting {entity.VendorList.Count} vendors at {entity.Location}]");

            for (int i = 0; i < entity.VendorList.Count; i++)
            {
                _SpawnAttempts++;

                if (!(UOR_Utility.CreateSpawn(entity.VendorList[i]) is BaseCreature vendor))
                {
                    _SpawnFailures++;

                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Failed to create vendor '{entity.VendorList[i]}' - skipping");
                    {
                        continue;
                    }
                }

                _SpawnSuccesses++;

                Spawn(map, vendor, FindValidSpawnPoint(map, entity.Location));

                if (UOR_Settings.ENABLE_VENDOR_EXTRA)
                {
                    Spawn(map, new TownNPC(), FindValidSpawnPoint(map, entity.Location));
                }
            }

            if (_SpawnAttempts % 100 == 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDOR SPAWN-[{_SpawnSuccesses}/{_SpawnAttempts} succeeded, {_SpawnFailures} failed]");
            }
        }

        /// <summary>
        /// Finds a valid spawn point near the target location.
        /// Uses a safe iteration limit to prevent infinite loops.
        /// </summary>
        private static Point3D FindValidSpawnPoint(Map map, Point3D targetLocation)
        {
            // First, try the exact location
            if (map.CanSpawnMobile(targetLocation))
            {
                return targetLocation;
            }

            // Search for nearby valid point with safety limit
            for (int attempt = 0; attempt < UOR_Settings.MAX_SPAWN_CHECKS; attempt++)
            {
                Point3D candidate = UOR_Utility.GetSpawnPoint(targetLocation, 1, 6, map, out bool isWater);

                if (!isWater && map.CanSpawnMobile(candidate))
                {
                    return candidate;
                }
            }

            // Fallback: return original location (let ServUO handle placement)
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Could not find valid spawn point near {targetLocation}");

            return targetLocation;
        }

        private static void Spawn(Map map, BaseCreature vendor, Point3D location)
        {
            if (vendor == null)
                return;

            vendor.Home = new Point3D(location.X, location.Y, UOR_Settings.VENDOR_MARKER);
            vendor.RangeHome = 15;

            vendor.OnBeforeSpawn(location, map);
            vendor.MoveToWorld(location, map);
            vendor.OnAfterSpawn();

            _Service.AddVendor(vendor);

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDOR SPAWN-[Spawned {vendor.GetType().Name} at {location} on {map.Name}]");
        }
    }
}
