using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Enums;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Service for managing server edit commands.
/// Commands flow: Server COMMANDS/ → Editor local folder → Apply to pack → Delete files
/// 
/// Key Responsibilities:
/// - Detect pending command files in local folder
/// - Parse and validate commands
/// - Apply commands to loaded spawn pack
/// - Delete command files after successful application
/// 
/// User Flow:
/// 1. Editor detects pending commands on launch or via DataWatcher
/// 2. Modal shows user what commands are pending
/// 3. User chooses which pack to apply commands to (or loads one first)
/// 4. Commands applied to pack data in memory
/// 5. Pack saved with updated data
/// 6. Command files deleted
/// </summary>
public class CommandService
{
    /// <summary>
    /// Event raised when pending commands are detected or changed
    /// </summary>
    public event Action? OnPendingCommandsChanged;

    /// <summary>
    /// Cached list of pending commands (refreshed via CheckForPendingCommands)
    /// </summary>
    private List<EditCommand> _pendingCommands = [];

    /// <summary>
    /// Cached list of pending command file paths (for deletion after apply)
    /// </summary>
    private List<string> _pendingCommandFiles = [];

    /// <summary>
    /// Get the current pending commands (call CheckForPendingCommands first to refresh)
    /// </summary>
    public IReadOnlyList<EditCommand> PendingCommands => _pendingCommands.AsReadOnly();

    /// <summary>
    /// Get count of pending commands
    /// </summary>
    public int PendingCount => _pendingCommands.Count;

    /// <summary>
    /// Whether there are any pending commands
    /// </summary>
    public bool HasPendingCommands => _pendingCommands.Count > 0;

