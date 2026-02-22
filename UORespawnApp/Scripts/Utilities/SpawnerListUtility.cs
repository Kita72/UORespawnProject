namespace UORespawnApp.Scripts.Utilities
{
    internal static class SpawnerListUtility
    {
        internal static readonly string SpawnersFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "UOR_SpawnerList.txt");

        internal static List<XMLSpawnPoint> Spawns { get; private set; } = [];

        internal static void LoadSpawnerList()
        {
            string fileLoc = SpawnersFile;

            if (Directory.Exists(Settings.ServUODataFolder))
            {
                var serverFile = Path.Combine(Settings.ServUODataFolder, "UOR_SpawnerList.txt");
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

                        // Format: MapId:X:Y:HomeRange:MaxCount:SpawnNames (SpawnNames are pipe-separated)
                        if (parts.Length < 4) continue;

                        // Parse map ID directly
                        if (!int.TryParse(parts[0], out int mapId))
                        {
                            continue; // Skip invalid map ID
                        }

                        if (!int.TryParse(parts[1], out int x) ||
                            !int.TryParse(parts[2], out int y) ||
                            !int.TryParse(parts[3], out int range))
                        {
                            continue; // Skip invalid coordinates
                        }

                        // Parse MaxCount (default to 0 if not present)
                        int maxCount = 0;
                        if (parts.Length >= 5 && int.TryParse(parts[4], out int parsedMax))
                        {
                            maxCount = parsedMax;
                        }

                        // Parse SpawnNames (pipe-separated, default to empty list)
                        List<string> spawnNames = [];
                        if (parts.Length >= 6 && !string.IsNullOrWhiteSpace(parts[5]))
                        {
                            spawnNames = [.. parts[5].Split('|', StringSplitOptions.RemoveEmptyEntries)];
                        }

                        // Store center coordinates and radius for circle visualization
                        // Also keep legacy X/Y adjusted to top-left for backward compatibility
                        int adjustedX = x - (range / 2);
                        int adjustedY = y - (range / 2);

                        Spawns.Add(new XMLSpawnPoint
                        {
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
    }
}
