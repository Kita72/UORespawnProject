using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities
{
    internal static class MapDisplayUtility
    {
        private static readonly List<(string ShortTime, string PlayerName, int PlayerMap, Microsoft.Maui.Graphics.Point PlayerLocation, Microsoft.Maui.Graphics.Point SpawnLocation, string CreatureName)> SpawnStats = [];

        private static readonly Dictionary<string, Color> playerColorCache = [];

        private static readonly Random rand = new();

        internal static bool HasSpawnData => SpawnStats.Count > 0;

        internal static List<SpawnStatData> GetSpawnDataForMap(int mapId)
        {
            var result = new List<SpawnStatData>();
            var filteredData = SpawnStats.Where(item => item.PlayerMap == mapId).ToList();

            // Pre-calculate total dots per player for this map
            var playerDotCounts = filteredData
                .GroupBy(d => d.PlayerName)
                .ToDictionary(g => g.Key, g => g.Count());

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
                    PlayerName = data.PlayerName,
                    PlayerX = (int)data.PlayerLocation.X,
                    PlayerY = (int)data.PlayerLocation.Y,
                    SpawnX = (int)data.SpawnLocation.X,
                    SpawnY = (int)data.SpawnLocation.Y,
                    CreatureName = data.CreatureName,
                    ColorR = (int)(playerColor.Red * 255),
                    ColorG = (int)(playerColor.Green * 255),
                    ColorB = (int)(playerColor.Blue * 255),
                    TotalDotsForPlayer = playerDotCounts.GetValueOrDefault(data.PlayerName, 1)
                });
            }

            return result;
        }

        internal static int GetSpawnDataCount(int mapId)
        {
            return SpawnStats.Count(item => item.PlayerMap == mapId);
        }

        internal static void InstantiateStatData()
        {
            var statsFolderPath = PathConstants.ServerStatsPath;

            if (string.IsNullOrEmpty(statsFolderPath))
            {
                Logger.Warning("Server STATS folder not available - no ServUO folder linked or STATS folder doesn't exist");
                return;
            }

            Logger.Info($"Looking for spawn stats in: {statsFolderPath}");

            if (Directory.Exists(statsFolderPath))
            {
                var files = Directory.GetFiles(statsFolderPath, "*.txt");
                Logger.Info($"Found {files.Length} stat files");

                int totalLines = 0;
                int successfulLines = 0;
                int skippedLines = 0;

                foreach (string filePath in files)
                {
                    try
                    {
                        Logger.Info($"Reading spawn stats: {Path.GetFileName(filePath)}");
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

                            // Support both old format (7 parts) and new format (8 parts with creature name)
                            // Old: Time|Player|Map|PX|PY|SX|SY
                            // New: Time|Player|Map|PX|PY|SX|SY|CreatureName
                            if (parts.Length >= 7)
                            {
                                try
                                {
                                    string shortTime = parts[0].Trim();      // "4:46 PM"
                                    string playerName = parts[1].Trim();     // "Maliki"
                                    string mapString = parts[2].Trim();      // "Trammel"

                                    // Convert map name to int mapId
                                    int mapId = MapUtility.ParseMapName(mapString);

                                    int playerX = int.Parse(parts[3].Trim());
                                    int playerY = int.Parse(parts[4].Trim());
                                    int spawnX = int.Parse(parts[5].Trim());
                                    int spawnY = int.Parse(parts[6].Trim());

                                    // Parse creature name (default to empty if not present)
                                    string creatureName = parts.Length >= 8 ? parts[7].Trim() : "";

                                    SpawnStats.Add((shortTime, playerName, mapId,
                                        new Microsoft.Maui.Graphics.Point(playerX, playerY), 
                                        new Microsoft.Maui.Graphics.Point(spawnX, spawnY),
                                        creatureName));

                                    successfulLines++;
                                }
                                catch (Exception ex)
                                {
                                    Logger.Warning($"Error parsing spawn stat line: '{line}' - {ex.Message}");
                                    skippedLines++;
                                }
                            }
                            else
                            {
                                Logger.Warning($"Invalid spawn stat line format (expected 7-8 parts, got {parts.Length}): '{line}'");
                                skippedLines++;
                            }
                        }

                        Logger.Info($"Processed {fileLineCount} lines from {Path.GetFileName(filePath)}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error reading spawn stats file {Path.GetFileName(filePath)}", ex);
                    }
                }

                Logger.Info($"Spawn stats summary - Total lines: {totalLines}, Successfully parsed: {successfulLines}, Skipped/errors: {skippedLines}, Spawn events loaded: {SpawnStats.Count}");
            }
            else
            {
                Logger.Warning($"STATS folder not found at: {statsFolderPath}");
            }
        }
    }

    public class SpawnStatData
    {
        public string PlayerName { get; set; } = "";
        public int PlayerX { get; set; }
        public int PlayerY { get; set; }
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }
        public string CreatureName { get; set; } = "";
        public int ColorR { get; set; }
        public int ColorG { get; set; }
        public int ColorB { get; set; }
        public int TotalDotsForPlayer { get; set; }
    }
}
