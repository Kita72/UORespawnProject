using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Enums;
using UORespawnApp.Scripts.Helpers;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Service for synchronizing spawn pack data with server-generated reference data.
/// 
/// When the server data changes (bestiary, regions, signs, hives), spawn packs may
/// contain invalid references. This service:
/// 
/// 1. Removes creatures from spawns that are no longer in the Bestiary
/// 2. Removes region spawns for regions no longer in RegionList
/// 3. Removes vendor spawns for sign/hive locations no longer in SignData/HiveData
/// 4. Removes vendors from VendorList that are no longer valid
/// 
/// This ensures all packs stay aligned with the server's current data.
/// </summary>
public class SpawnPackSyncService
{
    private int _creaturesRemoved;
    private int _regionsRemoved;
    private int _vendorLocationsRemoved;
    private int _vendorsRemoved;

    /// <summary>
    /// Synchronizes currently loaded spawn data AND backup packs with server reference data.
    /// Call this after server data (bestiary, regions, signs, hives) is loaded.
    /// 
    /// This ensures:
    /// 1. Active pack data is aligned with server
    /// 2. Backup packs are aligned (so Reset to Default works correctly)
    /// </summary>
    public async Task SyncAllPacksAsync()
    {
        Logger.Info("[PackSync] Starting spawn data synchronization with server data...");

        _creaturesRemoved = 0;
        _regionsRemoved = 0;
        _vendorLocationsRemoved = 0;
        _vendorsRemoved = 0;

        try
        {
            await Task.Run(() =>
            {
                // Build lookup sets from server data for fast validation
                var validCreatures = GetValidCreatures();
                var validRegions = GetValidRegions();
                var validVendors = GetValidVendors();
                var validSignLocations = GetValidSignLocations();
                var validHiveLocations = GetValidHiveLocations();

                Logger.Info($"[PackSync] Server data: {validCreatures.Count} creatures, {validRegions.Count} regions, {validVendors.Count} vendors");
                Logger.Info($"[PackSync] Server data: {validSignLocations.Count} sign locations, {validHiveLocations.Count} hive locations");

                // 1. Sync currently loaded data (active pack)
                bool activeModified = SyncLoadedData(validCreatures, validRegions, validVendors, validSignLocations, validHiveLocations);

                if (activeModified)
                {
                    Logger.Info($"[PackSync] Active pack - Removed: {_creaturesRemoved} creatures, {_regionsRemoved} regions, {_vendorLocationsRemoved} vendor locations, {_vendorsRemoved} invalid vendors");
                    SaveSyncedData();
                }
                else
                {
                    Logger.Info("[PackSync] Active pack is aligned with server");
                }

                // 2. Sync ALL pack folders (Approved, Created, Imported)
                // This ensures Reset to Default and switching packs produces aligned data
                SyncAllPackFolders(validCreatures, validRegions, validVendors, validSignLocations, validHiveLocations);
            });
        }
        catch (Exception ex)
        {
            Logger.Error("[PackSync] Error during spawn pack synchronization", ex);
        }
    }

    /// <summary>
    /// Syncs all packs in Approved, Created, and Imported folders.
    /// This ensures any pack the user loads/resets will have aligned data.
    /// Note: Backup folder contains ZIPs (not .bin files) - those are not synced.
    /// </summary>
    private void SyncAllPackFolders(
        HashSet<string> validCreatures,
        HashSet<string> validRegions,
        HashSet<string> validVendors,
        HashSet<string> validSignLocations,
        HashSet<string> validHiveLocations)
    {
        // Sync Approved packs (bundled default packs)
        SyncPackCategory(PathConstants.PacksApprovedPath, "Approved", validCreatures, validRegions, validVendors, validSignLocations, validHiveLocations);

        // Sync Created packs (user-created)
        SyncPackCategory(PathConstants.PacksCreatedPath, "Created", validCreatures, validRegions, validVendors, validSignLocations, validHiveLocations);

        // Sync Imported packs (user-imported)
        SyncPackCategory(PathConstants.PacksImportedPath, "Imported", validCreatures, validRegions, validVendors, validSignLocations, validHiveLocations);
    }

