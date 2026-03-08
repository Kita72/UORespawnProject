using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Engines.Spawners;

using Server.Custom.UORespawnServer.Mobiles;
using Server.Custom.UORespawnServer.Helpers;
using Server.Custom.UORespawnServer.Enums;

namespace Server.Custom.UORespawnServer.Managers;
// Manages our Game Data : Lists of Required Data for Editor!
internal static class GameManager
{
    // Cache of valid creature names from bestiary (case-insensitive for matching)
    private static readonly HashSet<string> _BestiaryCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Total count of valid creature types in bestiary.
    /// Used for dynamic MAX_RECYCLE_TOTAL calculation.
    /// </summary>
    internal static int BestiaryCount { get; private set; } = 0;

    internal static void InitializeData()
    {
        UOR_Utility.SendMsg(ConsoleColor.Green, "DATA-[Initializing...");

        GenMapList();
        GenBestiaryList();
        GenRegionList();
        GenTileList();
        GenSpawnerList();
        GenVendorData();

        UOR_Utility.SendMsg(ConsoleColor.Green, "DATA-[Initialized]");
    }

    /// <summary>
    /// Checks for and applies any pending edit commands.
    /// Called after normal spawn data load to apply server-side edits that haven't been consumed by editor.
    /// </summary>
    internal static void CheckAndApplyPendingCommands()
    {
        if (!CommandManager.HasAnyPendingCommands())
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, "COMMANDS-[No pending commands]");
            return;
        }

        UOR_Utility.SendMsg(ConsoleColor.Cyan, "COMMANDS-[Checking pending commands...]");

        int totalApplied = 0;
        int totalFailed = 0;

        // Process settings commands
        if (CommandManager.HasPendingCommands(CommandTarget.Settings))
        {
            var (applied, failed) = ApplySettingsCommands();
            totalApplied += applied;
            totalFailed += failed;
        }

        // Process spawn commands (Box, Region, Tile, Vendor)
        CommandTarget[] spawnTargets = [CommandTarget.Box, CommandTarget.Region, CommandTarget.Tile, CommandTarget.Vendor];

        foreach (var target in spawnTargets)
        {
            if (CommandManager.HasPendingCommands(target))
            {
                var (applied, failed) = ApplySpawnCommands(target);
                totalApplied += applied;
                totalFailed += failed;
            }
        }

        if (totalApplied > 0 || totalFailed > 0)
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, $"COMMANDS-[Applied: {totalApplied}, Failed: {totalFailed}]");
        }
    }

    /// <summary>
    /// Applies pending settings commands.
    /// </summary>
    private static (int applied, int failed) ApplySettingsCommands()
    {
        int applied = 0;
        int failed = 0;

        var commands = CommandManager.ProcessAndConsumeCommands(CommandTarget.Settings);

        foreach (var command in commands)
        {
            if (!CommandManager.ValidateCommand(command))
            {
                failed++;
                continue;
            }

            // Settings commands use SpawnName as key, ExtraData as value
            if (UOR_Settings.ApplySettingCommand(command.SpawnName, command.ExtraData))
            {
                applied++;
                UOR_Utility.SendMsg(ConsoleColor.Cyan, $"SETTING APPLIED-[{command.SpawnName}={command.ExtraData}]");
            }
            else
            {
                failed++;
            }
        }

        return (applied, failed);
    }

    /// <summary>
    /// Applies pending spawn commands for a specific target type.
    /// </summary>
    private static (int applied, int failed) ApplySpawnCommands(CommandTarget target)
    {
        int applied = 0;
        int failed = 0;

        var commands = CommandManager.ProcessAndConsumeCommands(target);

        foreach (var command in commands)
        {
            if (!CommandManager.ValidateCommand(command))
            {
                failed++;
                continue;
            }

            if (SpawnManager.ApplySpawnCommand(command))
            {
                applied++;
                UOR_Utility.SendMsg(ConsoleColor.Cyan, $"SPAWN APPLIED-[{command.Action} {command.SpawnName} to {target}/{command.Section}]");
            }
            else
            {
                failed++;
            }
        }

        return (applied, failed);
    }

    private static void GenMapList()
    {
        List<string> mapList = [];

        for (int i = 0; i < Map.Maps.Length; i++)
        {
            if (Map.Maps[i] != null && Map.Maps[i] != Map.Internal)
            {
                if (!mapList.Contains($"{(Map.Maps[i].MapID, Map.Maps[i].Name)}"))
                {
                    mapList.Add($"{(Map.Maps[i].MapID, Map.Maps[i].Name)}");
                }
            }
        }

        if (mapList.Count > 0)
        {
            File.WriteAllLines(UOR_DIR.MAP_LIST_FILE, mapList);

            UOR_Utility.SendMsg(ConsoleColor.Green, "MAPS-[Generated]");
        }
        else
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, "MAPS-[Empty]");
        }
    }

    private static void GenBestiaryList()
    {
        try
        {
            // Clear any existing cache
            _BestiaryCache.Clear();

            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            List<string> types = [..allTypes.Where(t => IsValidSpawn(t)).Select(t => t.Name)];

            types.Sort();

            if (types.Count > 0)
            {
                // Populate cache for cross-referencing during spawner list generation
                foreach (var type in types)
                {
                    _BestiaryCache.Add(type);
                }

                // Store count for limit calculations before cache is cleared
                BestiaryCount = _BestiaryCache.Count;

                File.WriteAllLines(UOR_DIR.BESTIARY_LIST_FILE, types);
            }

            UOR_Utility.SendMsg(ConsoleColor.Green, $"BESTIARY-[Generated: {BestiaryCount} creatures]");
        }
        catch (Exception ex)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"BESTIARY: {ex.Message}");

            return;
        }
    }

    private static bool IsValidSpawn(Type t)
    {
        if (t.Name == nameof(RiftMob) || t.Name == nameof(PlaceHolder)) { return false; }

        if (t.Name.EndsWith("EffectNPC") || t.Name == nameof(AmbushNPC)) { return true; }

        if (t.Name == "GameMaster" || t.Name.StartsWith("Summoned")) { return false; }

        if (t.Name.StartsWith("Base")) { return false; }

        if (t.IsClass)
        {
            if (IsValidBase(t))
            {
                if (t.GetConstructors().Any(c => c.GetParameters().Length == 0))
                {
                    return true;
                }
                else
                {
                    if (UOR_Settings.ENABLE_DEBUG)
                    {
                        UOR_Utility.SendMsg(ConsoleColor.Yellow, $"BESTIARY EXCLUDED-[{t.Name}]");
                    }
                }
            }
        }

        return false;
    }

    private static bool IsValidBase(Type type)
    {
        if (type.BaseType == typeof(BaseCreature)){ return true; }
        if (type.BaseType == typeof(BaseMount)){ return true; }
        if (type.IsAbstract){ return false; }

        return false;
    }

    private static void GenRegionList()
    {
        List<string> regionList = [];

        try
        {
            string name;
            Map map;
            Rectangle3D[] area;

            string[] areaParts;
            string location;

            int lastMap = 0;

            for (int i = 0; i < Region.Regions.Count; i++)
            {
                name = Region.Regions[i].Name;
                map = Region.Regions[i].Map;
                area = Region.Regions[i].Area;

                // Clean off bad regions from end of regions.xml
                if (map.MapID != lastMap)
                {
                    if (map.MapID > lastMap){ lastMap++; }

                    if (map.MapID < lastMap){ continue; }
                }

                if (!string.IsNullOrEmpty(name) && UOR_Utility.IsValidRegion(name))
                {
                    for (int j = 0; j < area.Length; j++)
                    {
                        areaParts = area[j].ToString().Split(',');

                        location = $"{areaParts[0]},{areaParts[1].TrimStart()},{area[j].Width},{area[j].Height})";

                        regionList.Add($"{map.MapID}:{name}:{location}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"REGIONS: {ex.Message}");
        }

        if (regionList.Count > 0)
        {
            File.WriteAllLines(UOR_DIR.REGIONS_LIST_FILE, regionList);

            UOR_Utility.SendMsg(ConsoleColor.Green, "REGIONS-[Generated]");
        }
        else
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, "REGIONS-[Empty]");
        }
    }

    private static void GenTileList()
    {
        List<string> tileNames = [];

        for (int i = 0; i < TileData.MaxLandValue; i++)
        {
            var name = TileHelper.GetTileName(i, Map.Felucca, Point3D.Zero);

            if (!tileNames.Contains(name) && IsValidTile(name))
            {
                tileNames.Add(name);
            }
        }

        if (tileNames.Count > 0)
        {
            tileNames.Sort();

            File.WriteAllLines(UOR_DIR.TILE_LIST_FILE, tileNames);

            UOR_Utility.SendMsg(ConsoleColor.Green, $"TILES-[Generated]");
        }
        else
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, "TILES-[Empty]");
        }
    }

    private static bool IsValidTile(string name)
    {
        return !TileHelper.InvalidTileNames.Contains(name);
    }

    private static void GenSpawnerList()
    {
        List<string> allSpawners = [];
        int filtered = 0;

        foreach (Item item in World.Items.Values)
        {
            if (item is Spawner s)
            {
                // Skip internal map
                if (s.Map == null || s.Map == Map.Internal)
                {
                    filtered++;
                    continue;
                }

                // Get spawn names from the spawner's spawn objects
                string spawnNames = string.Empty;

                if (s.Entries != null && s.Entries.Count > 0)
                {
                    spawnNames = string.Join("|", s.Entries.Select(e => e.SpawnedName));
                }

                // Clean spawn names and validate creatures exist in bestiary
                string cleanedNames = CleanAndValidateSpawnNames(spawnNames);

                if (cleanedNames == null)
                {
                    filtered++;
                    continue;
                }

                // Format: Serial:MapId:X:Y:HomeRange:Count:SpawnNames
                allSpawners.Add($"{s.Serial.Value}:{s.Map.MapID}:{s.X}:{s.Y}:{s.HomeRange}:{s.Count}:{cleanedNames}");
            }
        }

        if (allSpawners.Count > 0)
        {
            File.WriteAllLines(UOR_DIR.SPAWNERS_LIST_FILE, allSpawners);

            UOR_Utility.SendMsg(ConsoleColor.Green, $"SPAWNERS-[Generated: {allSpawners.Count} valid, {filtered} filtered]");
        }
        else
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, $"SPAWNERS-[Empty, {filtered} filtered]");
        }

        _BestiaryCache.Clear();
    }

    /// <summary>
    /// Cleans and validates spawn names, extracting pure creature names from XmlSpawner syntax.
    /// Returns cleaned pipe-separated names if ALL creatures are valid, null otherwise.
    /// </summary>
    /// <param name="spawnNames">Pipe-separated spawn names (may contain XmlSpawner syntax)</param>
    /// <returns>Cleaned spawn names string, or null if any creature is invalid</returns>
    private static string CleanAndValidateSpawnNames(string spawnNames)
    {
        if (string.IsNullOrWhiteSpace(spawnNames))
        {
            return null;
        }

        var lookup = _BestiaryCache.GetAlternateLookup<ReadOnlySpan<char>>();
        var span = spawnNames.AsSpan();
        StringBuilder sb = null;

        foreach (Range range in span.Split('|'))
        {
            var creature = span[range].Trim();

            if (creature.IsEmpty)
            {
                continue;
            }

            var cleanName = ExtractCleanTypeName(creature);

            if (cleanName.IsEmpty)
            {
                continue;
            }

            if (!lookup.Contains(cleanName))
            {
                return null; // Invalid creature found, reject entire entry
            }

            sb ??= new StringBuilder();

            if (sb.Length > 0)
            {
                sb.Append('|');
            }

            sb.Append(cleanName);
        }

        return sb?.Length > 0 ? sb.ToString() : null;
    }

    /// <summary>
    /// Extracts a clean creature type name from XmlSpawner syntax.
    /// Handles patterns like:
    /// - "Orc" -> "Orc"
    /// - "tribewarrior,Kurak" -> "tribewarrior"
    /// - "TribeWarrior,Barako/Z/100" -> "TribeWarrior"
    /// - "GargoyleDestroyer, /blessed/true/..." -> "GargoyleDestroyer"
    /// - "MyrmidexQueen/Cantwalk/true" -> "MyrmidexQueen"
    /// </summary>
    private static ReadOnlySpan<char> ExtractCleanTypeName(ReadOnlySpan<char> rawName)
    {
        if (rawName.IsWhiteSpace())
        {
            return [];
        }

        var name = rawName.Trim();

        // Handle comma first (e.g., "tribewarrior,Kurak" or "GargoyleDestroyer, /blessed")
        int commaIndex = name.IndexOf(',');
        if (commaIndex > 0)
        {
            name = name[..commaIndex].Trim();
        }

        // Handle slash (e.g., "MyrmidexQueen/Cantwalk/true" or "TribeWarrior/Z/100")
        int slashIndex = name.IndexOf('/');
        if (slashIndex > 0)
        {
            name = name[..slashIndex].Trim();
        }

        return name;
    }

    private static void GenVendorData()
    {
        VendorManager.VendorDataInitialize();
    }
}
