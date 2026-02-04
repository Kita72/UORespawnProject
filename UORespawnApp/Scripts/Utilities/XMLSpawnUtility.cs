namespace UORespawnApp
{
    internal static class XMLSpawnUtility
    {
        internal static readonly string SpawnersFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "UOR_SpawnerList.txt");

        internal static readonly string StaticFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "UOR_StaticList.txt");

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

                        if (TryGetMap(parts[0], out GameMap map))
                        {
                            int x = int.Parse(parts[1]);
                            int y = int.Parse(parts[2]);
                            int range = int.Parse(parts[3]);

                            // Adjust X/Y to top-left corner (from center point)
                            int adjustedX = x - (range / 2);
                            int adjustedY = y - (range / 2);

                            Spawns.Add(new XMLSpawnPoint
                            {
                                Map = map,
                                X = adjustedX,
                                Y = adjustedY,
                                Width = range,
                                Height = range
                            });
                        }
                    }

                    Console.WriteLine($"Loaded {Spawns.Count} XML spawner points");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading spawner list: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Spawner list not found: {fileLoc}");
            }
        }

        internal static void LoadStaticList()
        {
            string fileLoc = StaticFile;

            if (Directory.Exists(Settings.ServUODataFolder))
            {
                var serverFile = Path.Combine(Settings.ServUODataFolder, "UOR_StaticList.txt");
                if (File.Exists(serverFile))
                {
                    fileLoc = serverFile;
                }
            }

            if (File.Exists(fileLoc))
            {
                try
                {
                    WorldSpawnUtility.SetStaticList([.. File.ReadAllLines(fileLoc)]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading static list: {ex.Message}");
                }
            }
        }

        private static bool TryGetMap(string name, out GameMap map)
        {
            switch (name.ToLower())
            {
                case "felucca":
                    map = GameMap.Map0;
                    return true;

                case "trammel":
                    map = GameMap.Map1;
                    return true;

                case "ilshenar":
                    map = GameMap.Map2;
                    return true;

                case "malas":
                    map = GameMap.Map3;
                    return true;

                case "tokuno":
                    map = GameMap.Map4;
                    return true;

                case "termur":
                case "ter mur":
                    map = GameMap.Map5;
                    return true;
            }

            map = GameMap.Map0;
            return false;
        }

        internal static List<XMLSpawnPoint> GetSpawnersForMap(GameMap map)
        {
            return [.. Spawns.Where(s => s.Map == map)];
        }
    }
}