    /// <summary>
    /// Syncs all packs within a specific category folder.
    /// </summary>
    private void SyncPackCategory(
        string categoryPath,
        string categoryName,
        HashSet<string> validCreatures,
        HashSet<string> validRegions,
        HashSet<string> validVendors,
        HashSet<string> validSignLocations,
        HashSet<string> validHiveLocations)
    {
        if (!Directory.Exists(categoryPath))
        {
            return;
        }

        foreach (var packFolder in Directory.GetDirectories(categoryPath))
        {
            var packName = Path.GetFileName(packFolder);

            try
            {
                bool modified = SyncPackFolder(packFolder, validCreatures, validRegions, validVendors, validSignLocations, validHiveLocations);

                if (modified)
                {
                    Logger.Info($"[PackSync] {categoryName} pack '{packName}' synced with server data");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[PackSync] Error syncing {categoryName} pack '{packName}'", ex);
            }
        }
    }

    /// <summary>
    /// Syncs a single pack folder by reading, cleaning, and writing back the spawn files.
    /// </summary>
    private bool SyncPackFolder(
        string packFolder,
        HashSet<string> validCreatures,
        HashSet<string> validRegions,
        HashSet<string> validVendors,
        HashSet<string> validSignLocations,
        HashSet<string> validHiveLocations)
    {
        bool anyModified = false;

        // Sync Box Spawns
        var boxPath = Path.Combine(packFolder, PathConstants.BOX_FILENAME);
        if (File.Exists(boxPath))
        {
            var boxSpawns = ReadBoxSpawnsFromFile(boxPath);
            int removed = RemoveInvalidCreaturesFromBoxSpawns(boxSpawns, validCreatures);
            if (removed > 0)
            {
                WriteBoxSpawnsToFile(boxPath, boxSpawns);
                anyModified = true;
            }
        }

        // Sync Tile Spawns
        var tilePath = Path.Combine(packFolder, PathConstants.TILE_FILENAME);
        if (File.Exists(tilePath))
        {
            var tileSpawns = ReadTileSpawnsFromFile(tilePath);
            int removed = RemoveInvalidCreaturesFromTileSpawns(tileSpawns, validCreatures);
            if (removed > 0)
            {
                WriteTileSpawnsToFile(tilePath, tileSpawns);
                anyModified = true;
            }
        }

        // Sync Region Spawns
        var regionPath = Path.Combine(packFolder, PathConstants.REGION_FILENAME);
        if (File.Exists(regionPath))
        {
            var regionSpawns = ReadRegionSpawnsFromFile(regionPath);
            int creaturesRemoved = RemoveInvalidCreaturesFromRegionSpawns(regionSpawns, validCreatures);
            int regionsRemoved = RemoveInvalidRegions(regionSpawns, validRegions);
            if (creaturesRemoved > 0 || regionsRemoved > 0)
            {
                WriteRegionSpawnsToFile(regionPath, regionSpawns);
                anyModified = true;
            }
        }

        // Sync Vendor Spawns
        var vendorPath = Path.Combine(packFolder, PathConstants.VENDOR_FILENAME);
        if (File.Exists(vendorPath))
        {
            var vendorSpawns = ReadVendorSpawnsFromFile(vendorPath);
            int locsRemoved = RemoveInvalidVendorLocations(vendorSpawns, validSignLocations, validHiveLocations);
            int vendorsRemoved = RemoveInvalidVendorsFromSpawns(vendorSpawns, validVendors);
            if (locsRemoved > 0 || vendorsRemoved > 0)
            {
                WriteVendorSpawnsToFile(vendorPath, vendorSpawns);
                anyModified = true;
            }
        }

        return anyModified;
    }

    /// <summary>
    /// Syncs currently loaded in-memory data.
    /// </summary>
    private bool SyncLoadedData(
        HashSet<string> validCreatures,
        HashSet<string> validRegions,
        HashSet<string> validVendors,
        HashSet<string> validSignLocations,
        HashSet<string> validHiveLocations)
    {
        bool modified = false;

        // Box Spawns - remove invalid creatures
        int boxRemoved = RemoveInvalidCreaturesFromBoxSpawns(Utility.BoxSpawns, validCreatures);
        if (boxRemoved > 0)
        {
            modified = true;
            _creaturesRemoved += boxRemoved;
            Logger.Info($"[PackSync] Removed {boxRemoved} invalid creatures from box spawns");
        }

        // Tile Spawns - remove invalid creatures
        int tileRemoved = RemoveInvalidCreaturesFromTileSpawns(Utility.TileSpawns, validCreatures);
        if (tileRemoved > 0)
        {
            modified = true;
            _creaturesRemoved += tileRemoved;
            Logger.Info($"[PackSync] Removed {tileRemoved} invalid creatures from tile spawns");
        }

        // Region Spawns - remove invalid creatures and regions
        int regionCreaturesRemoved = RemoveInvalidCreaturesFromRegionSpawns(Utility.RegionSpawns, validCreatures);
        int regionsRemoved = RemoveInvalidRegions(Utility.RegionSpawns, validRegions);
        if (regionCreaturesRemoved > 0 || regionsRemoved > 0)
        {
            modified = true;
            _creaturesRemoved += regionCreaturesRemoved;
            _regionsRemoved += regionsRemoved;
            Logger.Info($"[PackSync] Removed {regionCreaturesRemoved} creatures, {regionsRemoved} regions from region spawns");
        }

        // Vendor Spawns - remove invalid locations and vendors
        int vendorLocsRemoved = RemoveInvalidVendorLocations(Utility.VendorSpawns, validSignLocations, validHiveLocations);
        int vendorsRemoved = RemoveInvalidVendorsFromSpawns(Utility.VendorSpawns, validVendors);
        if (vendorLocsRemoved > 0 || vendorsRemoved > 0)
        {
            modified = true;
            _vendorLocationsRemoved += vendorLocsRemoved;
            _vendorsRemoved += vendorsRemoved;
            Logger.Info($"[PackSync] Removed {vendorLocsRemoved} locations, {vendorsRemoved} vendors from vendor spawns");
        }

        return modified;
    }

    /// <summary>
    /// Saves the synced data back to disk.
    /// </summary>
    private void SaveSyncedData()
    {
        try
        {
            BinarySerializationService.SaveBoxSpawns();
            BinarySerializationService.SaveTileSpawns();
            BinarySerializationService.SaveRegionSpawns();
            BinarySerializationService.SaveVendorSpawns();
            Logger.Info("[PackSync] Saved synchronized spawn data to active pack");
        }
        catch (Exception ex)
        {
            Logger.Error("[PackSync] Error saving synchronized data", ex);
        }
    }

    #region Validation Data Builders

    private static HashSet<string> GetValidCreatures()
    {
        var list = BestiaryListUtility.BestiaryNameList ?? [];
        return [.. list.Select(c => c.ToLowerInvariant())];
    }

    private static HashSet<string> GetValidRegions()
    {
        var regions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int mapId = 0; mapId <= 5; mapId++)
        {
            var mapRegions = RegionListUtility.GetRegionsForMap(mapId);
            foreach (var region in mapRegions)
            {
                regions.Add($"{mapId}:{region.Name}");
            }
        }

        return regions;
    }

