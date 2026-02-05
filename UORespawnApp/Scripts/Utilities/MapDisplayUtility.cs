namespace UORespawnApp
{
    internal static class MapDisplayUtility
    {
        private static readonly List<(string ShortTime, string PlayerName, GameMap PlayerMap, Microsoft.Maui.Graphics.Point PlayerLocation, Microsoft.Maui.Graphics.Point SpawnLocation)> SpawnStats = [];

        private static readonly Dictionary<string, Color> playerColorCache = [];

        private static readonly Random rand = new();

        internal static bool HasSpawnData => SpawnStats.Count > 0;

        internal static List<SpawnStatData> GetSpawnDataForMap(GameMap map)
        {
            var result = new List<SpawnStatData>();
            var filteredData = SpawnStats.Where(item => item.PlayerMap == map).ToList();
            
            foreach (var data in filteredData)
            {
                Color playerColor;
                
                if (playerColorCache.TryGetValue(data.PlayerName, out Color? value))
                {
                    playerColor = value;
                }
                else
                {
                    playerColor = Color.FromRgb(rand.Next(256), rand.Next(256), rand.Next(256));
                    playerColorCache[data.PlayerName] = playerColor;
                }
                
                result.Add(new SpawnStatData
                {
                    PlayerX = (int)data.PlayerLocation.X,
                    PlayerY = (int)data.PlayerLocation.Y,
                    SpawnX = (int)data.SpawnLocation.X,
                    SpawnY = (int)data.SpawnLocation.Y,
                    ColorR = (int)(playerColor.Red * 255),
                    ColorG = (int)(playerColor.Green * 255),
                    ColorB = (int)(playerColor.Blue * 255)
                });
            }
            
            return result;
        }

        internal static int GetSpawnDataCount(GameMap map)
        {
            return SpawnStats.Count(item => item.PlayerMap == map);
        }

        internal static void InstantiateStatData()
        {
            string statsFolderPath = Path.Combine(Settings.ServUODataFolder, "UOR_Stats");

            Console.WriteLine($"📂 Looking for spawn stats in: {statsFolderPath}");

            if (Directory.Exists(statsFolderPath))
            {
                var files = Directory.GetFiles(statsFolderPath, "*.txt");
                Console.WriteLine($"📄 Found {files.Length} stat files");

                int totalLines = 0;
                int successfulLines = 0;
                int skippedLines = 0;

                foreach (string filePath in files)
                {
                    try
                    {
                        Console.WriteLine($"📖 Reading: {Path.GetFileName(filePath)}");
                        int fileLineCount = 0;

                        foreach (string line in File.ReadLines(filePath))
                        {
                            totalLines++;
                            fileLineCount++;

                            if (string.IsNullOrWhiteSpace(line))
                            {
                                skippedLines++;
                                continue;
                            }

                            string[] parts = line.Split('|');

                            if (parts.Length == 7)
                            {
                                try
                                {
                                    string shortTime = parts[0].Trim();      // "4:46 PM"
                                    string playerName = parts[1].Trim();     // "Maliki"
                                    string mapString = parts[2].Trim();      // "Trammel"

                                    // Convert map name to GameMap enum
                                    GameMap map = ConvertMapNameToEnum(mapString);

                                    int playerX = int.Parse(parts[3].Trim());
                                    int playerY = int.Parse(parts[4].Trim());
                                    int spawnX = int.Parse(parts[5].Trim());
                                    int spawnY = int.Parse(parts[6].Trim());

                                    SpawnStats.Add((shortTime, playerName, map, 
                                        new Microsoft.Maui.Graphics.Point(playerX, playerY), 
                                        new Microsoft.Maui.Graphics.Point(spawnX, spawnY)));

                                    successfulLines++;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"⚠️ Error parsing line: '{line}' - {ex.Message}");
                                    skippedLines++;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ Invalid line format (expected 7 parts, got {parts.Length}): '{line}'");
                                skippedLines++;
                            }
                        }

                        Console.WriteLine($"   ✅ Processed {fileLineCount} lines from {Path.GetFileName(filePath)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error reading file {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                Console.WriteLine($"");
                Console.WriteLine($"📊 SUMMARY:");
                Console.WriteLine($"   Total lines: {totalLines}");
                Console.WriteLine($"   Successfully parsed: {successfulLines}");
                Console.WriteLine($"   Skipped/errors: {skippedLines}");
                Console.WriteLine($"   Spawn events loaded: {SpawnStats.Count}");
            }
            else
            {
                Console.WriteLine($"❌ UOR_Stats folder not found at: {statsFolderPath}");
            }
        }

        private static GameMap ConvertMapNameToEnum(string mapName)
        {
            // Handle both full names and enum names
            return mapName.ToLower() switch
            {
                "felucca" or "map0" => GameMap.Map0,
                "trammel" or "map1" => GameMap.Map1,
                "ilshenar" or "map2" => GameMap.Map2,
                "malas" or "map3" => GameMap.Map3,
                "tokuno" or "map4" => GameMap.Map4,
                "termur" or "ter mur" or "map5" => GameMap.Map5,
                _ => throw new ArgumentException($"Unknown map name: {mapName}")
            };
        }

        internal static void ClearSpawnData()
        {
            SpawnStats.Clear();
            playerColorCache.Clear();
            Console.WriteLine("?? Cleared spawn statistics data");
        }
    }

    public class SpawnStatData
    {
        public int PlayerX { get; set; }
        public int PlayerY { get; set; }
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }
        public int ColorR { get; set; }
        public int ColorG { get; set; }
        public int ColorB { get; set; }
    }
}
