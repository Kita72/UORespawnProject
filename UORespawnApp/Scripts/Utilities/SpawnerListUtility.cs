using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities
{
    internal static class SpawnerListUtility
    {
        internal static readonly string SpawnersFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "UOR_SpawnerList.txt");

        internal static List<XMLSpawnPoint> Spawns { get; private set; } = [];

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
                        List<string> spawnNames = [];
                        if (parts.Length >= offset + 6 && !string.IsNullOrWhiteSpace(parts[offset + 5]))
                        {
                            spawnNames = [.. parts[offset + 5].Split('|', StringSplitOptions.RemoveEmptyEntries)];
                        }

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
                            SpawnNames = spawnNames,
                            X = adjustedX,
                            Y = adjustedY,
                            Width = range,
                            Height = range
                        });
                    }

                    Logger.Info($"Loaded {Spawns.Count} XML spawner points");
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
