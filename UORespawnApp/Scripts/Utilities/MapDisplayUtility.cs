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

            if (Directory.Exists(statsFolderPath))
            {
                foreach (string filePath in Directory.GetFiles(statsFolderPath))
                {
                    try
                    {
                        foreach (string line in File.ReadLines(filePath))
                        {
                            string[] parts = line.Split('|');

                            if (parts.Length == 7)
                            {
                                string shortTime = parts[0];
                                string playerName = parts[1];

                                if (Enum.TryParse(parts[2], out GameMap map))
                                {
                                    int playerX = int.Parse(parts[3]);
                                    int playerY = int.Parse(parts[4]);
                                    int spawnX = int.Parse(parts[5]);
                                    int spawnY = int.Parse(parts[6]);

                                    SpawnStats.Add((shortTime, playerName, map, new Microsoft.Maui.Graphics.Point(playerX, playerY), new Microsoft.Maui.Graphics.Point(spawnX, spawnY)));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
                    }
                }
            }
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
