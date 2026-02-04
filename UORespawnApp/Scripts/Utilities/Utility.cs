namespace UORespawnApp
{
    public enum GameMap
    {
        Map0, // Felucca
        Map1, // Trammel
        Map2, // Ilshenar
        Map3, // Malas
        Map4, // Tokuno
        Map5  // Termur
    }

    public enum Frequency
    {
        Common,
        UnCommon,
        Rare
    }

    internal static class Utility
    {
        internal const string Version = "2.0.0.1";

        internal static Session? SESSION { get; private set; }

        internal static void StartSession(Session session)
        {
            SESSION = session;
        }

        internal static readonly string SpawnFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_Spawn.csv");

        internal static readonly string ChanceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_SpawnSettings.csv");

        internal static Dictionary<GameMap, List<SpawnEntity>> Spawns { get; private set; } = [];

        internal static void InitializeSpawnDictionary()
        {
            foreach (GameMap map in Enum.GetValues<GameMap>())
            {
                Spawns[map] = [];
            }
        }

        internal static void AddSpawn(GameMap map, SpawnEntity entity)
        {
            if (!Spawns.TryGetValue(map, out List<SpawnEntity>? value))
            {
                value = [];
                Spawns[map] = value;
            }

            if (!value.Contains(entity))
            {
                value.Add(entity);
            }
        }

        internal static void SaveSpawnData()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
                }

                if (string.IsNullOrEmpty(Settings.ServUODataFolder) || !Directory.Exists(Settings.ServUODataFolder))
                {
                    SpawnSave(SpawnFile);
                }
                else
                {
                    SpawnSave(SpawnFile);
                    SpawnSave(Path.Combine(Settings.ServUODataFolder, "UOR_Spawn.csv"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving spawn data: {ex.Message}");
            }
            finally
            {
                SaveChanceData();
            }
        }

        private static void SpawnSave(string filePath)
        {
            using StreamWriter streamWriter = new(filePath);

            foreach (var entry in Spawns)
            {
                var mapID = (int)entry.Key;

                var spawnEntities = entry.Value.Select(entity =>
                {
                    var commonSpawnStr = string.Join("*", entity.CommonSpawnList);
                    var unCommonSpawnStr = string.Join("*", entity.UnCommonSpawnList);
                    var rareSpawnStr = string.Join("*", entity.RareSpawnList);
                    return $"{entity.Position},{entity.TimedSpawn},{entity.SpawnBox.X},{entity.SpawnBox.Y},{entity.SpawnBox.Width},{entity.SpawnBox.Height}|{commonSpawnStr}|{unCommonSpawnStr}|{rareSpawnStr}";
                });

                streamWriter.WriteLine($"{mapID}:{string.Join(";", spawnEntities)}");
            }
        }

        internal static void LoadSpawnData()
        {
            try
            {
                if (File.Exists(SpawnFile))
                {
                    Spawns.Clear();

                    using var streamReader = new StreamReader(SpawnFile);

                    while (!streamReader.EndOfStream)
                    {
                        var line = streamReader.ReadLine();

                        var parts = line?.Split(':');

                        if (parts?.Length == 2)
                        {
                            var map = (GameMap)IsValidMapID(parts[0]);

                            var spawnEntities = parts[1].Split(';');

                            var entities = new List<SpawnEntity>();

                            foreach (var spawnEntity in spawnEntities)
                            {
                                var details = spawnEntity.Split('|');

                                if (details.Length == 4)
                                {
                                    var entityDetails = details[0].Split(',');

                                    if (entityDetails.Length == 6)
                                    {
                                        var position = int.Parse(entityDetails[0]);
                                        var timed = entityDetails[1];
                                        var x = int.Parse(entityDetails[2]);
                                        var y = int.Parse(entityDetails[3]);
                                        var width = int.Parse(entityDetails[4]);
                                        var height = int.Parse(entityDetails[5]);

                                        // Filter out empty strings when loading - split on empty sections creates [""]
                                        var commonSpawnList = details[1].Split('*')
                                            .Where(s => !string.IsNullOrWhiteSpace(s))
                                            .ToList();
                                        var unCommonSpawnList = details[2].Split('*')
                                            .Where(s => !string.IsNullOrWhiteSpace(s))
                                            .ToList();
                                        var rareSpawnList = details[3].Split('*')
                                            .Where(s => !string.IsNullOrWhiteSpace(s))
                                            .ToList();

                                        var spawnBox = new Microsoft.Maui.Graphics.Rect(x, y, width, height);

                                        var spawnEntityObject = new SpawnEntity
                                        {
                                            Position = position,
                                            TimedSpawn = timed,
                                            SpawnBox = spawnBox,
                                            CommonSpawnList = commonSpawnList,
                                            UnCommonSpawnList = unCommonSpawnList,
                                            RareSpawnList = rareSpawnList
                                        };

                                        entities.Add(spawnEntityObject);
                                    }
                                }
                            }

                            Spawns.Add(map, entities);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading spawn data: {ex.Message}");
            }
        }

        internal static void SaveChanceData()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
                }

                if (string.IsNullOrEmpty(Settings.ServUODataFolder) || !Directory.Exists(Settings.ServUODataFolder))
                {
                    SettingsSave(ChanceFile);
                }
                else
                {
                    SettingsSave(ChanceFile);
                    SettingsSave(Path.Combine(Settings.ServUODataFolder, "UOR_SpawnSettings.csv"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving chance data: {ex.Message}");
            }
        }

        private static void SettingsSave(string filePath)
        {
            using StreamWriter streamWriter = new(filePath);

            streamWriter.WriteLine($"MaxMob:{Settings.MaxMob}");
            streamWriter.WriteLine($"MinRange:{Settings.MinRange}");
            streamWriter.WriteLine($"MaxRange:{Settings.MaxRange}");
            streamWriter.WriteLine($"MaxCrowd:{Settings.MaxCrowd}");
            streamWriter.WriteLine($"WaterChance:{Settings.WaterChance}");
            streamWriter.WriteLine($"WeatherChance:{Settings.WeatherChance}");
            streamWriter.WriteLine($"StaticChance:{Settings.StaticChance}");
            streamWriter.WriteLine($"IsScaleSpawn:{Settings.IsScaleSpawn}");
            streamWriter.WriteLine($"Creature:{Settings.CreatureChance}");
            streamWriter.WriteLine($"Common:{Settings.CommonChance}");
            streamWriter.WriteLine($"UnCommon:{Settings.UnCommonChance}");
            streamWriter.WriteLine($"Rare:{Settings.RareChance}");
            streamWriter.WriteLine($"RiftSpawn:{Settings.EnableRiftSpawn}");
            streamWriter.WriteLine($"DebugSpawn:{Settings.EnableDebugSpawn}");
            streamWriter.WriteLine($"Version:{Version}");
        }

        internal static string GetMapImagePath()
        {
            if (SESSION != null)
            {
                string path = "";
                switch (SESSION.Current_Map)
                {
                    case GameMap.Map0: path = "maps/Map0.bmp"; break;
                    case GameMap.Map1: path = "maps/Map1.bmp"; break;
                    case GameMap.Map2: path = "maps/Map2.bmp"; break;
                    case GameMap.Map3: path = "maps/Map3.bmp"; break;
                    case GameMap.Map4: path = "maps/Map4.bmp"; break;
                    case GameMap.Map5: path = "maps/Map5.bmp"; break;
                }
                return path;
            }

            return "";
        }

        public static int IsValidMapID(string name)
        {
            if (int.TryParse(name, out int id))
            {
                return id;
            }

            if (name == "Felucca" || name == "Map0") return 0;
            if (name == "Trammel" || name == "Map1") return 1;
            if (name == "Ilshenar" || name == "Map2") return 2;
            if (name == "Malas" || name == "Map3") return 3;
            if (name == "Tokuno" || name == "Map4") return 4;
            if (name == "TerMur" || name == "Map5") return 5;

            return 0;
        }
    }
}