    private static HashSet<string> GetValidVendors()
    {
        var list = VendorListUtility.VendorNameList ?? [];
        return [.. list.Select(v => v.ToLowerInvariant())];
    }

    private static HashSet<string> GetValidSignLocations()
    {
        var locations = new HashSet<string>();

        for (int mapId = 0; mapId <= 5; mapId++)
        {
            var signs = SignDataUtility.GetSignsForMap(mapId);
            foreach (var sign in signs)
            {
                // Key format: mapId:signType:x:y:z
                locations.Add($"{mapId}:{sign.SignType}:{sign.X}:{sign.Y}:{sign.Z}");
            }
        }

        return locations;
    }

    private static HashSet<string> GetValidHiveLocations()
    {
        var locations = new HashSet<string>();

        for (int mapId = 0; mapId <= 5; mapId++)
        {
            var hives = HiveDataUtility.GetHivesForMap(mapId);
            foreach (var hive in hives)
            {
                // Key format: mapId:x:y:z
                locations.Add($"{mapId}:{hive.X}:{hive.Y}:{hive.Z}");
            }
        }

        return locations;
    }

    #endregion

    #region Creature Removal

    private static int RemoveInvalidCreaturesFromBoxSpawns(Dictionary<int, List<BoxSpawnEntity>> spawns, HashSet<string> validCreatures)
    {
        int removed = 0;

        foreach (var mapEntry in spawns)
        {
            foreach (var box in mapEntry.Value)
            {
                removed += RemoveInvalidFromList(box.WaterSpawns, validCreatures);
                removed += RemoveInvalidFromList(box.WeatherSpawns, validCreatures);
                removed += RemoveInvalidFromList(box.TimedSpawns, validCreatures);
                removed += RemoveInvalidFromList(box.CommonSpawns, validCreatures);
                removed += RemoveInvalidFromList(box.UncommonSpawns, validCreatures);
                removed += RemoveInvalidFromList(box.RareSpawns, validCreatures);
            }
        }

        return removed;
    }

