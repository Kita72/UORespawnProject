using System;
using System.Linq;

using Server.Items;
using Server.Mobiles;
using Server.Targeting;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Gumps;
using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Managers;
using Server.Custom.UORespawnServer.Targets;
using Server.Custom.UORespawnServer.Helpers;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Service for Control Gump - handles stats, toggles, and settings adjustments.
    /// Follows SRP: Gump only handles UI, this handles all business logic.
    /// </summary>
    internal class ControlService
    {
        private Timer _RefreshTimer;
        private PlayerMobile _ActiveUser;
        internal bool SystemPower { get; private set; }

        internal ControlService()
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, $"CONTROLS-[Loaded]");

            SystemPower = true;
        }

        #region Gump Management

        internal void OpenGump(PlayerMobile pm)
        {
            _ActiveUser = pm;

            pm.CloseGump(typeof(ControlGump));
            pm.SendGump(new ControlGump(pm, this));

            StartRefresh();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"CONTROLS GUMP-[{pm.Name} Accessed]");
        }

        internal void CloseGump()
        {
            StopRefresh();

            _ActiveUser = null;
        }

        internal void EditSpawn()
        {
            if (_ActiveUser == null) return;

            _ActiveUser.Target = new SpawnControlTarget(_ActiveUser, this);
        }

        private void StartRefresh()
        {
            StopRefresh();

            _RefreshTimer = Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), RefreshGump);
        }

        private void StopRefresh()
        {
            _RefreshTimer?.Stop();
            _RefreshTimer = null;
        }

        internal void RefreshGump()
        {
            if (_ActiveUser == null || _ActiveUser.Deleted || _ActiveUser.NetState == null)
            {
                StopRefresh();
                return;
            }

            _ActiveUser.CloseGump(typeof(ControlGump));
            _ActiveUser.SendGump(new ControlGump(_ActiveUser, this));
        }

        #endregion

        #region System Stats

        internal int GetPlayerCount()
        {
            return UOR_Core.GetRespawners(out var list) ? list.Count : 0;
        }

        internal int GetAllSpawnCount()
        {
            return UOR_Utility.GetAllSpawn()?.Count ?? 0;
        }

        internal int GetQueuedCount()
        {
            int total = 0;

            if (UOR_Core.GetRespawners(out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    total += list[i].GetQueCount();
                }
            }

            return total;
        }

        /// <summary>
        /// Gets total relocations performed this session.
        /// Shows how many spawns were reused instead of created new.
        /// </summary>
        internal int GetRecycledCount()
        {
            return UOR_Core.GetRelocatedCount();
        }

        internal bool GetIsPaused() => UOR_Core.IsPaused;
        internal bool GetIsLocked() => UOR_Core.IsLocked;
        internal string GetVersion() => UOR_Settings.VERSION;

        #endregion

        #region System Toggles

        internal void ToggleLock()
        {
            UOR_Core.ToggleLock();
        }

        internal void ToggleDebug()
        {
            UOR_Settings.ENABLE_DEBUG = !UOR_Settings.ENABLE_DEBUG;
        }

        internal void ToggleEffects()
        {
            UOR_Settings.ENABLE_SPAWN_EFFECTS = !UOR_Settings.ENABLE_SPAWN_EFFECTS;
        }

        internal void ToggleTownSpawn()
        {
            UOR_Settings.ENABLE_TOWN_SPAWN = !UOR_Settings.ENABLE_TOWN_SPAWN;
        }

        internal void ToggleGraveSpawn()
        {
            UOR_Settings.ENABLE_GRAVE_SPAWN = !UOR_Settings.ENABLE_GRAVE_SPAWN;
        }

        internal void ToggleRiftSpawn()
        {
            UOR_Settings.ENABLE_RIFT_SPAWN = !UOR_Settings.ENABLE_RIFT_SPAWN;
        }

        internal void ToggleVendorSpawn()
        {
            UOR_Settings.ENABLE_VENDOR_SPAWN = !UOR_Settings.ENABLE_VENDOR_SPAWN;

            UOR_Core.UpdateVendorService();
        }

        internal void ToggleVendorNight()
        {
            if (UOR_Settings.ENABLE_VENDOR_SPAWN)
            {
                UOR_Settings.ENABLE_VENDOR_NIGHT = !UOR_Settings.ENABLE_VENDOR_NIGHT;
            }
        }

        internal void ToggleVendorExtra()
        {
            if (UOR_Settings.ENABLE_VENDOR_SPAWN)
            {
                UOR_Settings.ENABLE_VENDOR_EXTRA = !UOR_Settings.ENABLE_VENDOR_EXTRA;

                UOR_Core.UpdateVendorService();
            }
        }

        internal void ToggleScaleSpawn()
        {
            UOR_Settings.ENABLE_SCALE_SPAWN = !UOR_Settings.ENABLE_SCALE_SPAWN;
        }

        #endregion

        #region Value Adjustments

        // All adjustments apply delta directly - UOR_Settings.ValidateSettings() is the source of truth
        // for valid ranges. Minimal sanity checks here (non-negative where required, 0-1 for chances).

        // Scale Modifier
        internal void AdjustScaleMod(double delta)
        {
            UOR_Settings.UpdateScaleMod(Math.Max(0.1, UOR_Settings.SCALE_MOD + delta));
        }

        // Intervals (must be >= 1)
        internal void AdjustSearchInterval(int delta) => UOR_Settings.SEARCH_INTERVAL = Math.Max(1, UOR_Settings.SEARCH_INTERVAL + delta);
        internal void AdjustProcessInterval(int delta) => UOR_Settings.PROCESS_INTERVAL = Math.Max(1, UOR_Settings.PROCESS_INTERVAL + delta);
        internal void AdjustValidateInterval(int delta) => UOR_Settings.VALIDATE_INTERVAL = Math.Max(1, UOR_Settings.VALIDATE_INTERVAL + delta);
        internal void AdjustTimedInterval(int delta) => UOR_Settings.TIMED_INTERVAL = Math.Max(1, UOR_Settings.TIMED_INTERVAL + delta);

        // Limits (must be >= 1)
        internal void AdjustMaxSpawn(int delta) => UOR_Settings.MAX_SPAWN_VAL = Math.Max(1, UOR_Settings.MAX_SPAWN_VAL + delta);
        internal void AdjustMaxRange(int delta) => UOR_Settings.MAX_RANGE_VAL = Math.Max(1, UOR_Settings.MAX_RANGE_VAL + delta);
        internal void AdjustMinRange(int delta) => UOR_Settings.MIN_RANGE_VAL = Math.Max(1, UOR_Settings.MIN_RANGE_VAL + delta);
        internal void AdjustMaxCrowd(int delta) => UOR_Settings.MAX_CROWD_VAL = Math.Max(1, UOR_Settings.MAX_CROWD_VAL + delta);
        internal void AdjustMaxQueueSize(int delta) => UOR_Settings.MAX_QUEUE_SIZE = Math.Max(1, UOR_Settings.MAX_QUEUE_SIZE + delta);

        // Chances (probability must be 0.0 - 1.0)
        internal void AdjustChanceWater(double delta) => UOR_Settings.CHANCE_WATER = ClampChance(UOR_Settings.CHANCE_WATER + delta);
        internal void AdjustChanceWeather(double delta) => UOR_Settings.CHANCE_WEATHER = ClampChance(UOR_Settings.CHANCE_WEATHER + delta);
        internal void AdjustChanceTimed(double delta) => UOR_Settings.CHANCE_TIMED = ClampChance(UOR_Settings.CHANCE_TIMED + delta);
        internal void AdjustChanceCommon(double delta) => UOR_Settings.CHANCE_COMMON = ClampChance(UOR_Settings.CHANCE_COMMON + delta);
        internal void AdjustChanceUncommon(double delta) => UOR_Settings.CHANCE_UNCOMMON = ClampChance(UOR_Settings.CHANCE_UNCOMMON + delta);
        internal void AdjustChanceRare(double delta) => UOR_Settings.CHANCE_RARE = ClampChance(UOR_Settings.CHANCE_RARE + delta);

        /// <summary>
        /// Clamps a chance value to valid probability range (0.0 - 1.0).
        /// This is a mathematical requirement, not an arbitrary limit.
        /// </summary>
        private static double ClampChance(double value) => Math.Max(0.0, Math.Min(1.0, value));

        #endregion

        #region Save Settings

        internal void SaveSettings()
        {
            // Log all current settings as commands for the editor to consume
            int commandsLogged = 0;

            // System Intervals
            commandsLogged += LogSettingCommand("SEARCH_INTERVAL", UOR_Settings.SEARCH_INTERVAL.ToString());
            commandsLogged += LogSettingCommand("PROCESS_INTERVAL", UOR_Settings.PROCESS_INTERVAL.ToString());
            commandsLogged += LogSettingCommand("VALIDATE_INTERVAL", UOR_Settings.VALIDATE_INTERVAL.ToString());
            commandsLogged += LogSettingCommand("TIMED_INTERVAL", UOR_Settings.TIMED_INTERVAL.ToString());

            // System Limits
            commandsLogged += LogSettingCommand("MAX_RECYCLE_TYPE", UOR_Settings.MAX_RECYCLE_TYPE.ToString());
            commandsLogged += LogSettingCommand("MAX_SPAWN_CHECKS", UOR_Settings.MAX_SPAWN_CHECKS.ToString());
            commandsLogged += LogSettingCommand("MAX_QUEUE_SIZE", UOR_Settings.MAX_QUEUE_SIZE.ToString());
            commandsLogged += LogSettingCommand("MAX_STAT_SIZE", UOR_Settings.MAX_STAT_SIZE.ToString());

            // Spawn Limits
            commandsLogged += LogSettingCommand("MAX_SPAWN", UOR_Settings.MAX_SPAWN_VAL.ToString());
            commandsLogged += LogSettingCommand("MIN_RANGE", UOR_Settings.MIN_RANGE_VAL.ToString());
            commandsLogged += LogSettingCommand("MAX_RANGE", UOR_Settings.MAX_RANGE_VAL.ToString());
            commandsLogged += LogSettingCommand("MAX_CROWD", UOR_Settings.MAX_CROWD_VAL.ToString());
            commandsLogged += LogSettingCommand("SCALE_MOD", UOR_Settings.SCALE_MOD.ToString("F2"));

            // Spawn Chances
            commandsLogged += LogSettingCommand("CHANCE_WATER", UOR_Settings.CHANCE_WATER.ToString("F2"));
            commandsLogged += LogSettingCommand("CHANCE_WEATHER", UOR_Settings.CHANCE_WEATHER.ToString("F2"));
            commandsLogged += LogSettingCommand("CHANCE_TIMED", UOR_Settings.CHANCE_TIMED.ToString("F2"));
            commandsLogged += LogSettingCommand("CHANCE_COMMON", UOR_Settings.CHANCE_COMMON.ToString("F2"));
            commandsLogged += LogSettingCommand("CHANCE_UNCOMMON", UOR_Settings.CHANCE_UNCOMMON.ToString("F2"));
            commandsLogged += LogSettingCommand("CHANCE_RARE", UOR_Settings.CHANCE_RARE.ToString("F2"));

            // Spawn Toggles
            commandsLogged += LogSettingCommand("ENABLE_SCALE_SPAWN", UOR_Settings.ENABLE_SCALE_SPAWN.ToString());
            commandsLogged += LogSettingCommand("ENABLE_RIFT_SPAWN", UOR_Settings.ENABLE_RIFT_SPAWN.ToString());
            commandsLogged += LogSettingCommand("ENABLE_TOWN_SPAWN", UOR_Settings.ENABLE_TOWN_SPAWN.ToString());
            commandsLogged += LogSettingCommand("ENABLE_GRAVE_SPAWN", UOR_Settings.ENABLE_GRAVE_SPAWN.ToString());

            // Vendor Toggles
            commandsLogged += LogSettingCommand("ENABLE_VENDOR_SPAWN", UOR_Settings.ENABLE_VENDOR_SPAWN.ToString());
            commandsLogged += LogSettingCommand("ENABLE_VENDOR_NIGHT", UOR_Settings.ENABLE_VENDOR_NIGHT.ToString());
            commandsLogged += LogSettingCommand("ENABLE_VENDOR_EXTRA", UOR_Settings.ENABLE_VENDOR_EXTRA.ToString());

            // Other Toggles
            commandsLogged += LogSettingCommand("ENABLE_SPAWN_EFFECTS", UOR_Settings.ENABLE_SPAWN_EFFECTS.ToString());
            commandsLogged += LogSettingCommand("ENABLE_DEBUG", UOR_Settings.ENABLE_DEBUG.ToString());

            UOR_Utility.SendMsg(ConsoleColor.Green, $"CONTROLS-[Logged {commandsLogged} setting commands]");

            _ActiveUser?.SendMessage(0x35, $"Settings logged ({commandsLogged} commands) for editor sync!");
        }

        /// <summary>
        /// Logs a single setting command. Returns 1 if successful, 0 otherwise.
        /// </summary>
        private int LogSettingCommand(string key, string value)
        {
            return CommandManager.WriteSettingsCommand(key, value) ? 1 : 0;
        }

        internal void TogglePower()
        {
            SystemPower = !SystemPower;

            if (SystemPower)
            {
                // Start the system at runtime using STARTUP (not Initialize which is for server boot)
                UOR_Core.STARTUP();

                UOR_Core.RelogPlayers();
            }
            else
            {
                UOR_Core.SHUTDOWN();
            }

            World.Save();
        }

        internal void TryOpenSpawnEditor(PlayerMobile m_Mobile, LandTarget land)
        {
            if (m_Mobile == null || land == null)
                return;

            Map map = m_Mobile.Map;
            Point3D location = land.Location;
            string tileName = TileHelper.GetTileName(land.TileID, map, location);

            int gumpsOpened = 0;

            // Check for BoxEntity at this location (highest priority spawn)
            BoxEntity box = SpatialGridManager.GetBoxAt(map, location);
            if (box != null)
            {
                string boxName = $"Box_{box.Id}";
                var boxService = new SpawnEditService(m_Mobile, box, CommandTarget.Box, boxName);
                boxService.OpenGump();
                gumpsOpened++;
            }

            // Check for RegionEntity at this location (walk up parent chain)
            RegionEntity regionEntity = FindRegionEntity(map, location);
            if (regionEntity != null)
            {
                var regionService = new SpawnEditService(m_Mobile, regionEntity, CommandTarget.Region, regionEntity.Name);
                regionService.OpenGump();
                gumpsOpened++;
            }

            // Check for TileEntity by tile name
            if (!string.IsNullOrWhiteSpace(tileName) && SpawnManager.TileLookup.TryGetValue(map, out var tileDict))
            {
                if (tileDict.TryGetValue(tileName, out TileEntity tileEntity))
                {
                    var tileService = new SpawnEditService(m_Mobile, tileEntity, CommandTarget.Tile, tileEntity.Name);
                    tileService.OpenGump();
                    gumpsOpened++;
                }
            }

            if (gumpsOpened == 0)
            {
                m_Mobile.SendMessage(0x22, $"No spawn data found for tile '{tileName}' at {location}.");
                m_Mobile.SendMessage(0x35, "Use the Editor to create spawn data for this location.");

                // Re-issue target for another attempt
                m_Mobile.Target = new Targets.SpawnControlTarget(m_Mobile, this);
            }
            else
            {
                m_Mobile.SendMessage(0x40, $"Opened {gumpsOpened} spawn editor(s) for this location.");
            }
        }

        internal void TryOpenSpawnEditor(PlayerMobile m_Mobile, StaticTarget decor)
        {
            if (m_Mobile == null || decor == null)
                return;

            Map map = m_Mobile.Map;
            Point3D location = decor.Location;
            string staticName = decor.Name;

            int gumpsOpened = 0;

            // Check for BoxEntity at this location
            BoxEntity box = SpatialGridManager.GetBoxAt(map, location);
            if (box != null)
            {
                string boxName = $"Box_{box.Id}";
                var boxService = new SpawnEditService(m_Mobile, box, CommandTarget.Box, boxName);
                boxService.OpenGump();
                gumpsOpened++;
            }

            // Check for RegionEntity at this location (walk up parent chain)
            RegionEntity regionEntity = FindRegionEntity(map, location);
            if (regionEntity != null)
            {
                var regionService = new SpawnEditService(m_Mobile, regionEntity, CommandTarget.Region, regionEntity.Name);
                regionService.OpenGump();
                gumpsOpened++;
            }

            // Check for TileEntity by static name (statics can also have tile-based spawn)
            if (!string.IsNullOrWhiteSpace(staticName) && SpawnManager.TileLookup.TryGetValue(map, out var tileDict))
            {
                if (tileDict.TryGetValue(staticName, out TileEntity tileEntity))
                {
                    var tileService = new SpawnEditService(m_Mobile, tileEntity, CommandTarget.Tile, tileEntity.Name);
                    tileService.OpenGump();
                    gumpsOpened++;
                }
            }

            if (TryByDecorName(m_Mobile, decor))
            {
                // TODO: Process Success
            }

            if (gumpsOpened == 0)
            {
                m_Mobile.SendMessage(0x22, $"No spawn data found for static '{staticName}' at {location}.");
                m_Mobile.SendMessage(0x35, "Use the Editor to create spawn data for this location.");

                // Re-issue target for another attempt
                m_Mobile.Target = new Targets.SpawnControlTarget(m_Mobile, this);
            }
            else
            {
                m_Mobile.SendMessage(0x40, $"Opened {gumpsOpened} spawn editor(s) for this location.");
            }
        }

        private bool TryByDecorName(PlayerMobile pm, StaticTarget decor)
        {
            switch (decor.Name.ToLower())
            {
                case "beehive": // Vendors
                    {
                        return TryOpenVendorEditor(pm, decor.Location, isSign: false, displayName: "Beehive");
                    }
                case "grave": // Undead // future addition once we get Static Spawn Supported!
                    {
                        // TODO: Send SpawnEditService
                        return false;
                    }
                case "gravestone": // Undead // future addition once we get Static Spawn Supported!
                    {
                        // TODO: Send SpawnEditService
                        return false;
                    }
                default: return false;
            }
        }

        internal void TryOpenSpawnEditor(PlayerMobile m_Mobile, Item item)
        {
            if (m_Mobile == null || item == null)
                return;

            Map map = m_Mobile.Map;
            Point3D location = item.Location;
            string itemName = item.Name ?? item.GetType().Name;

            int gumpsOpened = 0;

            // Check for BoxEntity at this location
            BoxEntity box = SpatialGridManager.GetBoxAt(map, location);
            if (box != null)
            {
                string boxName = $"Box_{box.Id}";
                var boxService = new SpawnEditService(m_Mobile, box, CommandTarget.Box, boxName);
                boxService.OpenGump();
                gumpsOpened++;
            }

            // Check for RegionEntity at this location (walk up parent chain)
            RegionEntity regionEntity = FindRegionEntity(map, location);

            if (regionEntity != null)
            {
                var regionService = new SpawnEditService(m_Mobile, regionEntity, CommandTarget.Region, regionEntity.Name);
                regionService.OpenGump();
                gumpsOpened++;
            }

            // Check for TileEntity by item name
            if (!string.IsNullOrWhiteSpace(itemName) && SpawnManager.TileLookup.TryGetValue(map, out var tileDict))
            {
                if (tileDict.TryGetValue(itemName, out TileEntity tileEntity))
                {
                    var tileService = new SpawnEditService(m_Mobile, tileEntity, CommandTarget.Tile, tileEntity.Name);
                    tileService.OpenGump();
                    gumpsOpened++;
                }
            }

            if (TryByItemName(m_Mobile, item))
            {
                // TODO: Process Success
            }

            if (gumpsOpened == 0)
            {
                m_Mobile.SendMessage(0x22, $"No spawn data found for item '{itemName}' at {location}.");
                m_Mobile.SendMessage(0x35, "Use the Editor to create spawn data for this location.");

                // Re-issue target for another attempt
                m_Mobile.Target = new SpawnControlTarget(m_Mobile, this);
            }
            else
            {
                m_Mobile.SendMessage(0x40, $"Opened {gumpsOpened} spawn editor(s) for this location.");
            }
        }

        private bool TryByItemName(PlayerMobile pm, Item item)
        {
            // Vendors - check if this is a registered shop sign
            if (item is BaseSign bs)
            {
                if (VendorManager.SignLocations.TryGetValue(pm.Map.MapID, out var signList) &&
                    signList.Any(s => s.Item3 == bs.Location))
                {
                    // Use the actual sign's name from the item, not the stored SignType enum
                    string signName = !string.IsNullOrEmpty(bs.Name) 
                        ? bs.Name 
                        : bs.ItemData.Name;

                    return TryOpenVendorEditor(pm, bs.Location, isSign: true, displayName: signName);
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to open a vendor editor for a sign or beehive location.
        /// </summary>
        /// <param name="displayName">The actual name from the targeted item (sign text or 'Beehive')</param>
        private bool TryOpenVendorEditor(PlayerMobile pm, Point3D targetLocation, bool isSign, string displayName)
        {
            Map map = pm.Map;

            // Find the VendorEntity that matches this location
            VendorEntity vendorEntity = FindVendorEntity(map, targetLocation, isSign);

            if (vendorEntity == null)
            {
                pm.SendMessage(0x22, $"No vendor spawn data found at this location.");
                pm.SendMessage(0x35, "Use the Editor to create vendor spawn data.");
                return false;
            }

            // Use the actual display name from the targeted item, with facing info for signs
            string locationName = isSign
                ? $"{displayName} ({vendorEntity.Facing})"
                : displayName;

            // Open the vendor editor gump
            var vendorService = new VendorEditService(pm, vendorEntity, map, locationName);
            vendorService.OpenGump();

            pm.SendMessage(0x40, $"Opened vendor editor for {locationName}.");
            return true;
        }

        /// <summary>
        /// Finds a VendorEntity by reversing the inside-location offset.
        /// Simple approach: for each vendor, calculate what the original sign/hive location would be.
        /// </summary>
        private VendorEntity FindVendorEntity(Map map, Point3D targetLocation, bool isSign)
        {
            if (!SpawnManager.VendorSpawns.TryGetValue(map, out var vendorList))
                return null;

            foreach (var vendor in vendorList)
            {
                if (vendor.IsSign != isSign)
                    continue;

                // Reverse the offset to get original sign/hive location
                Point2D originalLocation = GetOriginalLocation(vendor);

                if (originalLocation == targetLocation)
                    return vendor;
            }

            return null;
        }

        /// <summary>
        /// Reverses the GetInsideLocation calculation to get the original sign/hive world position.
        /// </summary>
        private Point2D GetOriginalLocation(VendorEntity vendor)
        {
            if (vendor.IsSign)
            {
                switch (vendor.Facing)
                {
                    case SignFacing.West:
                        return new Point2D(vendor.Location.X + 2, vendor.Location.Y);
                    case SignFacing.North:
                        return new Point2D(vendor.Location.X, vendor.Location.Y + 2);
                }
            }
            else
            {
                // Beehive - reverse the +1, +1 offset
                return new Point2D(vendor.Location.X - 1, vendor.Location.Y - 1);
            }

            return new Point2D(vendor.Location.X, vendor.Location.Y);
        }

        /// <summary>
        /// Finds a RegionEntity by walking up the region hierarchy from the location.
        /// Uses both reference-based and name-based lookup for reliability.
        /// </summary>
        private RegionEntity FindRegionEntity(Map map, Point3D location)
        {
            if (map == null)
                return null;

            if (!SpawnManager.RegionLookup.TryGetValue(map, out var regionDict))
                return null;

            // Start with the most specific region and walk up the parent chain
            Region region = Region.Find(location, map);

            while (region != null)
            {
                // Skip regions with null/empty names (world region)
                if (string.IsNullOrEmpty(region.Name))
                {
                    region = region.Parent;
                    continue;
                }

                // Try reference-based lookup first (fastest)
                if (regionDict.TryGetValue(region, out RegionEntity entity))
                    return entity;

                // Fallback: Name-based lookup (handles reference mismatch)
                entity = FindRegionByName(map, region.Name);
                if (entity != null)
                    return entity;

                region = region.Parent;
            }

            return null;
        }

        /// <summary>
        /// Finds a RegionEntity by name (case-insensitive).
        /// Used as fallback when reference-based lookup fails.
        /// </summary>
        private RegionEntity FindRegionByName(Map map, string regionName)
        {
            if (string.IsNullOrEmpty(regionName))
                return null;

            if (!SpawnManager.RegionSpawns.TryGetValue(map, out var regionList))
                return null;

            foreach (var entity in regionList)
            {
                if (entity != null && 
                    !string.IsNullOrEmpty(entity.Name) && 
                    entity.Name.Equals(regionName, StringComparison.OrdinalIgnoreCase))
                {
                    return entity;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a spawn entity has any spawn data in any of its lists.
        /// </summary>
        private bool HasAnySpawnData(Interfaces.ISpawnEntity entity)
        {
            if (entity == null)
                return false;

            return (entity.WaterList?.Count > 0) ||
                   (entity.WeatherList?.Count > 0) ||
                   (entity.TimedList?.Count > 0) ||
                   (entity.CommonList?.Count > 0) ||
                   (entity.UnCommonList?.Count > 0) ||
                   (entity.RareList?.Count > 0);
        }

        #endregion
    }
}
