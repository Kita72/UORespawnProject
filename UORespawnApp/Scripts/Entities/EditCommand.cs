using UORespawnApp.Scripts.Enums;

namespace UORespawnApp.Scripts.Entities;

/// <summary>
/// Represents a single edit command from the server.
/// Commands are pipe-delimited: Action|Target|Section|Trigger|SpawnName|ExtraData
/// </summary>
public class EditCommand
{
    /// <summary>
    /// The action to perform (Add, Remove, Update)
    /// </summary>
    public CommandAction Action { get; init; }

    /// <summary>
    /// The target type (Settings, Box, Region, Tile, Vendor)
    /// </summary>
    public CommandTarget Target { get; init; }

    /// <summary>
    /// The spawn section to modify (None, Common, Uncommon, Rare, Water, Weather, Timed)
    /// </summary>
    public SpawnSection Section { get; init; }

    /// <summary>
    /// The trigger condition (None, Weather, Timed)
    /// </summary>
    public SpawnTrigger Trigger { get; init; }

    /// <summary>
    /// The creature/vendor name or setting key
    /// </summary>
    public string SpawnName { get; init; } = string.Empty;

    /// <summary>
    /// Extra data for identifying the target:
    /// - Settings: The setting value
    /// - Box: MapId,BoxId
    /// - Region: MapId,RegionName
    /// - Tile: MapId,TileName
    /// - Vendor: MapId,X,Y,Z
    /// </summary>
    public string ExtraData { get; init; } = string.Empty;

    /// <summary>
    /// The original line from the command file (for debugging)
    /// </summary>
    public string RawLine { get; init; } = string.Empty;

    /// <summary>
    /// Parse a pipe-delimited command line into an EditCommand.
    /// Format: Action|Target|Section|Trigger|SpawnName|ExtraData
    /// Returns null if parsing fails.
    /// </summary>
    /// <param name="line">The raw command line to parse</param>
    /// <returns>Parsed EditCommand or null if invalid</returns>
    public static EditCommand? Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        // Skip comments
        if (line.TrimStart().StartsWith('#'))
            return null;

        var parts = line.Split('|');
        if (parts.Length < 6)
            return null;

        // Parse Action
        if (!Enum.TryParse<CommandAction>(parts[0], ignoreCase: true, out var action))
            return null;

        // Parse Target
        if (!Enum.TryParse<CommandTarget>(parts[1], ignoreCase: true, out var target))
            return null;

        // Parse Section
        if (!Enum.TryParse<SpawnSection>(parts[2], ignoreCase: true, out var section))
            return null;

        // Parse Trigger
        if (!Enum.TryParse<SpawnTrigger>(parts[3], ignoreCase: true, out var trigger))
            return null;

        return new EditCommand
        {
            Action = action,
            Target = target,
            Section = section,
            Trigger = trigger,
            SpawnName = parts[4],
            ExtraData = parts[5],
            RawLine = line
        };
    }

    /// <summary>
    /// Serialize the command back to pipe-delimited format.
    /// </summary>
    public override string ToString()
    {
        return $"{Action}|{Target}|{Section}|{Trigger}|{SpawnName}|{ExtraData}";
    }

    /// <summary>
    /// Get a human-readable description of the command.
    /// </summary>
    public string GetDescription()
    {
        return Target switch
        {
            CommandTarget.Settings => $"{Action} setting {SpawnName} = {ExtraData}",
            CommandTarget.Box => $"{Action} '{SpawnName}' to Box ({ExtraData}) [{Section}]",
            CommandTarget.Region => $"{Action} '{SpawnName}' to Region ({ExtraData}) [{Section}]",
            CommandTarget.Tile => $"{Action} '{SpawnName}' to Tile ({ExtraData}) [{Section}]",
            CommandTarget.Vendor => $"{Action} '{SpawnName}' vendor at ({ExtraData})",
            _ => RawLine
        };
    }

    /// <summary>
    /// Parse the MapId from ExtraData.
    /// Returns -1 if parsing fails.
    /// </summary>
    public int GetMapId()
    {
        if (string.IsNullOrEmpty(ExtraData))
            return -1;

        var parts = ExtraData.Split(',');
        if (parts.Length > 0 && int.TryParse(parts[0], out var mapId))
            return mapId;

        return -1;
    }

    /// <summary>
    /// Parse Box ID from ExtraData (for Box commands).
    /// Format: MapId,BoxId
    /// Returns -1 if parsing fails.
    /// </summary>
    public int GetBoxId()
    {
        if (Target != CommandTarget.Box || string.IsNullOrEmpty(ExtraData))
            return -1;

        var parts = ExtraData.Split(',');
        if (parts.Length > 1 && int.TryParse(parts[1], out var boxId))
            return boxId;

        return -1;
    }

    /// <summary>
    /// Parse Region name from ExtraData (for Region commands).
    /// Format: MapId,RegionName
    /// </summary>
    public string? GetRegionName()
    {
        if (Target != CommandTarget.Region || string.IsNullOrEmpty(ExtraData))
            return null;

        var parts = ExtraData.Split(',');
        return parts.Length > 1 ? parts[1] : null;
    }

    /// <summary>
    /// Parse Tile name from ExtraData (for Tile commands).
    /// Format: MapId,TileName
    /// </summary>
    public string? GetTileName()
    {
        if (Target != CommandTarget.Tile || string.IsNullOrEmpty(ExtraData))
            return null;

        var parts = ExtraData.Split(',');
        return parts.Length > 1 ? parts[1] : null;
    }

    /// <summary>
    /// Parse vendor location from ExtraData (for Vendor commands).
    /// Format: MapId,X,Y,Z
    /// Returns null if parsing fails.
    /// </summary>
    public (int X, int Y, int Z)? GetVendorLocation()
    {
        if (Target != CommandTarget.Vendor || string.IsNullOrEmpty(ExtraData))
            return null;

        var parts = ExtraData.Split(',');
        if (parts.Length >= 4 &&
            int.TryParse(parts[1], out var x) &&
            int.TryParse(parts[2], out var y) &&
            int.TryParse(parts[3], out var z))
        {
            return (x, y, z);
        }

        return null;
    }
}