    private static int RemoveInvalidCreaturesFromTileSpawns(Dictionary<int, List<TileSpawnEntity>> spawns, HashSet<string> validCreatures)
    {
        int removed = 0;

        foreach (var mapEntry in spawns)
        {
            foreach (var tile in mapEntry.Value)
            {
                removed += RemoveInvalidFromList(tile.WaterSpawns, validCreatures);
                removed += RemoveInvalidFromList(tile.WeatherSpawns, validCreatures);
                removed += RemoveInvalidFromList(tile.TimedSpawns, validCreatures);
                removed += RemoveInvalidFromList(tile.CommonSpawns, validCreatures);
                removed += RemoveInvalidFromList(tile.UncommonSpawns, validCreatures);
                removed += RemoveInvalidFromList(tile.RareSpawns, validCreatures);
            }
        }

        return removed;
    }

    private static int RemoveInvalidCreaturesFromRegionSpawns(Dictionary<int, List<RegionSpawnEntity>> spawns, HashSet<string> validCreatures)
    {
        int removed = 0;

        foreach (var mapEntry in spawns)
        {
            foreach (var region in mapEntry.Value)
            {
                removed += RemoveInvalidFromList(region.WaterSpawns, validCreatures);
                removed += RemoveInvalidFromList(region.WeatherSpawns, validCreatures);
                removed += RemoveInvalidFromList(region.TimedSpawns, validCreatures);
                removed += RemoveInvalidFromList(region.CommonSpawns, validCreatures);
                removed += RemoveInvalidFromList(region.UncommonSpawns, validCreatures);
                removed += RemoveInvalidFromList(region.RareSpawns, validCreatures);
            }
        }

        return removed;
    }

    private static int RemoveInvalidFromList(List<string> list, HashSet<string> validItems)
    {
        int originalCount = list.Count;
        list.RemoveAll(item => !validItems.Contains(item.ToLowerInvariant()));
        return originalCount - list.Count;
    }

    #endregion

    #region Region Removal

    private static int RemoveInvalidRegions(Dictionary<int, List<RegionSpawnEntity>> spawns, HashSet<string> validRegions)
    {
        int removed = 0;

        foreach (var mapEntry in spawns.ToList())
        {
            int mapId = mapEntry.Key;
            var regionsToRemove = mapEntry.Value
                .Where(r => !validRegions.Contains($"{mapId}:{r.Name}"))
                .ToList();

            foreach (var region in regionsToRemove)
            {
                mapEntry.Value.Remove(region);
                removed++;
            }
        }

        return removed;
    }

    #endregion

    #region Vendor Location Removal

    private static int RemoveInvalidVendorLocations(
        Dictionary<int, List<VendorEntity>> spawns,
        HashSet<string> validSignLocations,
        HashSet<string> validHiveLocations)
    {
        int removed = 0;

        foreach (var mapEntry in spawns.ToList())
        {
            int mapId = mapEntry.Key;
            var entitiesToRemove = new List<VendorEntity>();

            foreach (var vendor in mapEntry.Value)
            {
                string key;
                if (vendor.IsSign)
                {
                    key = $"{mapId}:{vendor.Sign}:{vendor.Location.X}:{vendor.Location.Y}:{vendor.Location.Z}";
                    if (!validSignLocations.Contains(key))
                    {
                        entitiesToRemove.Add(vendor);
                    }
                }
                else
                {
                    key = $"{mapId}:{vendor.Location.X}:{vendor.Location.Y}:{vendor.Location.Z}";
                    if (!validHiveLocations.Contains(key))
                    {
                        entitiesToRemove.Add(vendor);
                    }
                }
            }

            foreach (var entity in entitiesToRemove)
            {
                mapEntry.Value.Remove(entity);
                removed++;
            }
        }

        return removed;
    }

