using System.Text.RegularExpressions;
using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities
{
    internal static partial class SpawnerListUtility
    {
        internal static readonly string SpawnersFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "UOR_SpawnerList.txt");

        internal static List<XMLSpawnPoint> Spawns { get; private set; } = [];

        // Regex to strip {RND,x,y} patterns from spawn names (e.g., "Phantom,{RND,1,5}" -> "Phantom")
        [GeneratedRegex(@",?\{RND,\d+,\d+\}", RegexOptions.IgnoreCase)]
        private static partial Regex RndPatternRegex();

        // Patterns for detecting special spawner types
        private const string TreasureLevelPattern = "TreasureLevel";
        private const string QuestNpcPattern = "xmlquestnpc";

        internal static void LoadSpawnerList()
        {
            string fileLoc = SpawnersFile;

            // Check server OUTPUT folder first (new v2.0 structure)
            var serverOutputPath = PathConstants.ServerOutputPath;
            if (!string.IsNullOrEmpty(serverOutputPath))
            {
                var serverFile = Path.Combine(serverOutputPath, PathConstants.SPAWNER_LIST_FILENAME);
                if (File.Exists(serverFile))
                {
                    fileLoc = serverFile;
                }
            }

            if (File.Exists(fileLoc))
            {
                try
                {
                    Spawns.Clear();

                    string[] lines = File.ReadAllLines(fileLoc);

                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(':');

                        // Format: Serial:MapId:X:Y:HomeRange:MaxCount:SpawnNames (SpawnNames are pipe-separated)
                        // Also supports legacy format without Serial: MapId:X:Y:HomeRange:MaxCount:SpawnNames
                        if (parts.Length < 4) continue;

                        // Detect format: if first part is numeric (serial), use new format
                        // Legacy format starts with MapId (0-5), new format starts with Serial (large int)
                        bool hasSerial = int.TryParse(parts[0], out int serialValue) && serialValue > 255;
                        int offset = hasSerial ? 1 : 0;

                        string serial = hasSerial ? parts[0] : string.Empty;

                        // Parse map ID
                        if (!int.TryParse(parts[offset], out int mapId))
                        {
                            continue; // Skip invalid map ID
                        }

                        if (!int.TryParse(parts[offset + 1], out int x) ||
                            !int.TryParse(parts[offset + 2], out int y) ||
                            !int.TryParse(parts[offset + 3], out int range))
                        {
                            continue; // Skip invalid coordinates
                        }

                        // Parse MaxCount (default to 0 if not present)
                        int maxCount = 0;
                        if (parts.Length >= offset + 5 && int.TryParse(parts[offset + 4], out int parsedMax))
                        {
                            maxCount = parsedMax;
                        }

                        // Parse SpawnNames (pipe-separated, default to empty list)
                        // Also clean {RND,x,y} patterns from names
                        List<string> rawNames = [];
                        string rawSpawnData = string.Empty;
                        if (parts.Length >= offset + 6 && !string.IsNullOrWhiteSpace(parts[offset + 5]))
                        {
                            rawSpawnData = parts[offset + 5];
                            rawNames = [.. rawSpawnData.Split('|', StringSplitOptions.RemoveEmptyEntries)];
                        }

                        // Clean spawn names by removing {RND,x,y} patterns
                        List<string> cleanedNames = [];
                        foreach (var name in rawNames)
                        {
                            string cleaned = RndPatternRegex().Replace(name, "").Trim();
                            if (!string.IsNullOrEmpty(cleaned))
                            {
                                cleanedNames.Add(cleaned);
                            }
                        }

                        // Classify the spawner type based on spawn data
                        SpawnerType spawnerType = ClassifySpawnerType(rawSpawnData, cleanedNames);

                        // Store center coordinates and radius for circle visualization
                        // Also keep legacy X/Y adjusted to top-left for backward compatibility
                        int adjustedX = x - (range / 2);
                        int adjustedY = y - (range / 2);

                        Spawns.Add(new XMLSpawnPoint
                        {
                            Serial = serial,
                            Map = mapId,
                            CenterX = x,
                            CenterY = y,
                            Radius = range,
                            MaxCount = maxCount,
                            SpawnNames = cleanedNames,
                            Type = spawnerType,
                            X = adjustedX,
                            Y = adjustedY,
                            Width = range,
                            Height = range
                        });
                    }

                    // Log summary by type
                    int regular = Spawns.Count(s => s.Type == SpawnerType.Regular);
                    int empty = Spawns.Count(s => s.Type == SpawnerType.Empty);
                    int treasure = Spawns.Count(s => s.Type == SpawnerType.Treasure);
                    int quest = Spawns.Count(s => s.Type == SpawnerType.Quest);
                    Logger.Info($"Loaded {Spawns.Count} XML spawners (Regular: {regular}, Empty: {empty}, Treasure: {treasure}, Quest: {quest})");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error loading XML spawner list", ex);
                }
            }
            else
            {
                Logger.Warning($"XML spawner list not found: {fileLoc}");
            }
        }

        internal static List<XMLSpawnPoint> GetSpawnersForMap(int mapId)
        {
            return [.. Spawns.Where(s => s.Map == mapId)];
        }

        /// <summary>
        /// Gets a spawner by its serial number.
        /// </summary>
        internal static XMLSpawnPoint? GetSpawnerBySerial(string serial)
        {
            return Spawns.FirstOrDefault(s => s.Serial == serial);
        }

        /// <summary>
        /// Removes a spawner from the local list (after delete command sent).
        /// </summary>
        internal static void RemoveSpawnerLocally(string serial)
        {
            Spawns.RemoveAll(s => s.Serial == serial);
        }

        /// <summary>
        /// Classifies a spawner based on its spawn data content.
        /// </summary>
        /// <param name="rawSpawnData">The raw spawn data string before cleaning</param>
        /// <param name="cleanedNames">The cleaned list of spawn names</param>
        /// <returns>The classified SpawnerType</returns>
        private static SpawnerType ClassifySpawnerType(string rawSpawnData, List<string> cleanedNames)
        {
            // Check for quest spawners first (look for xmlquestnpc pattern in raw data)
            if (!string.IsNullOrEmpty(rawSpawnData) &&
                rawSpawnData.Contains(QuestNpcPattern, StringComparison.OrdinalIgnoreCase))
            {
                return SpawnerType.Quest;
            }

            // Check for treasure spawners (TreasureLevel1, TreasureLevel2, etc.)
            if (cleanedNames.Any(name => name.StartsWith(TreasureLevelPattern, StringComparison.OrdinalIgnoreCase)))
            {
                return SpawnerType.Treasure;
            }

            // Check for empty spawners (no creatures defined)
            if (cleanedNames.Count == 0)
            {
                return SpawnerType.Empty;
            }

            // Default to regular spawner
            return SpawnerType.Regular;
        }

        /// <summary>
        /// Gets the display name for a spawner type.
        /// </summary>
        internal static string GetSpawnerTypeName(SpawnerType type) => type switch
        {
            SpawnerType.Regular => "Creature Spawner",
            SpawnerType.Empty => "Empty Spawner",
            SpawnerType.Treasure => "Treasure Spawner",
            SpawnerType.Quest => "Quest Spawner",
            _ => "Unknown"
        };
    }
}