    /// <summary>
    /// Check for pending command files in the local data folder.
    /// Parses all found files and caches the commands.
    /// </summary>
    /// <returns>Number of pending commands found</returns>
    public int CheckForPendingCommands()
    {
        _pendingCommands.Clear();
        _pendingCommandFiles.Clear();

        var localPath = PathConstants.LocalDataPath;

        foreach (var filename in PathConstants.GetAllCommandEditFileNames())
        {
            var filePath = Path.Combine(localPath, filename);

            if (File.Exists(filePath))
            {
                _pendingCommandFiles.Add(filePath);

                try
                {
                    var lines = File.ReadAllLines(filePath);

                    foreach (var line in lines)
                    {
                        var command = EditCommand.Parse(line);
                        if (command != null)
                        {
                            _pendingCommands.Add(command);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to read command file {filename}: {ex.Message}");
                }
            }
        }

        if (_pendingCommands.Count > 0)
        {
            Logger.Info($"Found {_pendingCommands.Count} pending commands in {_pendingCommandFiles.Count} files");
            OnPendingCommandsChanged?.Invoke();
        }

        return _pendingCommands.Count;
    }

    /// <summary>
    /// Copy command files from server COMMANDS folder to local folder.
    /// Called by DataWatcher when server is linked.
    /// </summary>
    /// <returns>Number of files copied</returns>
    public int SyncCommandsFromServer()
    {
        var serverPath = PathConstants.ServerCommandsPath;
        if (serverPath == null)
        {
            Logger.Info("Server COMMANDS path not available");
            return 0;
        }

        var localPath = PathConstants.LocalDataPath;
        int filesCopied = 0;

        foreach (var filename in PathConstants.GetAllCommandEditFileNames())
        {
            var serverFile = Path.Combine(serverPath, filename);
            var localFile = Path.Combine(localPath, filename);

            if (File.Exists(serverFile))
            {
                try
                {
                    // Append to local file if it exists, otherwise copy
                    if (File.Exists(localFile))
                    {
                        var serverLines = File.ReadAllLines(serverFile);
                        File.AppendAllLines(localFile, serverLines);
                    }
                    else
                    {
                        File.Copy(serverFile, localFile);
                    }

                    // Delete server file after successful copy
                    File.Delete(serverFile);
                    filesCopied++;

                    Logger.Info($"Synced command file: {filename}");
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to sync command file {filename}: {ex.Message}");
                }
            }
        }

        if (filesCopied > 0)
        {
            CheckForPendingCommands();
        }

        return filesCopied;
    }

    /// <summary>
    /// Get pending commands filtered by target type
    /// </summary>
    public IEnumerable<EditCommand> GetCommandsByTarget(CommandTarget target)
    {
        return _pendingCommands.Where(c => c.Target == target);
    }

    /// <summary>
    /// Get a summary of pending commands grouped by target
    /// </summary>
    public Dictionary<CommandTarget, int> GetCommandSummary()
    {
        return _pendingCommands
            .GroupBy(c => c.Target)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Apply a settings command to the Settings static class.
    /// </summary>
    /// <param name="command">The settings command to apply</param>
    /// <returns>True if applied successfully</returns>
    public bool ApplySettingsCommand(EditCommand command)
    {
        if (command.Target != CommandTarget.Settings)
            return false;

        var settingName = command.SpawnName.ToUpperInvariant();
        var value = command.ExtraData;

        try
        {
            switch (settingName)
            {
                // Scale modifier
                case "SCALE_MOD":
                    if (double.TryParse(value, out var scaleMod))
                        Settings.ScaleMod = scaleMod;
                    break;

                // System intervals
                case "SEARCH_INTERVAL":
                    if (int.TryParse(value, out var searchInterval))
                        Settings.SearchInterval = searchInterval;
                    break;
                case "PROCESS_INTERVAL":
                    if (int.TryParse(value, out var processInterval))
                        Settings.ProcessInterval = processInterval;
                    break;
                case "VALIDATE_INTERVAL":
                    if (int.TryParse(value, out var validateInterval))
                        Settings.ValidateInterval = validateInterval;
                    break;
                case "TIMED_INTERVAL":
                    if (int.TryParse(value, out var timedInterval))
                        Settings.TimedInterval = timedInterval;
                    break;

                // System limits
                case "MAX_RECYCLE_TYPE":
                    if (int.TryParse(value, out var maxRecycleType))
                        Settings.MaxRecycleType = maxRecycleType;
                    break;
                case "MAX_SPAWN_CHECKS":
                    if (int.TryParse(value, out var maxSpawnChecks))
                        Settings.MaxSpawnChecks = maxSpawnChecks;
                    break;
                case "MAX_QUEUE_SIZE":
                    if (int.TryParse(value, out var maxQueueSize))
                        Settings.MaxQueueSize = maxQueueSize;
                    break;
                case "MAX_STAT_SIZE":
                    if (int.TryParse(value, out var maxStatSize))
                        Settings.MaxStatSize = maxStatSize;
                    break;

                // Spawn limits
                case "MAX_SPAWN":
                    if (int.TryParse(value, out var maxSpawn))
                        Settings.MaxMobs = maxSpawn;
                    break;
                case "MIN_RANGE":
                    if (int.TryParse(value, out var minRange))
                        Settings.MinRange = minRange;
                    break;
                case "MAX_RANGE":
                    if (int.TryParse(value, out var maxRange))
                        Settings.MaxRange = maxRange;
                    break;
                case "MAX_CROWD":
                    if (int.TryParse(value, out var maxCrowd))
                        Settings.MaxCrowd = maxCrowd;
                    break;

                // Spawn chances
                case "CHANCE_WATER":
                    if (double.TryParse(value, out var chanceWater))
                        Settings.WaterChance = chanceWater;
                    break;
                case "CHANCE_WEATHER":
                    if (double.TryParse(value, out var chanceWeather))
                        Settings.WeatherChance = chanceWeather;
                    break;
                case "CHANCE_TIMED":
                    if (double.TryParse(value, out var chanceTimed))
                        Settings.TimedChance = chanceTimed;
                    break;
                case "CHANCE_COMMON":
                    if (double.TryParse(value, out var chanceCommon))
                        Settings.CommonChance = chanceCommon;
                    break;
                case "CHANCE_UNCOMMON":
                    if (double.TryParse(value, out var chanceUncommon))
                        Settings.UnCommonChance = chanceUncommon;
                    break;
                case "CHANCE_RARE":
                    if (double.TryParse(value, out var chanceRare))
                        Settings.RareChance = chanceRare;
                    break;

                // Spawn toggles
                case "ENABLE_SCALE_SPAWN":
                    if (bool.TryParse(value, out var enableScale))
                        Settings.IsScaleSpawn = enableScale;
                    break;
                case "ENABLE_RIFT_SPAWN":
                    if (bool.TryParse(value, out var enableRift))
                        Settings.EnableRiftSpawn = enableRift;
                    break;
                case "ENABLE_TOWN_SPAWN":
                    if (bool.TryParse(value, out var enableTown))
                        Settings.EnableTownSpawn = enableTown;
                    break;
                case "ENABLE_GRAVE_SPAWN":
                    if (bool.TryParse(value, out var enableGrave))
                        Settings.EnableGraveSpawn = enableGrave;
                    break;

                // Vendor toggles
                case "ENABLE_VENDOR_SPAWN":
                    if (bool.TryParse(value, out var enableVendor))
                        Settings.EnableVendorSpawn = enableVendor;
                    break;
                case "ENABLE_VENDOR_NIGHT":
                    if (bool.TryParse(value, out var enableVendorNight))
                        Settings.EnableVendorNight = enableVendorNight;
                    break;
                case "ENABLE_VENDOR_EXTRA":
                    if (bool.TryParse(value, out var enableVendorExtra))
                        Settings.EnableVendorExtra = enableVendorExtra;
                    break;

                // Effects and debug
                case "ENABLE_SPAWN_EFFECTS":
                    if (bool.TryParse(value, out var enableEffects))
                        Settings.EnableSpawnEffects = enableEffects;
                    break;
                case "ENABLE_DEBUG":
                    if (bool.TryParse(value, out var enableDebug))
                        Settings.EnableDebugSpawn = enableDebug;
                    break;

                default:
                    Logger.Warning($"Unknown settings key: {settingName}");
                    return false;
            }

            Logger.Info($"Applied settings command: {settingName} = {value}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to apply settings command {settingName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Apply all pending settings commands.
    /// </summary>
    /// <returns>Number of commands applied successfully</returns>
    public int ApplyAllSettingsCommands()
    {
        var settingsCommands = GetCommandsByTarget(CommandTarget.Settings).ToList();
        int applied = 0;

        foreach (var command in settingsCommands)
        {
            if (ApplySettingsCommand(command))
                applied++;
        }

        Logger.Info($"Applied {applied}/{settingsCommands.Count} settings commands");
        return applied;
    }

    /// <summary>
    /// Delete all pending command files after successful application.
    /// Called after commands have been applied to a pack and saved.
    /// </summary>
    public void DeleteProcessedCommandFiles()
    {
        foreach (var filePath in _pendingCommandFiles)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Logger.Info($"Deleted processed command file: {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to delete command file {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        _pendingCommands.Clear();
        _pendingCommandFiles.Clear();

        OnPendingCommandsChanged?.Invoke();
        Logger.Info("Cleared all processed command files");
    }

    /// <summary>
    /// Clear cached commands without deleting files.
    /// Used when user dismisses the modal without applying.
    /// </summary>
    public void ClearCache()
    {
        _pendingCommands.Clear();
        _pendingCommandFiles.Clear();
    }

    #region Spawn Command Application

    /// <summary>
    /// Apply all pending spawn commands (Box, Region, Tile, Vendor).
    /// Must be called after spawn data is loaded into Utility dictionaries.
    /// </summary>
    /// <returns>Number of commands applied successfully</returns>
    public int ApplyAllSpawnCommands()
    {
        int applied = 0;

        foreach (var command in _pendingCommands)
        {
            bool success = command.Target switch
            {
                CommandTarget.Box => ApplyBoxCommand(command),
                CommandTarget.Region => ApplyRegionCommand(command),
                CommandTarget.Tile => ApplyTileCommand(command),
                CommandTarget.Vendor => ApplyVendorCommand(command),
                _ => false // Settings handled separately
            };

            if (success)
                applied++;
        }

        Logger.Info($"Applied {applied} spawn commands");
        return applied;
    }

    /// <summary>
    /// Apply a Box spawn command. ExtraData format: {MapId},{BoxId}
    /// </summary>
    private bool ApplyBoxCommand(EditCommand command)
    {
        try
        {
            var mapId = command.GetMapId();
            var boxId = command.GetBoxId();

            if (mapId == -1 || boxId == -1)
            {
                Logger.Warning($"Box command failed: Invalid ExtraData '{command.ExtraData}'");
                return false;
            }

            if (!Utility.BoxSpawns.TryGetValue(mapId, out var boxes))
            {
                Logger.Warning($"Box command failed: Map {mapId} not found");
                return false;
            }

            var entity = boxes.FirstOrDefault(b => b.Position == boxId);
            if (entity == null)
            {
                Logger.Warning($"Box command failed: Box {boxId} not found on map {mapId}");
                return false;
            }

            return ApplySpawnListCommand(entity, command);
        }
        catch (Exception ex)
        {
            Logger.Warning($"Box command failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Apply a Region spawn command. ExtraData format: {MapId},{RegionName}
    /// </summary>
    private bool ApplyRegionCommand(EditCommand command)
    {
        try
        {
            var mapId = command.GetMapId();
            var regionName = command.GetRegionName();

            if (mapId == -1 || string.IsNullOrEmpty(regionName))
            {
                Logger.Warning($"Region command failed: Invalid ExtraData '{command.ExtraData}'");
                return false;
            }

            if (!Utility.RegionSpawns.TryGetValue(mapId, out var regions))
            {
                Logger.Warning($"Region command failed: Map {mapId} not found");
                return false;
            }

            var entity = regions.FirstOrDefault(r =>
                r.Name.Equals(regionName, StringComparison.OrdinalIgnoreCase));
            if (entity == null)
            {
                Logger.Warning($"Region command failed: Region '{regionName}' not found on map {mapId}");
                return false;
            }

            return ApplySpawnListCommand(entity, command);
        }
        catch (Exception ex)
        {
            Logger.Warning($"Region command failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Apply a Tile spawn command. ExtraData format: {MapId},{TileName}
    /// </summary>
    private bool ApplyTileCommand(EditCommand command)
    {
        try
        {
            var mapId = command.GetMapId();
            var tileName = command.GetTileName();

            if (mapId == -1 || string.IsNullOrEmpty(tileName))
            {
                Logger.Warning($"Tile command failed: Invalid ExtraData '{command.ExtraData}'");
                return false;
            }

            if (!Utility.TileSpawns.TryGetValue(mapId, out var tiles))
            {
                Logger.Warning($"Tile command failed: Map {mapId} not found");
                return false;
            }

            var entity = tiles.FirstOrDefault(t =>
                t.Name.Equals(tileName, StringComparison.OrdinalIgnoreCase));
            if (entity == null)
            {
                Logger.Warning($"Tile command failed: Tile '{tileName}' not found on map {mapId}");
                return false;
            }

            return ApplySpawnListCommand(entity, command);
        }
        catch (Exception ex)
        {
            Logger.Warning($"Tile command failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Apply a Vendor spawn command. ExtraData format: {MapId},{X},{Y},{Z}
    /// </summary>
    private bool ApplyVendorCommand(EditCommand command)
    {
        try
        {
            var mapId = command.GetMapId();
            var location = command.GetVendorLocation();

            if (mapId == -1 || location == null)
            {
                Logger.Warning($"Vendor command failed: Invalid ExtraData '{command.ExtraData}'");
                return false;
            }

            if (!Utility.VendorSpawns.TryGetValue(mapId, out var vendors))
            {
                Logger.Warning($"Vendor command failed: Map {mapId} not found");
                return false;
            }

            var loc = location.Value;

            // Find vendor by matching location
            var entity = vendors.FirstOrDefault(v =>
                v.Location.X == loc.X &&
                v.Location.Y == loc.Y &&
                v.Location.Z == loc.Z);

            if (entity == null)
            {
                Logger.Warning($"Vendor command failed: Vendor at ({loc.X},{loc.Y},{loc.Z}) not found on map {mapId}");
                return false;
            }

            // Vendors use VendorList, not the 6-list structure
            string vendorName = command.SpawnName;

            if (command.Action == CommandAction.Add)
            {
                if (!entity.VendorList.Contains(vendorName, StringComparer.OrdinalIgnoreCase))
                {
                    entity.VendorList.Add(vendorName);
                    Logger.Info($"Added vendor '{vendorName}' at ({loc.X},{loc.Y},{loc.Z})");
                }
            }
            else if (command.Action == CommandAction.Remove)
            {
                var toRemove = entity.VendorList.FirstOrDefault(v =>
                    v.Equals(vendorName, StringComparison.OrdinalIgnoreCase));
                if (toRemove != null)
                {
                    entity.VendorList.Remove(toRemove);
                    Logger.Info($"Removed vendor '{vendorName}' from ({loc.X},{loc.Y},{loc.Z})");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning($"Vendor command failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Apply a spawn list command to an ISpawnEntity (Box, Region, Tile).
    /// Handles Add/Remove actions on the appropriate spawn list based on Section.
    /// </summary>
    private bool ApplySpawnListCommand(ISpawnEntity entity, EditCommand command)
    {
        var list = GetSpawnList(entity, command.Section);
        if (list == null)
        {
            Logger.Warning($"Invalid spawn section: {command.Section}");
            return false;
        }

        string spawnName = command.SpawnName;

        if (command.Action == CommandAction.Add)
        {
            if (!list.Contains(spawnName, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(spawnName);
                Logger.Info($"Added '{spawnName}' to {command.Target} {command.Section} list");
            }
        }
        else if (command.Action == CommandAction.Remove)
        {
            var toRemove = list.FirstOrDefault(s =>
                s.Equals(spawnName, StringComparison.OrdinalIgnoreCase));
            if (toRemove != null)
            {
                list.Remove(toRemove);
                Logger.Info($"Removed '{spawnName}' from {command.Target} {command.Section} list");
            }
        }

        return true;
    }

    /// <summary>
    /// Get the appropriate spawn list from an ISpawnEntity based on the SpawnSection.
    /// </summary>
    private static List<string>? GetSpawnList(ISpawnEntity entity, SpawnSection section)
    {
        return section switch
        {
            SpawnSection.Water => entity.WaterSpawns,
            SpawnSection.Weather => entity.WeatherSpawns,
            SpawnSection.Timed => entity.TimedSpawns,
            SpawnSection.Common => entity.CommonSpawns,
            SpawnSection.Uncommon => entity.UncommonSpawns,
            SpawnSection.Rare => entity.RareSpawns,
            _ => null
        };
    }

    #endregion
}
