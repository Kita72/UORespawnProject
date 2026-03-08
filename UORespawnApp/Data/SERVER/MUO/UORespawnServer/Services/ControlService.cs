using System;

using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Targeting;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Gumps;
using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Managers;
using Server.Custom.UORespawnServer.Targets;
using Server.Custom.UORespawnServer.Helpers;

namespace Server.Custom.UORespawnServer.Services;
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


    internal void OpenGump(PlayerMobile pm)
    {
        _ActiveUser = pm;

        pm.CloseGump<ControlGump>();
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
        if (_ActiveUser == null)
        {
            return;
        }

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

        _ActiveUser.CloseGump<ControlGump>();
        _ActiveUser.SendGump(new ControlGump(_ActiveUser, this));
    }

    internal static int GetPlayerCount() => UOR_Core.GetRespawners(out var list) ? list.Count : 0;

    internal static int GetAllSpawnCount()
    {
        return UOR_Utility.GetAllSpawn()?.Count ?? 0;
    }

    internal static int GetQueuedCount()
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
    internal static int GetRecycledCount() => UOR_Core.GetRelocatedCount();

    internal static bool GetIsPaused() => UOR_Core.IsPaused;
    internal static bool GetIsLocked() => UOR_Core.IsLocked;
    internal static string GetVersion() => UOR_Settings.VERSION;



    internal static void ToggleLock()
    {
        UOR_Core.ToggleLock();
    }

    internal static void ToggleDebug() => UOR_Settings.ENABLE_DEBUG = !UOR_Settings.ENABLE_DEBUG;

    internal static void ToggleEffects()
    {
        UOR_Settings.ENABLE_SPAWN_EFFECTS = !UOR_Settings.ENABLE_SPAWN_EFFECTS;
    }

    internal static void ToggleTownSpawn()
    {
        UOR_Settings.ENABLE_TOWN_SPAWN = !UOR_Settings.ENABLE_TOWN_SPAWN;
    }

    internal static void ToggleGraveSpawn()
    {
        UOR_Settings.ENABLE_GRAVE_SPAWN = !UOR_Settings.ENABLE_GRAVE_SPAWN;
    }

    internal static void ToggleRiftSpawn()
    {
        UOR_Settings.ENABLE_RIFT_SPAWN = !UOR_Settings.ENABLE_RIFT_SPAWN;
    }

    internal static void ToggleVendorSpawn()
    {
        UOR_Settings.ENABLE_VENDOR_SPAWN = !UOR_Settings.ENABLE_VENDOR_SPAWN;

        UOR_Core.UpdateVendorService();
    }

    internal static void ToggleVendorNight()
    {
        if (UOR_Settings.ENABLE_VENDOR_SPAWN)
        {
            UOR_Settings.ENABLE_VENDOR_NIGHT = !UOR_Settings.ENABLE_VENDOR_NIGHT;
        }
    }

    internal static void ToggleVendorExtra()
    {
        if (UOR_Settings.ENABLE_VENDOR_SPAWN)
        {
            UOR_Settings.ENABLE_VENDOR_EXTRA = !UOR_Settings.ENABLE_VENDOR_EXTRA;

            UOR_Core.UpdateVendorService();
        }
    }

    internal static void ToggleScaleSpawn() => UOR_Settings.ENABLE_SCALE_SPAWN = !UOR_Settings.ENABLE_SCALE_SPAWN;

    // All adjustments apply delta directly - UOR_Settings.ValidateSettings() is the source of truth
    // for valid ranges. Minimal sanity checks here (non-negative where required, 0-1 for chances).

    // Scale Modifier
    internal static void AdjustScaleMod(double delta) => UOR_Settings.UpdateScaleMod(Math.Max(0.1, UOR_Settings.SCALE_MOD + delta));

    // Intervals (must be >= 1)
    internal static void AdjustSearchInterval(int delta) => UOR_Settings.SEARCH_INTERVAL = Math.Max(1, UOR_Settings.SEARCH_INTERVAL + delta);
    internal static void AdjustProcessInterval(int delta) => UOR_Settings.PROCESS_INTERVAL = Math.Max(1, UOR_Settings.PROCESS_INTERVAL + delta);
    internal static void AdjustValidateInterval(int delta) => UOR_Settings.VALIDATE_INTERVAL = Math.Max(1, UOR_Settings.VALIDATE_INTERVAL + delta);
    internal static void AdjustTimedInterval(int delta) => UOR_Settings.TIMED_INTERVAL = Math.Max(1, UOR_Settings.TIMED_INTERVAL + delta);

    // Limits (must be >= 1)
    internal static void AdjustMaxSpawn(int delta) => UOR_Settings.MAX_SPAWN_VAL = Math.Max(1, UOR_Settings.MAX_SPAWN_VAL + delta);
    internal static void AdjustMaxRange(int delta) => UOR_Settings.MAX_RANGE_VAL = Math.Max(1, UOR_Settings.MAX_RANGE_VAL + delta);
    internal static void AdjustMinRange(int delta) => UOR_Settings.MIN_RANGE_VAL = Math.Max(1, UOR_Settings.MIN_RANGE_VAL + delta);
    internal static void AdjustMaxCrowd(int delta) => UOR_Settings.MAX_CROWD_VAL = Math.Max(1, UOR_Settings.MAX_CROWD_VAL + delta);
    internal static void AdjustMaxQueueSize(int delta) => UOR_Settings.MAX_QUEUE_SIZE = Math.Max(1, UOR_Settings.MAX_QUEUE_SIZE + delta);

    // Chances (probability must be 0.0 - 1.0)
    internal static void AdjustChanceWater(double delta) => UOR_Settings.CHANCE_WATER = ClampChance(UOR_Settings.CHANCE_WATER + delta);
    internal static void AdjustChanceWeather(double delta) => UOR_Settings.CHANCE_WEATHER = ClampChance(UOR_Settings.CHANCE_WEATHER + delta);
    internal static void AdjustChanceTimed(double delta) => UOR_Settings.CHANCE_TIMED = ClampChance(UOR_Settings.CHANCE_TIMED + delta);
    internal static void AdjustChanceCommon(double delta) => UOR_Settings.CHANCE_COMMON = ClampChance(UOR_Settings.CHANCE_COMMON + delta);
    internal static void AdjustChanceUncommon(double delta) => UOR_Settings.CHANCE_UNCOMMON = ClampChance(UOR_Settings.CHANCE_UNCOMMON + delta);
    internal static void AdjustChanceRare(double delta) => UOR_Settings.CHANCE_RARE = ClampChance(UOR_Settings.CHANCE_RARE + delta);

    /// <summary>
    /// Clamps a chance value to valid probability range (0.0 - 1.0).
    /// This is a mathematical requirement, not an arbitrary limit.
    /// </summary>
    private static double ClampChance(double value) => Math.Max(0.0, Math.Min(1.0, value));



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
    private static int LogSettingCommand(string key, string value) => CommandManager.WriteSettingsCommand(key, value) ? 1 : 0;

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

    internal void TryOpenSpawnEditor(PlayerMobile mobile, LandTarget land)
    {
        if (mobile == null || land == null)
        {
            return;
        }

        Map map = mobile.Map;
        Point3D location = land.Location;
        string tileName = TileHelper.GetTileName(land.TileID, map, location);

        int gumpsOpened = 0;

        // Check for BoxEntity at this location (highest priority spawn)
        BoxEntity box = SpatialGridManager.GetBoxAt(map, location);
        if (box != null)
        {
            string boxName = $"Box_{box.Id}";
            var boxService = new SpawnEditService(mobile, box, CommandTarget.Box, boxName);
            boxService.OpenGump();
            gumpsOpened++;
        }

        // Check for RegionEntity at this location (walk up parent chain)
        RegionEntity regionEntity = FindRegionEntity(map, location);
        if (regionEntity != null)
        {
            var regionService = new SpawnEditService(mobile, regionEntity, CommandTarget.Region, regionEntity.Name);
            regionService.OpenGump();
            gumpsOpened++;
        }

        // Check for TileEntity by tile name
        if (!string.IsNullOrWhiteSpace(tileName) && SpawnManager.TileLookup.TryGetValue(map, out var tileDict))
        {
            if (tileDict.TryGetValue(tileName, out TileEntity tileEntity))
            {
                var tileService = new SpawnEditService(mobile, tileEntity, CommandTarget.Tile, tileEntity.Name);
                tileService.OpenGump();
                gumpsOpened++;
            }
        }

        if (gumpsOpened == 0)
        {
            mobile.SendMessage(0x22, $"No spawn data found for tile '{tileName}' at {location}.");
            mobile.SendMessage(0x35, "Use the Editor to create spawn data for this location.");

            // Re-issue target for another attempt
            mobile.Target = new Targets.SpawnControlTarget(mobile, this);
        }
        else
        {
            mobile.SendMessage(0x40, $"Opened {gumpsOpened} spawn editor(s) for this location.");
        }
    }

    internal void TryOpenSpawnEditor(PlayerMobile mobile, StaticTarget decor)
    {
        if (mobile == null || decor == null)
        {
            return;
        }

        Map map = mobile.Map;
        Point3D location = decor.Location;
        string staticName = decor.Name;

        int gumpsOpened = 0;

        // Check for BoxEntity at this location
        BoxEntity box = SpatialGridManager.GetBoxAt(map, location);
        if (box != null)
        {
            string boxName = $"Box_{box.Id}";
            var boxService = new SpawnEditService(mobile, box, CommandTarget.Box, boxName);
            boxService.OpenGump();
            gumpsOpened++;
        }

        // Check for RegionEntity at this location (walk up parent chain)
        RegionEntity regionEntity = FindRegionEntity(map, location);
        if (regionEntity != null)
        {
            var regionService = new SpawnEditService(mobile, regionEntity, CommandTarget.Region, regionEntity.Name);
            regionService.OpenGump();
            gumpsOpened++;
        }

        // Check for TileEntity by static name (statics can also have tile-based spawn)
        if (!string.IsNullOrWhiteSpace(staticName) && SpawnManager.TileLookup.TryGetValue(map, out var tileDict))
        {
            if (tileDict.TryGetValue(staticName, out TileEntity tileEntity))
            {
                var tileService = new SpawnEditService(mobile, tileEntity, CommandTarget.Tile, tileEntity.Name);
                tileService.OpenGump();
                gumpsOpened++;
            }
        }

        if (TryByDecorName(mobile, decor))
        {
            // TODO: Process Success
        }

        if (gumpsOpened == 0)
        {
            mobile.SendMessage(0x22, $"No spawn data found for static '{staticName}' at {location}.");
            mobile.SendMessage(0x35, "Use the Editor to create spawn data for this location.");

            // Re-issue target for another attempt
            mobile.Target = new Targets.SpawnControlTarget(mobile, this);
        }
        else
        {
            mobile.SendMessage(0x40, $"Opened {gumpsOpened} spawn editor(s) for this location.");
        }
    }

    private static bool TryByDecorName(PlayerMobile pm, StaticTarget decor)
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

    internal void TryOpenSpawnEditor(PlayerMobile mobile, Item item)
    {
        if (mobile == null || item == null)
        {
            return;
        }

        Map map = mobile.Map;
        Point3D location = item.Location;
        string itemName = item.Name ?? item.GetType().Name;

        int gumpsOpened = 0;

        // Check for BoxEntity at this location
        BoxEntity box = SpatialGridManager.GetBoxAt(map, location);
        if (box != null)
        {
            string boxName = $"Box_{box.Id}";
            var boxService = new SpawnEditService(mobile, box, CommandTarget.Box, boxName);
            boxService.OpenGump();
            gumpsOpened++;
        }

        // Check for RegionEntity at this location (walk up parent chain)
        RegionEntity regionEntity = FindRegionEntity(map, location);

        if (regionEntity != null)
        {
            var regionService = new SpawnEditService(mobile, regionEntity, CommandTarget.Region, regionEntity.Name);
            regionService.OpenGump();
            gumpsOpened++;
        }

        // Check for TileEntity by item name
        if (!string.IsNullOrWhiteSpace(itemName) && SpawnManager.TileLookup.TryGetValue(map, out var tileDict))
        {
            if (tileDict.TryGetValue(itemName, out TileEntity tileEntity))
            {
                var tileService = new SpawnEditService(mobile, tileEntity, CommandTarget.Tile, tileEntity.Name);
                tileService.OpenGump();
                gumpsOpened++;
            }
        }

        if (TryByItemName(mobile, item))
        {
            // TODO: Process Success
        }

        if (gumpsOpened == 0)
        {
            mobile.SendMessage(0x22, $"No spawn data found for item '{itemName}' at {location}.");
            mobile.SendMessage(0x35, "Use the Editor to create spawn data for this location.");

            // Re-issue target for another attempt
            mobile.Target = new SpawnControlTarget(mobile, this);
        }
        else
        {
            mobile.SendMessage(0x40, $"Opened {gumpsOpened} spawn editor(s) for this location.");
        }
    }

    private static bool TryByItemName(PlayerMobile pm, Item item)
    {
        // Vendors - check if this is a registered shop sign
        if (item is BaseSign bs)
        {
            if (VendorManager.SignLocations.TryGetValue(pm.Map.MapID, out var signList) &&
                signList.Exists(s => s.Item3 == bs.Location))
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
    private static bool TryOpenVendorEditor(PlayerMobile pm, Point3D targetLocation, bool isSign, string displayName)
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
    private static VendorEntity FindVendorEntity(Map map, Point3D targetLocation, bool isSign)
    {
        if (!SpawnManager.VendorSpawns.TryGetValue(map, out var vendorList))
        {
            return null;
        }

        foreach (var vendor in vendorList)
        {
            if (vendor.IsSign != isSign)
            {
                continue;
            }

            // Reverse the offset to get original sign/hive location
            Point2D originalLocation = GetOriginalLocation(vendor);

            if (originalLocation == targetLocation)
            {
                return vendor;
            }
        }

        return null;
    }

    /// <summary>
    /// Reverses the GetInsideLocation calculation to get the original sign/hive world position.
    /// </summary>
    private static Point2D GetOriginalLocation(VendorEntity vendor)
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
    private static RegionEntity FindRegionEntity(Map map, Point3D location)
    {
        if (map == null)
        {
            return null;
        }

        if (!SpawnManager.RegionLookup.TryGetValue(map, out var regionDict))
        {
            return null;
        }

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
            {
                return entity;
            }

            // Fallback: Name-based lookup (handles reference mismatch)
            entity = FindRegionByName(map, region.Name);
            if (entity != null)
            {
                return entity;
            }

            region = region.Parent;
        }

        return null;
    }

    /// <summary>
    /// Finds a RegionEntity by name (case-insensitive).
    /// Used as fallback when reference-based lookup fails.
    /// </summary>
    private static RegionEntity FindRegionByName(Map map, string regionName)
    {
        if (string.IsNullOrEmpty(regionName))
        {
            return null;
        }

        if (!SpawnManager.RegionSpawns.TryGetValue(map, out var regionList))
        {
            return null;
        }

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

}
