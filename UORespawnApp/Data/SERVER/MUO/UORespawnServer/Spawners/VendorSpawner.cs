using System;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Mobiles;

namespace Server.Custom.UORespawnServer.Spawners;
internal static class VendorSpawner
{
    private static int _SpawnAttempts;
    private static int _SpawnSuccesses;
    private static int _SpawnFailures;

    /// <summary>
    /// Spawns all vendors for a VendorEntity.
    /// Returns number of vendors successfully spawned.
    /// Ownership is tracked via ISpawner pattern (UOR_VendorSpawner).
    /// </summary>
    internal static int SpawnVendors(Map map, VendorEntity entity)
    {
        if (entity?.VendorList == null || entity.VendorList.Count == 0)
            return 0;

        int spawned = 0;

        for (int i = 0; i < entity.VendorList.Count; i++)
        {
            _SpawnAttempts++;

            if (!(UOR_Utility.CreateSpawn(entity.VendorList[i]) is BaseCreature vendor))
            {
                _SpawnFailures++;
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Failed to create vendor '{entity.VendorList[i]}' - skipping");
                continue;
            }

            _SpawnSuccesses++;

            Point3D spawnPoint = FindValidSpawnPoint(map, entity.Location);
            Serial serial = SpawnVendor(map, vendor, spawnPoint);

            if (serial.IsMobile)
            {
                // No entity tracking needed - ISpawner pattern handles it
                spawned++;
            }

            // Extra town NPC if enabled
            if (UOR_Settings.ENABLE_VENDOR_EXTRA)
            {
                var townNpc = new TownNPC();
                Point3D extraPoint = FindValidSpawnPoint(map, entity.Location);
                SpawnVendor(map, townNpc, extraPoint);
                // ISpawner ownership assigned in SpawnVendor
            }
        }

        if (_SpawnAttempts % 100 == 0)
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDOR SPAWN-[{_SpawnSuccesses}/{_SpawnAttempts} succeeded, {_SpawnFailures} failed]");
        }

        return spawned;
    }

    /// <summary>
    /// Finds a valid spawn point near the target location.
    /// </summary>
    private static Point3D FindValidSpawnPoint(Map map, Point3D targetLocation)
    {
        if (map.CanSpawnMobile(targetLocation))
        {
            return targetLocation;
        }

        for (int attempt = 0; attempt < UOR_Settings.MAX_SPAWN_CHECKS; attempt++)
        {
            Point3D candidate = UOR_Utility.GetSpawnPoint(targetLocation, 1, 6, map, out bool isWater, out _);

            if (!isWater && map.CanFit(candidate, 40))
            {
                return candidate;
            }
        }

        UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Could not find valid spawn point near {targetLocation}");
        return targetLocation;
    }

    /// <summary>
    /// Spawns a single vendor and returns its Serial.
    /// </summary>
    private static Serial SpawnVendor(Map map, BaseCreature vendor, Point3D location)
    {
        if (vendor == null)
            return Serial.MinusOne;

        // Assign UOR_VendorSpawner ownership via ISpawner pattern
        UOR_VendorSpawner.Instance.Claim(vendor, location);

        vendor.OnBeforeSpawn(location, map);
        vendor.MoveToWorld(location, map);
        vendor.OnAfterSpawn();

        return vendor.Serial;
    }
}
