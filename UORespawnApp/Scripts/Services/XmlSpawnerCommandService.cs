using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Service for writing XML spawner commands to be processed by the server.
/// Commands are written to INPUT/UOR_XmlCommands.txt
/// Tracks pending commands per session to prevent duplicates.
/// </summary>
public class XmlSpawnerCommandService
{
    private const string COMMAND_FILENAME = "UOR_XmlCommands.txt";

    // Track pending commands by serial to prevent duplicates in a session
    // Key: serial, Value: command type (DELETE, EDIT, ADD)
    private readonly Dictionary<string, string> _pendingCommandsBySerial = new(StringComparer.OrdinalIgnoreCase);

    // Track pending ADD commands by location to prevent duplicate spawners at same spot
    // Key: "MapId|X|Y", Value: true
    private readonly HashSet<string> _pendingAddLocations = new(StringComparer.OrdinalIgnoreCase);

    private readonly Lock _lock = new();

    /// <summary>
    /// Gets the path to the XML commands file.
    /// Returns null if server is not linked.
    /// </summary>
    private string? GetCommandFilePath()
    {
        var inputPath = PathConstants.ServerInputPath;
        if (string.IsNullOrEmpty(inputPath))
        {
            Logger.Warning("Cannot write XML command: Server not linked");
            return null;
        }
        return Path.Combine(inputPath, COMMAND_FILENAME);
    }

    /// <summary>
    /// Checks if a spawner already has a pending command in this session.
    /// </summary>
    /// <param name="serial">The spawner serial to check</param>
    /// <returns>The pending command type, or null if none</returns>
    public string? GetPendingCommand(string serial)
    {
        if (string.IsNullOrEmpty(serial)) return null;
        lock (_lock)
        {
            return _pendingCommandsBySerial.TryGetValue(serial, out var cmd) ? cmd : null;
        }
    }

    /// <summary>
    /// Checks if a location already has a pending ADD command.
    /// </summary>
    public bool HasPendingAddAt(int mapId, int x, int y)
    {
        var key = $"{mapId}|{x}|{y}";
        lock (_lock)
        {
            return _pendingAddLocations.Contains(key);
        }
    }

    /// <summary>
    /// Clears all pending commands. Call when server syncs or on app restart.
    /// </summary>
    public void ClearPendingCommands()
    {
        lock (_lock)
        {
            _pendingCommandsBySerial.Clear();
            _pendingAddLocations.Clear();
        }
        Logger.Info("Cleared pending XML spawner commands");
    }

