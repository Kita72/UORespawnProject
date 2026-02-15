using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp
{
    internal static class XMLSpawnUtility
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

                        if (parts.Length < 4) continue;

                        if (TryGetMap(parts[0], out int mapId))
                        {
                            int x = int.Parse(parts[1]);
                            int y = int.Parse(parts[2]);
                            int range = int.Parse(parts[3]);

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
                                X = adjustedX,
                                Y = adjustedY,
                                Width = range,
                                Height = range
                            });
                        }
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

        private static bool TryGetMap(string name, out int mapId)
        {
            switch (name.ToLower())
            {
                case "felucca":
                    mapId = 0;
                    return true;

                case "trammel":
                    mapId = 1;
                    return true;

                case "ilshenar":
                    mapId = 2;
                    return true;

                case "malas":
                    mapId = 3;
                    return true;

                case "tokuno":
                    mapId = 4;
                    return true;

                case "termur":
                case "ter mur":
                    mapId = 5;
                    return true;
            }

            mapId = 0;
            return false;
        }

        internal static List<XMLSpawnPoint> GetSpawnersForMap(int mapId)
        {
            return [.. Spawns.Where(s => s.Map == mapId)];
        }
    }
}
