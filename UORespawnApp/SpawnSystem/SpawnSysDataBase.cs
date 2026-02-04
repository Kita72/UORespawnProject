using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

namespace Server.Custom.SpawnSystem
{
    internal static class SpawnSysDataBase
    {
        private const string Version = "2.0.0.1";

        private static readonly string WorldSpawnFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_WorldSpawn.csv");

        private static readonly string StaticSpawnFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_StaticSpawn.csv");

        private static readonly string SpawnFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_Spawn.csv");

        private static readonly string ChanceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_SpawnSettings.csv");

        internal static List<WorldEntity> WorldSpawns { get; private set; } = new List<WorldEntity>();

        internal static List<StaticEntity> StaticSpawns { get; private set; } = new List<StaticEntity>();

        internal static Dictionary<Map, List<SpawnEntity>> Spawns { get; private set; } = new Dictionary<Map, List<SpawnEntity>>();

        //General
        internal static int MaxMobs { get; private set; } = 15;
        internal static int MinRange { get; private set; } = 10;
        internal static int MaxRange { get; private set; } = 50;
        internal static int MaxCrowd { get; private set; } = 1;
        internal static double WaterChance { get; private set; } = 0.5;
        internal static double WeatherChance { get; private set; } = 0.1;
        internal static double StaticChance { get; private set; } = 0.1;
        internal static bool ScaleSpawn { get; private set; } = false;

        //Spawns
        internal static double CreatureChance { get; private set; } = 0.1;
        internal static double CommonChance { get; private set; } = 1.0;
        internal static double UnCommonChance { get; private set; } = 0.5;
        internal static double RareChance { get; private set; } = 0.1;
        internal static bool EnableRiftSpawn { get; private set; } = false;
        internal static bool EnableDebugSpawn { get; set; } = false;

        internal static void AddWorldEntity(WorldEntity entity)
        {
            var spawn = WorldSpawns.Find(we => we.MapHandle == entity.MapHandle);

            if (spawn == null)
            {
                WorldSpawns.Add(entity);
            }
        }

        internal static void LoadSpawns()
        {
            LoadWorldSpawn();

            LoadStaticSpawn();

            LoadSpawnData();

            LoadSpawnSettings();

            SpawnSysUtility.SendConsoleMsg(ConsoleColor.Yellow, "Spawn Loaded...");
        }

        internal static void ReLoadSpawns()
        {
            WorldSpawns.Clear();

            StaticSpawns.Clear();

            Spawns.Clear();

            LoadWorldSpawn();

            LoadStaticSpawn();

            LoadSpawnData();

            LoadSpawnSettings();
        }

