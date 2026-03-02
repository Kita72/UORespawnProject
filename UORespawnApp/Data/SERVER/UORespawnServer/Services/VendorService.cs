using System;
using System.Linq;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Mobiles;
using Server.Custom.UORespawnServer.Spawners;
using Server.Custom.UORespawnServer.Managers;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Vendor management service.
    /// 
    /// NOTE: Initial vendor spawning is now handled centrally in UOR_Core.OnServerStarted().
    /// This ensures proper ordering: Reclaim → Cleanup → Vendor Init → Services.
    /// 
    /// This class handles runtime vendor operations:
    /// - ResetVendors() - manual vendor reset
    /// - UpdateTime() - night mode visibility
    /// - RespawnVendorsAtLocation() - location-specific respawn
    /// - ValidateAndRespawn() - missing vendor detection
    /// </summary>
    internal class VendorService
    {
        internal VendorService()
        {
            // Startup vendor initialization moved to UOR_Core.OnServerStarted()
        }

        /// <summary>
        /// Resets all vendors - deletes existing and respawns from config.
        /// </summary>
        internal void ResetVendors()
        {
            DeleteAllVendors();

            if (UOR_Settings.ENABLE_VENDOR_SPAWN)
            {
                SpawnAllVendors();
            }
            else
            {
                UOR_Settings.ENABLE_VENDOR_NIGHT = false;
                UOR_Settings.ENABLE_VENDOR_EXTRA = false;
            }
        }

        /// <summary>
        /// Spawns all vendors from config data.
        /// </summary>
        private void SpawnAllVendors()
        {
            var vendorSpawns = SpawnManager.VendorSpawns;

            if (vendorSpawns == null || vendorSpawns.Count == 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[No vendor spawn data found]");
                return;
            }

            int totalSpawned = 0;
            int totalEntities = 0;

            foreach (var kvp in vendorSpawns)
            {
                Map map = kvp.Key;

                foreach (var entity in kvp.Value)
                {
                    totalEntities++;

                    if (entity.VendorList.Count > 0)
                    {
                        int spawned = VendorSpawner.SpawnVendors(map, entity);
                        totalSpawned += spawned;
                    }
                }
            }

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{totalSpawned} spawned across {totalEntities} locations]");
        }

        internal void DeleteAllVendors()
        {
            // Use ISpawner pattern - single call deletes all vendor spawn
            int totalDeleted = UOR_VendorSpawner.CleanupAll();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{totalDeleted} Deleted via ISpawner]");
        }

        internal void UpdateTime()
        {
            ToggleWorking(UOR_Settings.ENABLE_VENDOR_NIGHT);
        }

        private void ToggleWorking(bool nightModeEnabled)
        {
            // Use ISpawner-based query to get all vendor spawn
            var allVendorSpawn = UOR_VendorSpawner.GetAllSpawn();

            if (allVendorSpawn == null || allVendorSpawn.Count == 0)
                return;

            int hidden = 0;

            foreach (var creature in allVendorSpawn)
            {
                if (creature is BaseVendor bv)
                {
                    bv.Hidden = nightModeEnabled && UOR_Utility.IsNight(bv.Map, bv.Location);
                    bv.CantWalk = bv.Hidden;

                    if (bv.Hidden)
                        hidden++;

                    if (!nightModeEnabled)
                    {
                        NPCUtility.CheckNightDress(bv);
                    }
                }
                else
                {
                    NPCUtility.CheckNightDress(creature);
                }
            }

            if (hidden > 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{hidden} Hidden]");
            }
        }

        /// <summary>
        /// Respawns vendors at a specific location by deleting existing and spawning from config.
        /// Uses ISpawner pattern to find and delete vendors near the location.
        /// </summary>
        /// <param name="map">The map the vendors are on</param>
        /// <param name="entity">The VendorEntity with updated vendor list</param>
        /// <returns>Number of vendors spawned</returns>
        internal int RespawnVendorsAtLocation(Map map, VendorEntity entity)
        {
            if (map == null || entity == null)
                return 0;

            // Find and delete existing vendors near this location using ISpawner
            var allVendors = UOR_VendorSpawner.GetAllSpawn();
            int deleted = 0;
            int range = 8; // Search range for vendors at this location

            foreach (var vendor in allVendors.ToList())
            {
                if (vendor.Map == map && vendor.GetDistanceToSqrt(entity.Location) <= range)
                {
                    vendor.Delete();
                    deleted++;
                }
            }

            if (deleted > 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{deleted} deleted at {entity.Location}]");
            }

            // Respawn from updated config
            int spawned = 0;
            if (entity.VendorList.Count > 0)
            {
                spawned = VendorSpawner.SpawnVendors(map, entity);
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{spawned} respawned at {entity.Location}]");
            }

            return spawned;
        }

        internal int GetTotalVendorCount()
        {
            // Use ISpawner-based query instead of entity tracking lists
            return UOR_VendorSpawner.GetCount();
        }

        /// <summary>
        /// Validates all vendor entities and respawns any that are missing vendors.
        /// </summary>
        internal void ValidateAndRespawn()
        {
            var vendorSpawns = SpawnManager.VendorSpawns;

            if (vendorSpawns == null)
                return;

            int respawned = 0;

            foreach (var kvp in vendorSpawns)
            {
                Map map = kvp.Key;

                foreach (var entity in kvp.Value)
                {
                    if (entity.NeedsSpawn())
                    {
                        respawned += VendorSpawner.SpawnVendors(map, entity);
                    }
                }
            }

            if (respawned > 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{respawned} Respawned]");
            }
        }

        /// <summary>
        /// Called on world save - no longer needs serial persistence since ISpawner handles tracking.
        /// </summary>
        internal void Save()
        {
            // ISpawner pattern handles spawn tracking automatically
            // No serial persistence needed
            int count = GetTotalVendorCount();
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{count} active]");
        }
    }
}