    private static int RemoveInvalidVendorsFromSpawns(Dictionary<int, List<VendorEntity>> spawns, HashSet<string> validVendors)
    {
        int removed = 0;

        foreach (var mapEntry in spawns)
        {
            foreach (var vendor in mapEntry.Value)
            {
                int originalCount = vendor.VendorList.Count;
                vendor.VendorList.RemoveAll(v => !validVendors.Contains(v.ToLowerInvariant()));
                removed += originalCount - vendor.VendorList.Count;
            }
        }

        return removed;
    }

    #endregion

    #region Pack File I/O

    // Box Spawn File I/O
    private static Dictionary<int, List<BoxSpawnEntity>> ReadBoxSpawnsFromFile(string filePath)
    {
        var spawns = new Dictionary<int, List<BoxSpawnEntity>>();
        for (int i = 0; i <= 5; i++) spawns[i] = [];

        using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));
        int version = reader.ReadInt32();
        string fileVersion = reader.ReadString();
        int mapCount = reader.ReadInt32();

        for (int m = 0; m < mapCount; m++)
        {
            int mapId = reader.ReadInt32();
            string mapName = reader.ReadString();
            int boxCount = reader.ReadInt32();

            if (!spawns.ContainsKey(mapId)) spawns[mapId] = [];

            for (int b = 0; b < boxCount; b++)
            {
                var box = new BoxSpawnEntity
                {
                    Position = reader.ReadInt32(),
                    Priority = reader.ReadInt32(),
                    MapId = reader.ReadInt32()
                };

                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                box.SpawnBox = new Rect(x, y, width, height);

                box.WeatherSpawn = (WeatherTypes)reader.ReadInt32();
                box.TimedSpawn = (TimeNames)reader.ReadInt32();

                box.WaterSpawns = ReadStringList(reader);
                box.WeatherSpawns = ReadStringList(reader);
                box.TimedSpawns = ReadStringList(reader);
                box.CommonSpawns = ReadStringList(reader);
                box.UncommonSpawns = ReadStringList(reader);
                box.RareSpawns = ReadStringList(reader);

                spawns[mapId].Add(box);
            }
        }

        return spawns;
    }

    private static void WriteBoxSpawnsToFile(string filePath, Dictionary<int, List<BoxSpawnEntity>> spawns)
    {
        using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
        writer.Write(1); // version
        writer.Write("1.0");

        var nonEmptyMaps = spawns.Where(kvp => kvp.Value.Count > 0).OrderBy(kvp => kvp.Key).ToList();
        writer.Write(nonEmptyMaps.Count);

        foreach (var mapEntry in nonEmptyMaps)
        {
            writer.Write(mapEntry.Key);
            writer.Write(MapUtility.GetMapName(mapEntry.Key));
            writer.Write(mapEntry.Value.Count);

            foreach (var box in mapEntry.Value)
            {
                writer.Write(box.Position);
                writer.Write(box.Priority);
                writer.Write(box.MapId);
                writer.Write((int)box.SpawnBox.X);
                writer.Write((int)box.SpawnBox.Y);
                writer.Write((int)box.SpawnBox.Width);
                writer.Write((int)box.SpawnBox.Height);
                writer.Write((int)box.WeatherSpawn);
                writer.Write((int)box.TimedSpawn);

                WriteStringList(writer, box.WaterSpawns);
                WriteStringList(writer, box.WeatherSpawns);
                WriteStringList(writer, box.TimedSpawns);
                WriteStringList(writer, box.CommonSpawns);
                WriteStringList(writer, box.UncommonSpawns);
                WriteStringList(writer, box.RareSpawns);
            }
        }
    }

    // Tile Spawn File I/O
    private static Dictionary<int, List<TileSpawnEntity>> ReadTileSpawnsFromFile(string filePath)
    {
        var spawns = new Dictionary<int, List<TileSpawnEntity>>();
        for (int i = 0; i <= 5; i++) spawns[i] = [];

        using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));
        int version = reader.ReadInt32();
        string fileVersion = reader.ReadString();
        int mapCount = reader.ReadInt32();

        for (int m = 0; m < mapCount; m++)
        {
            int mapId = reader.ReadInt32();
            string mapName = reader.ReadString();
            int tileCount = reader.ReadInt32();

            if (!spawns.ContainsKey(mapId)) spawns[mapId] = [];

            for (int t = 0; t < tileCount; t++)
            {
                var tile = new TileSpawnEntity
                {
                    Id = reader.ReadInt32(),
                    Name = reader.ReadString(),
                    MapId = reader.ReadInt32(),
                    WeatherSpawn = (WeatherTypes)reader.ReadInt32(),
                    TimedSpawn = (TimeNames)reader.ReadInt32(),
                    WaterSpawns = ReadStringList(reader),
                    WeatherSpawns = ReadStringList(reader),
                    TimedSpawns = ReadStringList(reader),
                    CommonSpawns = ReadStringList(reader),
                    UncommonSpawns = ReadStringList(reader),
                    RareSpawns = ReadStringList(reader)
                };

                spawns[mapId].Add(tile);
            }
        }

        return spawns;
    }

    private static void WriteTileSpawnsToFile(string filePath, Dictionary<int, List<TileSpawnEntity>> spawns)
    {
        using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
        writer.Write(1); // version
        writer.Write("1.0");

        var nonEmptyMaps = spawns.Where(kvp => kvp.Value.Count > 0).OrderBy(kvp => kvp.Key).ToList();
        writer.Write(nonEmptyMaps.Count);

        foreach (var mapEntry in nonEmptyMaps)
        {
            writer.Write(mapEntry.Key);
            writer.Write(MapUtility.GetMapName(mapEntry.Key));
            writer.Write(mapEntry.Value.Count);

            foreach (var tile in mapEntry.Value)
            {
                writer.Write(tile.Id);
                writer.Write(tile.Name ?? "");
                writer.Write(tile.MapId);
                writer.Write((int)tile.WeatherSpawn);
                writer.Write((int)tile.TimedSpawn);

                WriteStringList(writer, tile.WaterSpawns);
                WriteStringList(writer, tile.WeatherSpawns);
                WriteStringList(writer, tile.TimedSpawns);
                WriteStringList(writer, tile.CommonSpawns);
                WriteStringList(writer, tile.UncommonSpawns);
                WriteStringList(writer, tile.RareSpawns);
            }
        }
    }

    // Region Spawn File I/O
    private static Dictionary<int, List<RegionSpawnEntity>> ReadRegionSpawnsFromFile(string filePath)
    {
        var spawns = new Dictionary<int, List<RegionSpawnEntity>>();
        for (int i = 0; i <= 5; i++) spawns[i] = [];

        using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));
        int version = reader.ReadInt32();
        string fileVersion = reader.ReadString();
        int mapCount = reader.ReadInt32();

        for (int m = 0; m < mapCount; m++)
        {
            int mapId = reader.ReadInt32();
            string mapName = reader.ReadString();
            int regionCount = reader.ReadInt32();

            if (!spawns.ContainsKey(mapId)) spawns[mapId] = [];

            for (int r = 0; r < regionCount; r++)
            {
                var region = new RegionSpawnEntity
                {
                    Id = reader.ReadInt32(),
                    Name = reader.ReadString(),
                    MapId = reader.ReadInt32(),
                    WeatherSpawn = (WeatherTypes)reader.ReadInt32(),
                    TimedSpawn = (TimeNames)reader.ReadInt32(),
                    WaterSpawns = ReadStringList(reader),
                    WeatherSpawns = ReadStringList(reader),
                    TimedSpawns = ReadStringList(reader),
                    CommonSpawns = ReadStringList(reader),
                    UncommonSpawns = ReadStringList(reader),
                    RareSpawns = ReadStringList(reader)
                };

                spawns[mapId].Add(region);
            }
        }

        return spawns;
    }

    private static void WriteRegionSpawnsToFile(string filePath, Dictionary<int, List<RegionSpawnEntity>> spawns)
    {
        using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
        writer.Write(1); // version
        writer.Write("1.0");

        var nonEmptyMaps = spawns.Where(kvp => kvp.Value.Count > 0).OrderBy(kvp => kvp.Key).ToList();
        writer.Write(nonEmptyMaps.Count);

        foreach (var mapEntry in nonEmptyMaps)
        {
            writer.Write(mapEntry.Key);
            writer.Write(MapUtility.GetMapName(mapEntry.Key));
            writer.Write(mapEntry.Value.Count);

            foreach (var region in mapEntry.Value)
            {
                writer.Write(region.Id);
                writer.Write(region.Name ?? "");
                writer.Write(region.MapId);
                writer.Write((int)region.WeatherSpawn);
                writer.Write((int)region.TimedSpawn);

                WriteStringList(writer, region.WaterSpawns);
                WriteStringList(writer, region.WeatherSpawns);
                WriteStringList(writer, region.TimedSpawns);
                WriteStringList(writer, region.CommonSpawns);
                WriteStringList(writer, region.UncommonSpawns);
                WriteStringList(writer, region.RareSpawns);
            }
        }
    }

    // Vendor Spawn File I/O
    private static Dictionary<int, List<VendorEntity>> ReadVendorSpawnsFromFile(string filePath)
    {
        var spawns = new Dictionary<int, List<VendorEntity>>();
        for (int i = 0; i <= 5; i++) spawns[i] = [];

        using var reader = new BinaryReader(File.Open(filePath, FileMode.Open));
        int version = reader.ReadInt32();
        string fileVersion = reader.ReadString();
        int mapCount = reader.ReadInt32();

        for (int m = 0; m < mapCount; m++)
        {
            int mapId = reader.ReadInt32();
            string mapName = reader.ReadString();
            int vendorCount = reader.ReadInt32();

            if (!spawns.ContainsKey(mapId)) spawns[mapId] = [];

            for (int v = 0; v < vendorCount; v++)
            {
                var isSign = reader.ReadBoolean();
                var signType = (SignTypes)reader.ReadInt32();
                var facing = (FacingTypes)reader.ReadInt32();

                var x = reader.ReadInt32();
                var y = reader.ReadInt32();
                var z = reader.ReadInt32();
                var location = new Point3D { X = x, Y = y, Z = z };

                var vendorList = ReadStringList(reader);

                VendorEntity vendor;
                if (isSign)
                {
                    vendor = new VendorEntity(signType, facing, location);
                }
                else
                {
                    vendor = new VendorEntity(location);
                }

                vendor.MapId = mapId;
                vendor.SetVendors(vendorList);

                spawns[mapId].Add(vendor);
            }
        }

        return spawns;
    }

    private static void WriteVendorSpawnsToFile(string filePath, Dictionary<int, List<VendorEntity>> spawns)
    {
        using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
        writer.Write(1); // version
        writer.Write("1.0");

        var nonEmptyMaps = spawns.Where(kvp => kvp.Value.Count > 0).OrderBy(kvp => kvp.Key).ToList();
        writer.Write(nonEmptyMaps.Count);

        foreach (var mapEntry in nonEmptyMaps)
        {
            writer.Write(mapEntry.Key);
            writer.Write(MapUtility.GetMapName(mapEntry.Key));
            writer.Write(mapEntry.Value.Count);

            foreach (var vendor in mapEntry.Value)
            {
                writer.Write(vendor.IsSign);
                writer.Write((int)vendor.Sign);
                writer.Write((int)vendor.Facing);
                writer.Write(vendor.Location.X);
                writer.Write(vendor.Location.Y);
                writer.Write(vendor.Location.Z);
                WriteStringList(writer, vendor.VendorList);
            }
        }
    }

    // Shared helpers
    private static List<string> ReadStringList(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        var list = new List<string>(count);

        for (int i = 0; i < count; i++)
        {
            list.Add(reader.ReadString());
        }

        return list;
    }

    private static void WriteStringList(BinaryWriter writer, List<string> list)
    {
        writer.Write(list?.Count ?? 0);

        if (list != null)
        {
            foreach (var item in list)
            {
                writer.Write(item ?? "");
            }
        }
    }

    #endregion
}