        internal static void LoadWorldSpawn()
        {
            try
            {
                if (File.Exists(WorldSpawnFile))
                {
                    if (WorldSpawns.Count < 6)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            _ = new WorldEntity(Map.Maps[i]);
                        }
                    }

                    var lines = File.ReadLines(WorldSpawnFile).ToArray();

                    WorldEntity currentEntity = null;

                    foreach (var line in lines)
                    {
                        var parts = line.Split(',');

                        if (parts.Length == 2) 
                        {
                            var mapHandle = Map.Maps[IsValidMapID(parts[0])];

                            currentEntity = WorldSpawns.Find(e => e.MapHandle == mapHandle);
                            
                        }
                        else
                        {
                            parts = line.Split('|');

                            var tile = (WorldTile)Enum.Parse(typeof(WorldTile), parts[0]);

                            var spawnDetails = parts[1].Split('*');

                            foreach (var spawnDetail in spawnDetails)
                            {
                                var spawnParts = spawnDetail.Split(':');

                                if (spawnParts.Length >= 3)
                                {
                                    var name = spawnParts[0];
                                    var freq = (Frequency)Enum.Parse(typeof(Frequency), spawnParts[1]);
                                    var isMob = bool.Parse(spawnParts[2]);
                                    var tileEntity = new TileEntity(freq, name, isMob);

                                    currentEntity?.AddSpawn(tile, tileEntity);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkRed, $"Loading World Spawn Error: {ex.Message}");
            }
        }

        private static int IsValidMapID(string name)
        {
            if (int.TryParse(name, out int id))
            {
                return id;
            }

            if (name == "Felucca") return 0;
            if (name == "Trammel") return 1;
            if (name == "Ilshenar") return 2;
            if (name == "Malas") return 3;
            if (name == "Tokuno") return 4;
            if (name == "TerMur") return 5;

            return 0;
        }

        internal static void LoadStaticSpawn()
        {
            try
            {
                if (File.Exists(StaticSpawnFile))
                {
                    StaticSpawns = new List<StaticEntity>();

                    var lines = File.ReadLines(StaticSpawnFile).ToArray();

                    for (int index = 0; index < lines.Length;)
                    {
                        var parts = lines[index].Split(',');

                        if (parts.Length >= 2)
                        {
                            var staticName = parts[0];

                            var spawnCount = int.Parse(parts[1]);

                            List<(Frequency freq, string name)> spawn = new List<(Frequency freq, string name)>();

                            for (int i = 0; i < spawnCount; i++)
                            {
                                index++;

                                var lineParts = lines[index].Split(',');

                                if (lineParts.Length == 2)
                                {
                                    if (Enum.TryParse(lineParts[0], out Frequency freq))
                                    {
                                        spawn.Add((freq, lineParts[1]));
                                    }
                                }
                            }

                            StaticSpawns.Add(new StaticEntity(staticName, spawn));
                        }

                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkRed, $"Loading Static Spawn Error: {ex.Message}");
            }
        }

        internal static void LoadSpawnData()
        {
            try
            {
                if (File.Exists(SpawnFile))
                {
                    Spawns.Clear();

                    using (var streamReader = new StreamReader(SpawnFile))
                    {
                        while (!streamReader.EndOfStream)
                        {
                            var line = streamReader.ReadLine();

                            var parts = line?.Split(':');

                            if (parts?.Length == 2)
                            {
                                var map = Map.Maps[IsValidMapID(parts[0])];

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

                                            var spawnBox = new Rectangle(x, y, width, height);

                                            var commonSpawnList = details[1].Split('*').ToList();
                                            var unCommonSpawnList = details[2].Split('*').ToList();
                                            var rareSpawnList = details[3].Split('*').ToList();

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
            }
            catch (Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkRed, $"Loading Spawn Data Error: {ex.Message}");
            }
        }

        internal static void LoadSpawnSettings()
        {
            try
            {
                if (File.Exists(ChanceFile))
                {
                    var lines = File.ReadLines(ChanceFile).ToArray();

                    if (lines.Length == 15)
                    {
                        if (int.TryParse(lines[0].Split(':').Last(), out int val))
                        {
                            MaxMobs = val;
                        }

                        if (int.TryParse(lines[1].Split(':').Last(), out val))
                        {
                            MinRange = val;
                        }

                        if (int.TryParse(lines[2].Split(':').Last(), out val))
                        {
                            MaxRange = val;
                        }

                        if (int.TryParse(lines[3].Split(':').Last(), out val))
                        {
                            MaxCrowd = val;
                        }

                        if (double.TryParse(lines[4].Split(':').Last(), out double chance))
                        {
                            WaterChance = chance;
                        }

                        if (double.TryParse(lines[5].Split(':').Last(), out chance))
                        {
                            WeatherChance = chance;
                        }

                        if (double.TryParse(lines[6].Split(':').Last(), out chance))
                        {
                            StaticChance = chance;
                        }

                        if (bool.TryParse(lines[7].Split(':').Last(), out bool enable))
                        {
                            ScaleSpawn = enable;
                        }

                        if (double.TryParse(lines[8].Split(':').Last(), out chance))
                        {
                            CreatureChance = chance;
                        }

                        if (double.TryParse(lines[9].Split(':').Last(), out chance))
                        {
                            CommonChance = chance;
                        }

                        if (double.TryParse(lines[10].Split(':').Last(), out chance))
                        {
                            UnCommonChance = chance;
                        }

                        if (double.TryParse(lines[11].Split(':').Last(), out chance))
                        {
                            RareChance = chance;
                        }

                        if (bool.TryParse(lines[12].Split(':').Last(), out enable))
                        {
                            EnableRiftSpawn = enable;
                        }

                        if (bool.TryParse(lines[13].Split(':').Last(), out enable))
                        {
                            EnableDebugSpawn = enable;
                        }

                        string version = lines[14].Split(':').Last();

                        if (!string.IsNullOrEmpty(version))
                        {
                            if (Version != version)
                            {
                                SpawnSysUtility.SendConsoleMsg(ConsoleColor.Yellow, "Update Needed => Update Scripts!");
                            }
                        }

                        SpawnSysSettings.InitializeStats();
                    }
                }
            }
            catch (Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkRed, $"Loading Spawn Chance Error: {ex.Message}");
            }
        }

        internal static Frequency GetFreq(double chance)
        {
            if (chance <= RareChance)
            {
                return Frequency.Rare;
            }

            if (chance <= UnCommonChance)
            {
                return Frequency.UnCommon;
            }

            return Frequency.Common;
        }
    }
}