    /// <summary>
    /// Writes a DELETE command for an XML spawner.
    /// Format: DELETE|Serial
    /// </summary>
    /// <param name="serial">The serial number of the spawner to delete (e.g., "0x12345678")</param>
    /// <returns>True if command was written successfully</returns>
    public bool WriteDeleteCommand(string serial)
    {
        if (string.IsNullOrEmpty(serial))
        {
            Logger.Warning("Cannot delete XML spawner: No serial provided");
            return false;
        }

        var filePath = GetCommandFilePath();
        if (filePath == null) return false;

        lock (_lock)
        {
            // Check for existing pending command on this spawner
            if (_pendingCommandsBySerial.TryGetValue(serial, out var existingCmd))
            {
                Logger.Warning($"Cannot delete XML spawner {serial}: Already has pending {existingCmd} command");
                return false;
            }

            try
            {
                var command = $"DELETE|{serial}";
                File.AppendAllLines(filePath, [command]);
                _pendingCommandsBySerial[serial] = "DELETE";
                Logger.Info($"XML spawner delete command written: {serial}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to write XML delete command: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Writes an ADD command for a new XML spawner.
    /// Format: ADD|MapId|X|Y|Z|HomeRange|MaxCount|Creature1:Count1|Creature2:Count2|...
    /// </summary>
    /// <param name="mapId">Map ID where spawner will be placed</param>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="z">Z coordinate (usually 0, server will adjust)</param>
    /// <param name="homeRange">Home range / radius of the spawner (10-250)</param>
    /// <param name="maxCount">Maximum spawn count (1-100)</param>
    /// <param name="creatures">List of creature names (duplicates count as quantity)</param>
    /// <returns>True if command was written successfully</returns>
    public bool WriteAddCommand(int mapId, int x, int y, int z, int homeRange, int maxCount, List<string> creatures)
    {
        if (creatures == null || creatures.Count == 0)
        {
            Logger.Warning("Cannot add XML spawner: No creatures specified");
            return false;
        }

        var filePath = GetCommandFilePath();
        if (filePath == null) return false;

        lock (_lock)
        {
            // Check for existing pending ADD at this location
            var locationKey = $"{mapId}|{x}|{y}";
            if (_pendingAddLocations.Contains(locationKey))
            {
                Logger.Warning($"Cannot add XML spawner: Already has pending ADD command at Map {mapId} ({x},{y})");
                return false;
            }

            try
            {
                // Count duplicates to determine creature quantities
                var creatureCounts = creatures
                    .GroupBy(c => c)
                    .Select(g => $"{g.Key}:{g.Count()}")
                    .ToList();

                var creaturesData = string.Join("|", creatureCounts);
                var command = $"ADD|{mapId}|{x}|{y}|{z}|{homeRange}|{maxCount}|{creaturesData}";

                File.AppendAllLines(filePath, [command]);
                _pendingAddLocations.Add(locationKey);
                Logger.Info($"XML spawner add command written: Map {mapId} at ({x},{y}) Range:{homeRange} MaxCount:{maxCount} Creatures:{creatures.Count}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to write XML add command: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Writes an EDIT command for an existing XML spawner.
    /// Format: EDIT|Serial|HomeRange|MaxCount|Creature1:Count1|Creature2:Count2|...
    /// </summary>
    /// <param name="serial">The serial number of the spawner to edit</param>
    /// <param name="homeRange">Home range / radius of the spawner (10-250)</param>
    /// <param name="maxCount">Maximum spawn count (1-100)</param>
    /// <param name="creatures">List of creature names (duplicates count as quantity)</param>
    /// <returns>True if command was written successfully</returns>
    public bool WriteEditCommand(string serial, int homeRange, int maxCount, List<string> creatures)
    {
        if (string.IsNullOrEmpty(serial))
        {
            Logger.Warning("Cannot edit XML spawner: No serial provided");
            return false;
        }

        if (creatures == null || creatures.Count == 0)
        {
            Logger.Warning("Cannot edit XML spawner: No creatures specified");
            return false;
        }

        var filePath = GetCommandFilePath();
        if (filePath == null) return false;

        lock (_lock)
        {
            // Check for existing pending command on this spawner
            if (_pendingCommandsBySerial.TryGetValue(serial, out var existingCmd))
            {
                Logger.Warning($"Cannot edit XML spawner {serial}: Already has pending {existingCmd} command");
                return false;
            }

            try
            {
                // Count duplicates to determine creature quantities
                var creatureCounts = creatures
                    .GroupBy(c => c)
                    .Select(g => $"{g.Key}:{g.Count()}")
                    .ToList();

                var creaturesData = string.Join("|", creatureCounts);
                var command = $"EDIT|{serial}|{homeRange}|{maxCount}|{creaturesData}";

                File.AppendAllLines(filePath, [command]);
                _pendingCommandsBySerial[serial] = "EDIT";
                Logger.Info($"XML spawner edit command written: {serial} Range:{homeRange} MaxCount:{maxCount} Creatures:{creatures.Count}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to write XML edit command: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Checks if the server is linked and commands can be written.
    /// </summary>
    public bool CanWriteCommands()
    {
        return !string.IsNullOrEmpty(PathConstants.ServerInputPath);
    }
}
