using System.Text.RegularExpressions;
using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Helpers;

namespace UORespawnApp.Scripts.Utilities
{
    internal static partial class SpawnerListUtility
    {
        internal static string SpawnersFile => PathConstants.GetSpawnerListFilePath();

        internal static List<XMLSpawnPoint> Spawns { get; private set; } = [];

        // Regex to strip {RND,x,y} patterns from spawn names (e.g., "Phantom,{RND,1,5}" -> "Phantom")
        [GeneratedRegex(@",?\{RND,\d+,\d+\}", RegexOptions.IgnoreCase)]
        private static partial Regex RndPatternRegex();

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

                    string[] lines = FileUtility.ReadAllLines(fileLoc);

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
                        if (parts.Length >= offset + 6 && !string.IsNullOrWhiteSpace(parts[offset + 5]))
                        {
                            rawNames = [.. parts[offset + 5].Split('|', StringSplitOptions.RemoveEmptyEntries)];
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

                        // Store center coordinates and radius for circle visualization
                        Spawns.Add(new XMLSpawnPoint
                        {
                            Serial = serial,
                            Map = mapId,
                            CenterX = x,
                            CenterY = y,
                            Radius = range,
                            MaxCount = maxCount,
                            SpawnNames = cleanedNames
                        });
                    }

                    Logger.Info($"Loaded {Spawns.Count} XML spawners");
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
    }
}
