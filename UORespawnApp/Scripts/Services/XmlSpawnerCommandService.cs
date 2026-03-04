using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Service for writing XML spawner commands to be processed by the server.
/// Commands are written to INPUT/UOR_XmlCommands.txt
/// </summary>
public class XmlSpawnerCommandService
{
    private const string COMMAND_FILENAME = "UOR_XmlCommands.txt";

    /// <summary>
    /// Gets the path to the XML commands file.
    /// Returns null if server is not linked.
    /// </summary>
    private static string? GetCommandFilePath()
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

        try
        {
            var command = $"DELETE|{serial}";
            File.AppendAllLines(filePath, [command]);
            Logger.Info($"XML spawner delete command written: {serial}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to write XML delete command: {ex.Message}");
            return false;
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
            Logger.Info($"XML spawner add command written: Map {mapId} at ({x},{y}) Range:{homeRange} MaxCount:{maxCount} Creatures:{creatures.Count}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to write XML add command: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if the server is linked and commands can be written.
    /// </summary>
    public static bool CanWriteCommands()
    {
        return !string.IsNullOrEmpty(PathConstants.ServerInputPath);
    }
}
