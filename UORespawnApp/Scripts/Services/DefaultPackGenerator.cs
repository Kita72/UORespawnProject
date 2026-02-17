using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    /// <summary>
    /// Generates the Default Spawn Pack binary files based on the UOR_Ecology.txt configuration.
    /// Creates tile spawns for all 32 tile types across all 6 maps, and region spawns for
    /// major archetypes (Fel/Tram share regions, dungeons, expansions).
    /// </summary>
    public static class DefaultPackGenerator
    {
        private const int TILE_SPAWN_VERSION = 1;
        private const int REGION_SPAWN_VERSION = 1;
        private const int BOX_SPAWN_VERSION = 1;
        private const int SETTINGS_VERSION = 1;
        private const string APP_VERSION = "2.0.0.3";

        /// <summary>
        /// Generates all binary files for the DefaultPack if they don't exist.
        /// Called on app startup to ensure the default pack is available.
        /// </summary>
        public static void GenerateIfNeeded()
        {
            var packFolder = Path.Combine(PathConstants.PacksApprovedPath, "DefaultPack");
            
            if (!Directory.Exists(packFolder))
            {
                Directory.CreateDirectory(packFolder);
            }

            var tileFile = Path.Combine(packFolder, PathConstants.TILE_FILENAME);
            var regionFile = Path.Combine(packFolder, PathConstants.REGION_FILENAME);
            var boxFile = Path.Combine(packFolder, PathConstants.BOX_FILENAME);
            var settingsFile = Path.Combine(packFolder, PathConstants.SETTINGS_FILENAME);

            // Generate files if any are missing
            if (!File.Exists(tileFile) || !File.Exists(regionFile) || 
                !File.Exists(boxFile) || !File.Exists(settingsFile))
            {
                Logger.Info("Generating DefaultPack binary files...");
                
                GenerateTileSpawns(tileFile);
                GenerateRegionSpawns(regionFile);
                GenerateEmptyBoxSpawns(boxFile);
                GenerateDefaultSettings(settingsFile);
                
                Logger.Info("DefaultPack binary files generated successfully.");
            }
        }

        #region Tile Spawns - 32 Types x 6 Maps

        private static void GenerateTileSpawns(string filePath)
        {
            var tileData = GetTileSpawnData();
            
            using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
            
            writer.Write(TILE_SPAWN_VERSION);
            writer.Write(APP_VERSION);
            
            // Write for all 6 maps (0-5: Felucca, Trammel, Ilshenar, Malas, Tokuno, Ter Mur)
            writer.Write(6); // mapCount
            
            for (int mapId = 0; mapId < 6; mapId++)
            {
                writer.Write(mapId);
                writer.Write(MapUtility.GetMapName(mapId));
                writer.Write(tileData.Count);
                
                int tileId = mapId * 100; // Unique IDs per map: 0-99, 100-199, etc.
                
                foreach (var tile in tileData)
                {
                    WriteTileSpawnEntity(writer, tileId++, tile.Name, mapId, tile);
                }
            }
            
            Logger.Info($"Generated {tileData.Count * 6} tile spawn entries (32 tiles x 6 maps)");
        }

        private static void WriteTileSpawnEntity(BinaryWriter writer, int id, string name, int mapId, TileConfig config)
        {
            writer.Write(id);
            writer.Write(name);
            writer.Write(mapId);
            writer.Write((int)config.WeatherTrigger);
            writer.Write((int)config.TimedTrigger);

            WriteStringList(writer, config.Water);
            WriteStringList(writer, config.Weather);
            WriteStringList(writer, config.Timed);
            WriteStringList(writer, config.Common);
            WriteStringList(writer, config.Uncommon);
            WriteStringList(writer, config.Rare);
        }

        /// <summary>
        /// Returns all 32 tile types with their ecology-based spawn configurations.
        /// Config format from UOR_Ecology.txt: 'Name - Common - Uncommon - Rare - Water - Weather x4 - Timed x8'
        /// </summary>
        private static List<TileConfig> GetTileSpawnData()
        {
            return
            [
                // ===== NATURAL TERRAINS =====
                new TileConfig("grass",
                    common: ["Sheep", "Cow", "Goat", "Pig", "Chicken"],
                    uncommon: ["Hind", "TimberWolf", "Mongbat", "Boar"],
                    rare: ["GreatHart", "BlackBear", "GrizzlyBear"],
                    water: ["BullFrog"],
                    weather: ["BullFrog", "Slime", "GiantToad", "Sewerrat"],
                    timed: ["Cow", "Sheep", "Bird", "Bird", "TimberWolf", "Mongbat", "Shade", "Zombie"]),

                new TileConfig("forest",
                    common: ["Bird", "TimberWolf", "Rabbit", "Squirrel"],
                    uncommon: ["BlackBear", "GrizzlyBear", "Cougar", "GreatHart"],
                    rare: ["Reaper", "Ettin", "MLDryad", "Treefellow"],
                    water: ["BullFrog"],
                    weather: ["GiantToad", "Slime", "FrostSpider", "IceSnake"],
                    timed: ["Bird", "Bird", "TimberWolf", "GrizzlyBear", "DireWolf", "Reaper", "Shade", "Wisp"]),

                new TileConfig("jungle",
                    common: ["Snake", "TropicalBird", "Gorilla", "Gorilla"],
                    uncommon: ["GiantSerpent", "Alligator", "Lizardman"],
                    rare: ["SilverSerpent", "RottingCorpse", "PredatorHellCat"],
                    water: ["GiantToad"],
                    weather: ["BullFrog", "PoisonElemental", "IceSerpent", "IceElemental"],
                    timed: ["TropicalBird", "Gorilla", "Snake", "Alligator", "SilverSerpent", "RottingCorpse", "BogThing", "SwampTentacle"]),

                new TileConfig("sand",
                    common: ["Scorpion", "Snake", "Lizardman", "DesertScorpion"],
                    uncommon: ["EarthElemental", "Raptor", "Orc"],
                    rare: ["SandVortex", "Efreet", "Mummy", "StoneGargoyle"],
                    water: ["SeaSerpent"],
                    weather: ["DesertScorpion", "SandVortex", "IceSnake", "FrostSpider"],
                    timed: ["Scorpion", "Snake", "Raptor", "EarthElemental", "BoneKnight", "Mummy", "Lich", "Spectre"]),

                new TileConfig("snow",
                    common: ["PolarBear", "Walrus", "SnowLeopard", "FrostSpider"],
                    uncommon: ["FrostSpider", "IceSnake", "IceElemental"],
                    rare: ["WhiteWyrm", "IceFiend", "FrostTroll"],
                    water: ["SeaSerpent"],
                    weather: ["Walrus", "FrostTroll", "SnowElemental", "IceFiend"],
                    timed: ["PolarBear", "Walrus", "SnowLeopard", "FrostSpider", "IceElemental", "IceFiend", "WhiteWyrm", "FrostTroll"],
                    weatherTrigger: WeatherTypes.Snow,
                    timedTrigger: TimeNames.Witching_Hour),

                new TileConfig("swamp",
                    common: ["BullFrog", "Slime", "Rat", "Bogling"],
                    uncommon: ["Lizardman", "GiantRat", "Alligator"],
                    rare: ["PlagueBeast", "BogThing", "Raptor", "PlagueSpawn"],
                    water: ["SwampTentacle"],
                    weather: ["GiantToad", "AcidElemental", "FrostOoze", "IceElemental"],
                    timed: ["BullFrog", "Lizardman", "Alligator", "Bogling", "PlagueBeast", "BogThing", "PlagueSpawn", "Raptor"],
                    weatherTrigger: WeatherTypes.Storm,
                    timedTrigger: TimeNames.Middle_of_Night),

                new TileConfig("cave",
                    common: ["GiantRat", "Mongbat", "Slime", "EarthElemental"],
                    uncommon: ["GiantSpider", "Orc", "Scorpion"],
                    rare: ["AgapiteElemental", "DullCopperElemental", "ShadowIronElemental"],
                    water: ["WaterElemental"],
                    weather: ["Slime", "EarthElemental", "FrostSpider", "IceElemental"],
                    timed: ["GiantRat", "Mongbat", "EarthElemental", "Orc", "GiantSpider", "AgapiteElemental", "ShadowIronElemental", "DullCopperElemental"]),

                new TileConfig("dirt",
                    common: ["Horse", "Llama", "Chicken", "Dog"],
                    uncommon: ["Orc", "Brigand", "Ettin", "Ratman"],
                    rare: ["Ogre", "Troll", "OrcCaptain"],
                    water: ["EarthElemental"],
                    weather: ["Slime", "EarthElemental", "FrostOoze", "IceSnake"],
                    timed: ["Horse", "Llama", "Brigand", "Orc", "Ogre", "Troll", "Zombie", "Skeleton"]),

                new TileConfig("furrows",
                    common: ["Rabbit", "Bird", "Sheep", "Pig"],
                    uncommon: ["Goat", "Snake", "GiantRat"],
                    rare: ["Mongbat", "Corpser", "DireWolf"],
                    water: ["BullFrog"],
                    weather: ["EarthElemental", "Slime", "FrostSpider", "IceSnake"],
                    timed: ["Rabbit", "Bird", "Goat", "Pig", "Snake", "GiantRat", "Zombie", "Skeleton"]),

                new TileConfig("mountain",
                    common: ["MountainGoat", "Eagle", "Snake", "Llama"],
                    uncommon: ["Orc", "Ettin", "EarthElemental", "Harpy"],
                    rare: ["OgreLord", "Wyvern", "StoneHarpy", "Cyclops"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "Eagle", "SnowElemental", "IceFiend"],
                    timed: ["MountainGoat", "Eagle", "Orc", "Ettin", "OgreLord", "Wyvern", "StoneHarpy", "Gargoyle"]),

                new TileConfig("water_shallow",
                    common: ["Walrus", "BullFrog", "Dolphin", "Walrus"],
                    uncommon: ["SeaSerpent", "WaterElemental", "GiantSerpent"],
                    rare: ["DeepSeaSerpent", "Kraken", "WaterElemental"],
                    water: ["WaterElemental"],
                    weather: ["WaterElemental", "AirElemental", "IceElemental", "SeaSerpent"],
                    timed: ["Walrus", "Walrus", "SeaSerpent", "WaterElemental", "DeepSeaSerpent", "Kraken", "WaterElemental", "SeaSerpent"]),

                new TileConfig("water_deep",
                    common: ["Dolphin", "SeaSerpent", "WaterElemental"],
                    uncommon: ["DeepSeaSerpent", "WaterElemental", "SeaSerpent"],
                    rare: ["Kraken", "Leviathan", "SeaSerpent"],
                    water: ["WaterElemental"],
                    weather: ["SeaSerpent", "WaterElemental", "IceElemental", "Kraken"],
                    timed: ["Dolphin", "SeaSerpent", "DeepSeaSerpent", "WaterElemental", "Kraken", "Leviathan", "SeaSerpent", "DeepSeaSerpent"]),

                // ===== CONSTRUCTED & CIVILIZED =====
                new TileConfig("flagstone",
                    common: ["Cat", "Dog", "Bird", "Rat"],
                    uncommon: ["Sewerrat", "GiantRat", "Mongbat"],
                    rare: ["Brigand", "Executioner", "Executioner"],
                    water: ["Slime"],
                    weather: ["Rat", "Sewerrat", "FrostOoze", "IceSnake"],
                    timed: ["Cat", "Dog", "Rat", "Sewerrat", "Mongbat", "Brigand", "Shade", "Spectre"]),

                new TileConfig("brick",
                    common: ["Skeleton", "Zombie", "Rat", "HeadlessOne"],
                    uncommon: ["Lizardman", "Orc", "Ettin", "Wraith"],
                    rare: ["Lich", "Daemon", "EvilMage", "BoneKnight"],
                    water: ["WaterElemental"],
                    weather: ["Slime", "PoisonElemental", "FrostOoze", "IceFiend"],
                    timed: ["Skeleton", "Zombie", "Lizardman", "Orc", "Lich", "Daemon", "BoneKnight", "SkeletalMage"]),

                new TileConfig("wood_plank",
                    common: ["Rat", "Bird", "Cat", "Dog"],
                    uncommon: ["Brigand", "GiantRat", "Mongbat"],
                    rare: ["Executioner", "Executioner", "EvilMage"],
                    water: ["WaterElemental"],
                    weather: ["Rat", "Slime", "FrostOoze", "IceSnake"],
                    timed: ["Rat", "Bird", "Brigand", "GiantRat", "Executioner", "Executioner", "Shade", "Spectre"]),

                new TileConfig("marble",
                    common: ["Bird", "Cat", "Rat", "Mongbat"],
                    uncommon: ["Gazer", "Gargoyle", "StoneGargoyle"],
                    rare: ["Lich", "TerathanWarrior", "OphidianMage"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "PoisonElemental", "SnowElemental", "IceFiend"],
                    timed: ["Bird", "Mongbat", "Gazer", "Gargoyle", "Lich", "TerathanWarrior", "OphidianMage", "Succubus"]),

                new TileConfig("stone_moss",
                    common: ["Snake", "Slime", "Rat", "Mongbat"],
                    uncommon: ["Lizardman", "Harpy", "Mongbat", "Bogling"],
                    rare: ["StoneHarpy", "Gazer", "ElderGazer", "RottingCorpse"],
                    water: ["SwampTentacle"],
                    weather: ["PoisonElemental", "AcidElemental", "FrostSpider", "IceSnake"],
                    timed: ["Snake", "Slime", "Lizardman", "Harpy", "StoneHarpy", "Gazer", "RottingCorpse", "Lich"]),

                new TileConfig("sandstone",
                    common: ["Scorpion", "Snake", "Lizardman", "Rat"],
                    uncommon: ["EarthElemental", "Orc", "Ettin", "OrcScout"],
                    rare: ["StoneGargoyle", "FireElemental", "Efreet", "OrcBrute"],
                    water: ["SandVortex"],
                    weather: ["AirElemental", "SandVortex", "IceElemental", "FrostSpider"],
                    timed: ["Scorpion", "Snake", "EarthElemental", "Orc", "StoneGargoyle", "FireElemental", "Efreet", "Mummy"]),

                new TileConfig("gravel",
                    common: ["Horse", "Llama", "Dog", "Rat"],
                    uncommon: ["TimberWolf", "Brigand", "Orc", "EarthElemental"],
                    rare: ["Ettin", "Troll", "Ogre", "AgapiteElemental"],
                    water: ["EarthElemental"],
                    weather: ["Slime", "EarthElemental", "FrostOoze", "IceSnake"],
                    timed: ["Horse", "TimberWolf", "Brigand", "Orc", "Ettin", "Troll", "Zombie", "Skeleton"]),

                new TileConfig("embank",
                    common: ["Eagle", "MountainGoat", "Snake", "Bird"],
                    uncommon: ["Harpy", "StoneHarpy", "Mongbat", "Gargoyle"],
                    rare: ["Wyvern", "Drake", "Drake", "StoneGargoyle"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "Eagle", "SnowElemental", "IceFiend"],
                    timed: ["Eagle", "Harpy", "StoneHarpy", "Wyvern", "Drake", "Drake", "Gargoyle", "Daemon"]),

                // ===== EXOTIC, MAGICAL & ALIEN =====
                new TileConfig("obsidian",
                    common: ["LavaLizard", "FireElemental", "HellHound", "Imp"],
                    uncommon: ["Daemon", "FireGargoyle", "Efreet"],
                    rare: ["Balron", "Succubus", "LichLord", "Nightmare"],
                    water: ["LavaSerpent"],
                    weather: ["FireElemental", "FireElemental", "IceFiend", "WhiteWyrm"],
                    timed: ["LavaLizard", "FireElemental", "Daemon", "Imp", "Balron", "Succubus", "LichLord", "SkeletalKnight"],
                    weatherTrigger: WeatherTypes.Storm,
                    timedTrigger: TimeNames.Witching_Hour),

                new TileConfig("void",
                    common: ["ShadowWisp", "ShadowWisp", "Wisp", "ShadowWisp"],
                    uncommon: ["WandererOfTheVoid", "WandererOfTheVoid", "WandererOfTheVoid", "ShadowWisp"],
                    rare: ["VoidManifestation", "VoidManifestation", "VoidManifestation", "VoidManifestation"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "PoisonElemental", "IceElemental", "IceFiend"],
                    timed: ["ShadowWisp", "ShadowWisp", "WandererOfTheVoid", "WandererOfTheVoid", "VoidManifestation", "VoidManifestation", "VoidManifestation", "VoidManifestation"],
                    weatherTrigger: WeatherTypes.Storm,
                    timedTrigger: TimeNames.Middle_of_Night),

                new TileConfig("acid",
                    common: ["Slime", "AcidSlug", "BullFrog", "Rat"],
                    uncommon: ["AcidElemental", "PlagueBeast", "BogThing", "Bogling"],
                    rare: ["PlagueBeastLord", "Changeling", "Hydra", "FetidEssence"],
                    water: ["PoisonElemental"],
                    weather: ["PoisonElemental", "AcidElemental", "FrostOoze", "IceElemental"],
                    timed: ["Slime", "AcidElemental", "PlagueBeast", "PlagueBeastLord", "BogThing", "Changeling", "Hydra", "FetidEssence"]),

                new TileConfig("blood",
                    common: ["Mongbat", "HeadlessOne", "Rat", "VampireBat"],
                    uncommon: ["BloodElemental", "FleshGolem", "GoreFiend", "EvilMage"],
                    rare: ["RottingCorpse", "Lich", "Succubus", "BoneKnight"],
                    water: ["BloodElemental"],
                    weather: ["BloodElemental", "PoisonElemental", "FrostOoze", "IceFiend"],
                    timed: ["HeadlessOne", "BloodElemental", "FleshGolem", "RottingCorpse", "Lich", "Succubus", "BoneKnight", "SkeletalMage"]),

                new TileConfig("cloud",
                    common: ["Bird", "Eagle", "Harpy", "Mongbat"],
                    uncommon: ["AirElemental", "Wisp", "Gargoyle", "StoneHarpy"],
                    rare: ["StoneHarpy", "Efreet", "ElderGazer", "ElderGazer"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "Eagle", "SnowElemental", "IceFiend"],
                    timed: ["Bird", "AirElemental", "Wisp", "Gargoyle", "StoneHarpy", "Efreet", "ElderGazer", "ElderGazer"]),

                new TileConfig("crystal",
                    common: ["Wisp", "CrystalElemental", "SAPixie", "ShadowWisp"],
                    uncommon: ["CrystalLatticeSeeker", "UnfrozenMummy", "IceElemental", "IceElemental"],
                    rare: ["CrystalHydra", "CrystalDaemon", "MantraEffervescence", "EtherealWarrior"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "EnergyVortex", "IceElemental", "IceFiend"],
                    timed: ["Wisp", "CrystalElemental", "CrystalLatticeSeeker", "UnfrozenMummy", "CrystalHydra", "CrystalDaemon", "EtherealWarrior", "SerpentineDragon"],
                    weatherTrigger: WeatherTypes.Blizzard,
                    timedTrigger: TimeNames.Early_Morning),

                new TileConfig("mycelium",
                    common: ["GiantRat", "Slime", "BullFrog", "RedSolenWorker"],
                    uncommon: ["RedSolenWarrior", "RuneBeetle", "DreadSpider", "MyrmidexDrone"],
                    rare: ["RedSolenQueen", "MyrmidexWarrior", "TerathanMatriarch", "AcidElemental"],
                    water: ["WaterElemental"],
                    weather: ["PoisonElemental", "AcidElemental", "FrostSpider", "IceSnake"],
                    timed: ["GiantRat", "RedSolenWorker", "RedSolenWarrior", "RedSolenQueen", "DreadSpider", "MyrmidexDrone", "TerathanWarrior", "TerathanMatriarch"]),

                new TileConfig("shadow",
                    common: ["ShadowWisp", "Skeleton", "Zombie", "Spectre"],
                    uncommon: ["Spectre", "Shade", "Wraith", "BoneKnight"],
                    rare: ["Lich", "BoneKnight", "BoneMagi", "RottingCorpse"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "PoisonElemental", "IceFiend", "FrostSpider"],
                    timed: ["ShadowWisp", "Spectre", "Shade", "Lich", "BoneKnight", "BoneMagi", "RottingCorpse", "AncientLich"],
                    weatherTrigger: WeatherTypes.Storm,
                    timedTrigger: TimeNames.Middle_of_Night),

                new TileConfig("lava",
                    common: ["LavaLizard", "FireElemental", "HellHound", "FireAnt"],
                    uncommon: ["LavaSerpent", "FireGargoyle", "Imp", "Efreet"],
                    rare: ["Phoenix", "Balron", "Daemon", "Nightmare"],
                    water: ["LavaSerpent"],
                    weather: ["FireElemental", "FireElemental", "IceFiend", "WhiteWyrm"],
                    timed: ["LavaLizard", "FireElemental", "HellHound", "LavaSerpent", "Phoenix", "Balron", "Daemon", "Efreet"],
                    weatherTrigger: WeatherTypes.Storm,
                    timedTrigger: TimeNames.Witching_Hour),

                new TileConfig("ice_slippery",
                    common: ["Walrus", "PolarBear", "FrostOoze", "FrostSpider"],
                    uncommon: ["IceSnake", "FrostSpider", "IceElemental", "SnowElemental"],
                    rare: ["SnowElemental", "IceFiend", "WhiteWyrm", "ArcticOgreLord"],
                    water: ["SeaSerpent"],
                    weather: ["Walrus", "FrostTroll", "SnowElemental", "IceFiend"],
                    timed: ["Walrus", "IceSnake", "FrostSpider", "IceElemental", "SnowElemental", "IceFiend", "WhiteWyrm", "ArcticOgreLord"],
                    weatherTrigger: WeatherTypes.Blizzard,
                    timedTrigger: TimeNames.Witching_Hour),

                new TileConfig("ash",
                    common: ["HellHound", "Skeleton", "Zombie", "LavaLizard"],
                    uncommon: ["FireElemental", "LavaLizard", "Imp", "BoneKnight"],
                    rare: ["Daemon", "Balron", "Lich", "SkeletalDragon"],
                    water: ["LavaSerpent"],
                    weather: ["FireElemental", "AirElemental", "IceFiend", "FrostSpider"],
                    timed: ["HellHound", "FireElemental", "LavaLizard", "Daemon", "Balron", "Lich", "BoneKnight", "SkeletalDragon"]),

                new TileConfig("leaves",
                    common: ["Rabbit", "Bird", "Squirrel", "Ferret"],
                    uncommon: ["Snake", "TimberWolf", "Cougar", "SAPixie"],
                    rare: ["SAPixie", "MLDryad", "Satyr", "Reaper"],
                    water: ["BullFrog"],
                    weather: ["GiantToad", "Slime", "FrostSpider", "IceSnake"],
                    timed: ["Rabbit", "Snake", "TimberWolf", "SAPixie", "MLDryad", "Satyr", "Reaper", "Treefellow"])
            ];
        }

        #endregion

        #region Region Spawns - Archetypes for Fel/Tram + Expansions

        private static void GenerateRegionSpawns(string filePath)
        {
            var regionData = GetRegionSpawnData();
            
            using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
            
            writer.Write(REGION_SPAWN_VERSION);
            writer.Write(APP_VERSION);
            
            // Group regions by map
            var regionsByMap = new Dictionary<int, List<(string Name, RegionConfig Config)>>();
            
            foreach (var region in regionData)
            {
                foreach (var mapId in region.MapIds)
                {
                    if (!regionsByMap.ContainsKey(mapId))
                    {
                        regionsByMap[mapId] = [];
                    }
                    regionsByMap[mapId].Add((region.Name, region.Config));
                }
            }
            
            writer.Write(regionsByMap.Count); // mapCount
            
            int totalRegions = 0;
            
            foreach (var mapEntry in regionsByMap.OrderBy(kvp => kvp.Key))
            {
                writer.Write(mapEntry.Key);
                writer.Write(MapUtility.GetMapName(mapEntry.Key));
                writer.Write(mapEntry.Value.Count);
                
                int regionId = mapEntry.Key * 1000; // Unique IDs per map
                
                foreach (var (name, config) in mapEntry.Value)
                {
                    WriteRegionSpawnEntity(writer, regionId++, name, mapEntry.Key, config);
                    totalRegions++;
                }
            }
            
            Logger.Info($"Generated {totalRegions} region spawn entries across {regionsByMap.Count} maps");
        }

        private static void WriteRegionSpawnEntity(BinaryWriter writer, int id, string name, int mapId, RegionConfig config)
        {
            writer.Write(id);
            writer.Write(name);
            writer.Write(mapId);
            writer.Write((int)config.WeatherTrigger);
            writer.Write((int)config.TimedTrigger);

            WriteStringList(writer, config.Water);
            WriteStringList(writer, config.Weather);
            WriteStringList(writer, config.Timed);
            WriteStringList(writer, config.Common);
            WriteStringList(writer, config.Uncommon);
            WriteStringList(writer, config.Rare);
        }

        /// <summary>
        /// Returns regional spawn archetypes from UOR_Ecology.txt.
        /// Fel (0) and Tram (1) share most regions. Other maps have unique regions.
        /// </summary>
        private static List<RegionData> GetRegionSpawnData()
        {
            return
            [
                // ===== CIVILIZED WORLD - Fel/Tram (0,1) =====
                new RegionData("Britain", [0, 1], new RegionConfig(
                    common: ["Dog", "Cat", "Bird", "Rat"],
                    uncommon: ["Rat", "Sewerrat", "GiantRat", "Mongbat"],
                    rare: ["Brigand", "Slime", "GiantRat"],
                    water: ["WaterElemental"],
                    weather: ["Rat", "Slime", "FrostOoze", "IceSnake"],
                    timed: ["Dog", "Cat", "Rat", "Mongbat", "Brigand", "Shade", "Spectre", "Zombie"])),

                new RegionData("Yew", [0, 1], new RegionConfig(
                    common: ["Sheep", "Cow", "TimberWolf", "Bird"],
                    uncommon: ["Orc", "Ettin", "Reaper", "Mongbat"],
                    rare: ["Lich", "OgreLord", "ElderGazer", "Gazer"],
                    water: ["WaterElemental"],
                    weather: ["Slime", "PoisonElemental", "FrostSpider", "IceSnake"],
                    timed: ["Sheep", "Orc", "Ettin", "Reaper", "Lich", "OgreLord", "Shade", "Skeleton"])),

                new RegionData("Skara Brae", [0, 1], new RegionConfig(
                    common: ["Sheep", "Cow", "Bird", "Pig"],
                    uncommon: ["TimberWolf", "Mongbat", "GiantRat", "Snake"],
                    rare: ["Reaper", "Corpser", "EarthElemental"],
                    water: ["BullFrog"],
                    weather: ["GiantToad", "Slime", "FrostSpider", "IceSnake"],
                    timed: ["Sheep", "TimberWolf", "Mongbat", "Reaper", "Corpser", "Shade", "Spectre", "Wisp"])),

                new RegionData("Minoc", [0, 1], new RegionConfig(
                    common: ["Dog", "Cat", "PackHorse", "Goat"],
                    uncommon: ["EarthElemental", "Orc", "Ettin", "Troll"],
                    rare: ["Ogre", "Troll", "AgapiteElemental", "Golem"],
                    water: ["WaterElemental"],
                    weather: ["Slime", "EarthElemental", "SnowElemental", "IceFiend"],
                    timed: ["Dog", "EarthElemental", "Orc", "Ettin", "Ogre", "Troll", "AgapiteElemental", "Golem"])),

                new RegionData("Trinsic", [0, 1], new RegionConfig(
                    common: ["Dog", "Cat", "Horse", "Bird"],
                    uncommon: ["Lizardman", "Mongbat", "Snake", "GiantSpider"],
                    rare: ["Daemon", "DireWolf", "EvilMage"],
                    water: ["WaterElemental"],
                    weather: ["Slime", "EarthElemental", "FrostOoze", "IceSnake"],
                    timed: ["Dog", "Lizardman", "Mongbat", "Snake", "Daemon", "Shade", "Zombie", "Skeleton"])),

                new RegionData("Jhelom", [0, 1], new RegionConfig(
                    common: ["Bull", "Cow", "Sheep", "Goat"],
                    uncommon: ["Mongbat", "TimberWolf", "GreatHart", "Gazer"],
                    rare: ["Drake", "Drake", "Raptor", "Raptor"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "Eagle", "IceElemental", "SeaSerpent"],
                    timed: ["Bull", "Mongbat", "TimberWolf", "Drake", "Drake", "Shade", "Spectre", "Brigand"])),

                new RegionData("Moonglow", [0, 1], new RegionConfig(
                    common: ["Cat", "Dog", "Bird", "Cow"],
                    uncommon: ["Mongbat", "Wisp", "Gazer", "Imp"],
                    rare: ["ElderGazer", "EvilMage", "EtherealWarrior"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "Wisp", "IceElemental", "IceFiend"],
                    timed: ["Cat", "Mongbat", "Wisp", "Gazer", "ElderGazer", "Lich", "Shade", "Spectre"])),

                new RegionData("Magincia", [0, 1], new RegionConfig(
                    common: ["Dog", "Cat", "Rabbit", "Bird"],
                    uncommon: ["Mongbat", "Snake", "GiantRat", "Imp"],
                    rare: ["Daemon", "Gargoyle", "ArcaneDaemon"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "Wisp", "IceElemental", "IceFiend"],
                    timed: ["Dog", "Mongbat", "Snake", "Daemon", "Gargoyle", "Shade", "Spectre", "Lich"])),

                new RegionData("Vesper", [0, 1], new RegionConfig(
                    common: ["Cat", "Dog", "Bird", "Rat"],
                    uncommon: ["Sewerrat", "GiantRat", "Brigand", "Mongbat"],
                    rare: ["Brigand", "Executioner", "EvilMage"],
                    water: ["WaterElemental"],
                    weather: ["Rat", "Slime", "FrostOoze", "IceSnake"],
                    timed: ["Cat", "Dog", "Rat", "Brigand", "Executioner", "Shade", "Spectre", "Zombie"])),

                new RegionData("Cove", [0, 1], new RegionConfig(
                    common: ["Cat", "Dog", "Bird", "Pig"],
                    uncommon: ["Orc", "OrcScout", "Mongbat", "GiantRat"],
                    rare: ["OrcCaptain", "OrcBrute", "Ogre"],
                    water: ["WaterElemental"],
                    weather: ["Slime", "EarthElemental", "FrostOoze", "IceSnake"],
                    timed: ["Cat", "Orc", "OrcScout", "Mongbat", "OrcCaptain", "Ogre", "Shade", "Skeleton"])),

                new RegionData("Serpents Hold", [0, 1], new RegionConfig(
                    common: ["Dog", "Cat", "Bird", "Horse"],
                    uncommon: ["Snake", "GiantSerpent", "SilverSerpent", "Mongbat"],
                    rare: ["Drake", "Dragon", "Wyvern"],
                    water: ["SeaSerpent"],
                    weather: ["AirElemental", "PoisonElemental", "IceElemental", "IceFiend"],
                    timed: ["Dog", "Snake", "GiantSerpent", "SilverSerpent", "Drake", "Dragon", "Shade", "Spectre"])),

                new RegionData("Nujel'm", [0, 1], new RegionConfig(
                    common: ["Cat", "Dog", "Bird", "Rabbit"],
                    uncommon: ["Mongbat", "Snake", "Gazer", "Imp"],
                    rare: ["ElderGazer", "Daemon", "Succubus"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "Wisp", "IceElemental", "IceFiend"],
                    timed: ["Cat", "Mongbat", "Snake", "Gazer", "ElderGazer", "Daemon", "Shade", "Spectre"])),

                // ===== DUNGEONS - Fel/Tram (0,1) =====
                new RegionData("Dungeon Deceit", [0, 1], new RegionConfig(
                    common: ["Skeleton", "Zombie", "Mummy", "Ghoul"],
                    uncommon: ["SkeletalKnight", "BoneMagi", "Lich", "Wraith"],
                    rare: ["LichLord", "SilverSerpent", "RottingCorpse", "RottingCorpse"],
                    water: ["WaterElemental"],
                    weather: ["PoisonElemental", "AcidElemental", "FrostOoze", "IceFiend"],
                    timed: ["Skeleton", "SkeletalKnight", "BoneMagi", "Lich", "LichLord", "RottingCorpse", "AncientLich", "ShadowWyrm"])),

                new RegionData("Dungeon Despise", [0, 1], new RegionConfig(
                    common: ["Lizardman", "Ettin", "EarthElemental", "Rat"],
                    uncommon: ["Ogre", "Troll", "AcidElemental", "Orc"],
                    rare: ["OgreLord", "Titan", "Cyclops", "AcidElemental"],
                    water: ["WaterElemental"],
                    weather: ["Slime", "PoisonElemental", "FrostOoze", "IceFiend"],
                    timed: ["Lizardman", "Ettin", "Ogre", "OgreLord", "Titan", "Cyclops", "AcidElemental", "PlagueBeast"])),

                new RegionData("Dungeon Destard", [0, 1], new RegionConfig(
                    common: ["GiantRat", "Slime", "Drake", "Lizardman"],
                    uncommon: ["Wyvern", "Dragon", "WaterElemental", "GiantSerpent"],
                    rare: ["AncientWyrm", "ShadowWyrm", "GreaterDragon", "Drake"],
                    water: ["WaterElemental"],
                    weather: ["FireElemental", "AirElemental", "IceElemental", "IceFiend"],
                    timed: ["Drake", "Wyvern", "Dragon", "AncientWyrm", "ShadowWyrm", "GreaterDragon", "Lich", "Daemon"])),

                new RegionData("Dungeon Shame", [0, 1], new RegionConfig(
                    common: ["Scorpion", "EarthElemental", "DullCopperElemental", "Slime"],
                    uncommon: ["AirElemental", "FireElemental", "WaterElemental", "EarthElemental"],
                    rare: ["BloodElemental", "PoisonElemental", "ElderGazer", "ElderGazer"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "PoisonElemental", "IceElemental", "IceFiend"],
                    timed: ["Scorpion", "EarthElemental", "BloodElemental", "PoisonElemental", "AcidElemental", "ElderGazer", "ElderGazer", "Lich"])),

                new RegionData("Dungeon Wrong", [0, 1], new RegionConfig(
                    common: ["Skeleton", "Zombie", "Rat", "GiantRat"],
                    uncommon: ["BoneKnight", "SkeletalMage", "Lich", "Wraith"],
                    rare: ["LichLord", "AncientLich", "RottingCorpse", "Daemon"],
                    water: ["WaterElemental"],
                    weather: ["PoisonElemental", "AcidElemental", "FrostOoze", "IceFiend"],
                    timed: ["Skeleton", "BoneKnight", "SkeletalMage", "Lich", "LichLord", "AncientLich", "RottingCorpse", "Daemon"])),

                new RegionData("Dungeon Covetous", [0, 1], new RegionConfig(
                    common: ["Harpy", "HeadlessOne", "Gazer", "Mongbat"],
                    uncommon: ["StoneHarpy", "ElderGazer", "Lich", "Daemon"],
                    rare: ["LichLord", "Balron", "Succubus", "AncientLich"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "PoisonElemental", "IceElemental", "IceFiend"],
                    timed: ["Harpy", "StoneHarpy", "Gazer", "ElderGazer", "Lich", "LichLord", "Balron", "AncientLich"])),

                new RegionData("Dungeon Hythloth", [0, 1], new RegionConfig(
                    common: ["Daemon", "Imp", "HellHound", "FireElemental"],
                    uncommon: ["FireGargoyle", "Balron", "Succubus", "Lich"],
                    rare: ["Balron", "AncientLich", "Daemon", "LichLord"],
                    water: ["LavaSerpent"],
                    weather: ["FireElemental", "FireElemental", "IceFiend", "WhiteWyrm"],
                    timed: ["Daemon", "Imp", "HellHound", "FireGargoyle", "Balron", "Succubus", "LichLord", "AncientLich"])),

                new RegionData("Fire Dungeon", [0, 1], new RegionConfig(
                    common: ["LavaLizard", "FireElemental", "HellHound", "FireAnt"],
                    uncommon: ["FireGargoyle", "Imp", "Efreet", "LavaSerpent"],
                    rare: ["Daemon", "Balron", "LichLord", "Succubus"],
                    water: ["LavaSerpent"],
                    weather: ["FireElemental", "FireElemental", "IceFiend", "WhiteWyrm"],
                    timed: ["LavaLizard", "FireElemental", "HellHound", "Daemon", "Balron", "LichLord", "Succubus", "SkeletalKnight"])),

                new RegionData("Ice Dungeon", [0, 1], new RegionConfig(
                    common: ["PolarBear", "Walrus", "FrostSpider", "FrostOoze"],
                    uncommon: ["IceSnake", "FrostTroll", "SnowElemental", "IceElemental"],
                    rare: ["IceFiend", "WhiteWyrm", "ArcticOgreLord", "SnowElemental"],
                    water: ["SeaSerpent"],
                    weather: ["Walrus", "FrostTroll", "SnowElemental", "IceFiend"],
                    timed: ["PolarBear", "IceSnake", "FrostTroll", "SnowElemental", "IceFiend", "WhiteWyrm", "ArcticOgreLord", "FrostTroll"])),

                new RegionData("Terathan Keep", [0, 1], new RegionConfig(
                    common: ["TerathanDrone", "TerathanWarrior", "GiantSpider", "OphidianWarrior"],
                    uncommon: ["TerathanMatriarch", "OphidianWarrior", "OphidianMage", "DreadSpider"],
                    rare: ["TerathanAvenger", "OphidianArchmage", "DreadSpider", "OphidianKnight"],
                    water: ["WaterElemental"],
                    weather: ["PoisonElemental", "AcidElemental", "FrostSpider", "IceSnake"],
                    timed: ["TerathanDrone", "TerathanWarrior", "TerathanMatriarch", "TerathanAvenger", "OphidianWarrior", "OphidianArchmage", "DreadSpider", "Nightmare"])),

                // ===== ILSHENAR (Map 2) =====
                new RegionData("Lakeshire", [2], new RegionConfig(
                    common: ["Bird", "Cat", "Dog", "Rabbit"],
                    uncommon: ["TimberWolf", "Mongbat", "GiantRat", "Snake"],
                    rare: ["DireWolf", "Gazer", "EvilMage"],
                    water: ["WaterElemental"],
                    weather: ["Slime", "EarthElemental", "FrostOoze", "IceSnake"],
                    timed: ["Bird", "TimberWolf", "Mongbat", "Gazer", "DireWolf", "EvilMage", "Shade", "Spectre"])),

                new RegionData("Montor", [2], new RegionConfig(
                    common: ["Gargoyle", "StoneGargoyle", "Mongbat", "Imp"],
                    uncommon: ["FireGargoyle", "Daemon", "Gazer", "ElderGazer"],
                    rare: ["Balron", "Succubus", "LichLord", "AncientLich"],
                    water: ["WaterElemental"],
                    weather: ["FireElemental", "AirElemental", "IceFiend", "FrostSpider"],
                    timed: ["Gargoyle", "StoneGargoyle", "FireGargoyle", "Daemon", "Balron", "Succubus", "LichLord", "AncientLich"])),

                new RegionData("Reg Volom", [2], new RegionConfig(
                    common: ["LavaLizard", "FireElemental", "HellHound", "Imp"],
                    uncommon: ["FireGargoyle", "Daemon", "Efreet", "Balron"],
                    rare: ["Balron", "Phoenix", "LichLord", "AncientLich"],
                    water: ["LavaSerpent"],
                    weather: ["FireElemental", "FireElemental", "IceFiend", "WhiteWyrm"],
                    timed: ["LavaLizard", "FireElemental", "FireGargoyle", "Daemon", "Balron", "Phoenix", "LichLord", "AncientLich"])),

                new RegionData("Ankh Dungeon", [2], new RegionConfig(
                    common: ["Lich", "SkeletalKnight", "BoneMagi", "Wraith"],
                    uncommon: ["LichLord", "AncientLich", "RottingCorpse", "Spectre"],
                    rare: ["AncientLich", "ShadowWyrm", "LichLord", "Daemon"],
                    water: ["WaterElemental"],
                    weather: ["PoisonElemental", "AcidElemental", "IceFiend", "FrostOoze"],
                    timed: ["Lich", "SkeletalKnight", "BoneMagi", "LichLord", "AncientLich", "ShadowWyrm", "RottingCorpse", "Daemon"])),

                // ===== MALAS (Map 3) =====
                new RegionData("Luna", [3], new RegionConfig(
                    common: ["Cat", "Dog", "Bird", "Horse"],
                    uncommon: ["Mongbat", "Snake", "GiantRat", "Imp"],
                    rare: ["Gazer", "EvilMage", "Daemon"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "Wisp", "IceElemental", "IceFiend"],
                    timed: ["Cat", "Dog", "Mongbat", "Snake", "Gazer", "EvilMage", "Shade", "Spectre"])),

                new RegionData("Umbra", [3], new RegionConfig(
                    common: ["ShadowWisp", "Skeleton", "Zombie", "Spectre"],
                    uncommon: ["Spectre", "Shade", "Wraith", "BoneKnight"],
                    rare: ["Lich", "LichLord", "AncientLich", "RottingCorpse"],
                    water: ["WaterElemental"],
                    weather: ["PoisonElemental", "AirElemental", "IceFiend", "FrostSpider"],
                    timed: ["ShadowWisp", "Spectre", "Shade", "Lich", "LichLord", "AncientLich", "RottingCorpse", "Daemon"])),

                new RegionData("Doom Dungeon", [3], new RegionConfig(
                    common: ["BoneKnight", "SkeletalKnight", "Lich", "Daemon"],
                    uncommon: ["LichLord", "AncientLich", "Balron", "Succubus"],
                    rare: ["AncientLich", "Balron", "ShadowWyrm", "DemonKnight"],
                    water: ["BloodElemental"],
                    weather: ["PoisonElemental", "FireElemental", "IceFiend", "FrostOoze"],
                    timed: ["BoneKnight", "Lich", "LichLord", "AncientLich", "Balron", "ShadowWyrm", "DemonKnight", "Daemon"])),

                // ===== TOKUNO (Map 4) =====
                new RegionData("Zento", [4], new RegionConfig(
                    common: ["Crane", "Gaman", "DeathwatchBeetle", "Raptor"],
                    uncommon: ["Kappa", "DeathwatchBeetle", "FanDancer", "RevenantLion"],
                    rare: ["Oni", "RuneBeetle", "Yamandon", "TsukiWolf"],
                    water: ["Kappa"],
                    weather: ["AirElemental", "Eagle", "SnowElemental", "IceFiend"],
                    timed: ["Crane", "Kappa", "DeathwatchBeetle", "Oni", "RuneBeetle", "Yamandon", "TsukiWolf", "Raptor"])),

                new RegionData("Fan Dancers Dojo", [4], new RegionConfig(
                    common: ["FanDancer", "EliteNinja", "Ronin", "DeathwatchBeetle"],
                    uncommon: ["LadyOfTheSnow", "RevenantLion", "Oni", "Kappa"],
                    rare: ["Oni", "Yamandon", "Raptor", "LadyOfTheSnow"],
                    water: ["Kappa"],
                    weather: ["AirElemental", "SnowElemental", "IceElemental", "IceFiend"],
                    timed: ["FanDancer", "EliteNinja", "Ronin", "Oni", "Yamandon", "Raptor", "LadyOfTheSnow", "RevenantLion"])),

                new RegionData("Yomotsu Mines", [4], new RegionConfig(
                    common: ["YomotsuWarrior", "YomotsuPriest", "Kappa", "IronBeetle"],
                    uncommon: ["YomotsuElder", "Oni", "Yamandon", "RevenantLion"],
                    rare: ["Yamandon", "Oni", "Raptor", "YomotsuElder"],
                    water: ["Kappa"],
                    weather: ["EarthElemental", "FireElemental", "IceElemental", "IceFiend"],
                    timed: ["YomotsuWarrior", "YomotsuPriest", "YomotsuElder", "Oni", "Yamandon", "Raptor", "RevenantLion", "Kappa"])),

                // ===== TER MUR (Map 5) =====
                new RegionData("Royal City", [5], new RegionConfig(
                    common: ["Gargoyle", "StoneGargoyle", "Bird", "Cat"],
                    uncommon: ["FireGargoyle", "Mongbat", "Imp", "Gazer"],
                    rare: ["Daemon", "ElderGazer", "Balron", "Succubus"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "FireElemental", "IceElemental", "IceFiend"],
                    timed: ["Gargoyle", "StoneGargoyle", "FireGargoyle", "Daemon", "ElderGazer", "Balron", "Succubus", "Lich"])),

                new RegionData("Holy City", [5], new RegionConfig(
                    common: ["Gargoyle", "StoneGargoyle", "Mongbat", "Imp"],
                    uncommon: ["FireGargoyle", "Daemon", "Gazer", "ElderGazer"],
                    rare: ["Balron", "Succubus", "LichLord", "AncientLich"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "FireElemental", "IceElemental", "IceFiend"],
                    timed: ["Gargoyle", "StoneGargoyle", "FireGargoyle", "Daemon", "Balron", "Succubus", "LichLord", "AncientLich"])),

                new RegionData("Stygian Abyss", [5], new RegionConfig(
                    common: ["GrayGoblin", "FireAnt", "LavaLizard", "Slime"],
                    uncommon: ["FireDaemon", "SkeletalDragon", "Lich", "Golem"],
                    rare: ["StygianDrake", "StygianDrake", "Hydra", "AbysmalHorror"],
                    water: ["LavaSerpent"],
                    weather: ["FireElemental", "AcidElemental", "IceFiend", "WhiteWyrm"],
                    timed: ["GrayGoblin", "FireDaemon", "SkeletalDragon", "Lich", "StygianDrake", "StygianDrake", "Hydra", "AbysmalHorror"])),

                new RegionData("Underworld", [5], new RegionConfig(
                    common: ["ShadowWisp", "WandererOfTheVoid", "GrayGoblin", "Slime"],
                    uncommon: ["WandererOfTheVoid", "WandererOfTheVoid", "WandererOfTheVoid", "FireAnt"],
                    rare: ["VoidManifestation", "VoidManifestation", "VoidManifestation", "VoidManifestation"],
                    water: ["WaterElemental"],
                    weather: ["PoisonElemental", "AcidElemental", "IceElemental", "IceFiend"],
                    timed: ["ShadowWisp", "WandererOfTheVoid", "WandererOfTheVoid", "WandererOfTheVoid", "VoidManifestation", "VoidManifestation", "VoidManifestation", "VoidManifestation"])),

                // ===== MONDAIN'S LEGACY - Fel/Tram (0,1) =====
                new RegionData("Heartwood", [0, 1], new RegionConfig(
                    common: ["Bird", "Squirrel", "Rabbit", "SAPixie"],
                    uncommon: ["SAPixie", "MLDryad", "Satyr", "Raptor"],
                    rare: ["Changeling", "Hydra", "Raptor", "Reaper"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "PoisonElemental", "IceElemental", "IceFiend"],
                    timed: ["Bird", "SAPixie", "MLDryad", "Changeling", "Hydra", "Raptor", "Reaper", "Treefellow"])),

                new RegionData("Blighted Grove", [0, 1], new RegionConfig(
                    common: ["Bogling", "TimberWolf", "DireWolf", "Slime"],
                    uncommon: ["Changeling", "MLDryad", "Reaper", "Corpser"],
                    rare: ["Hydra", "Changeling", "Corpser", "MLDryad"],
                    water: ["SwampTentacle"],
                    weather: ["PoisonElemental", "AcidElemental", "FrostSpider", "IceSnake"],
                    timed: ["Bogling", "Changeling", "MLDryad", "Hydra", "Changeling", "Corpser", "Satyr", "FetidEssence"])),

                new RegionData("Sanctuary", [0, 1], new RegionConfig(
                    common: ["Rat", "GiantRat", "Slime", "MinotaurScout"],
                    uncommon: ["Minotaur", "MinotaurScout", "Changeling", "Ratman"],
                    rare: ["MinotaurCaptain", "TormentedMinotaur", "Doppleganger", "RatmanMage"],
                    water: ["WaterElemental"],
                    weather: ["Slime", "PoisonElemental", "FrostOoze", "IceFiend"],
                    timed: ["Rat", "Minotaur", "MinotaurCaptain", "TormentedMinotaur", "Doppleganger", "RottingCorpse", "Lich", "AncientLich"])),

                new RegionData("Prism of Light", [0, 1], new RegionConfig(
                    common: ["CrystalElemental", "Wisp", "SAPixie", "ShadowWisp"],
                    uncommon: ["CrystalLatticeSeeker", "UnfrozenMummy", "IceElemental", "IceElemental"],
                    rare: ["CrystalHydra", "MantraEffervescence", "CrystalDaemon", "EtherealWarrior"],
                    water: ["WaterElemental"],
                    weather: ["AirElemental", "EnergyVortex", "IceElemental", "IceFiend"],
                    timed: ["CrystalElemental", "CrystalLatticeSeeker", "UnfrozenMummy", "CrystalHydra", "MantraEffervescence", "CrystalDaemon", "EtherealWarrior", "SerpentineDragon"])),

                // ===== LOST LANDS - Fel/Tram (0,1) =====
                new RegionData("Papua", [0, 1], new RegionConfig(
                    common: ["Bird", "Gorilla", "Snake", "Bird"],
                    uncommon: ["Lizardman", "Alligator", "GiantSerpent", "Mongbat"],
                    rare: ["TerathanWarrior", "Raptor", "OphidianWarrior"],
                    water: ["SwampTentacle"],
                    weather: ["Slime", "AcidElemental", "FrostOoze", "IceSnake"],
                    timed: ["Bird", "Lizardman", "Alligator", "TerathanWarrior", "Raptor", "OphidianWarrior", "Shade", "Zombie"])),

                new RegionData("Delucia", [0, 1], new RegionConfig(
                    common: ["Cow", "Sheep", "Bird", "Dog"],
                    uncommon: ["Orc", "Ettin", "Mongbat", "GiantSpider"],
                    rare: ["OrcCaptain", "Titan", "Cyclops", "OgreLord"],
                    water: ["WaterElemental"],
                    weather: ["Slime", "EarthElemental", "FrostOoze", "IceSnake"],
                    timed: ["Cow", "Orc", "Ettin", "OrcCaptain", "Titan", "Cyclops", "OgreLord", "Lich"]))
            ];
        }

        #endregion

        #region Empty Box Spawns & Default Settings

        private static void GenerateEmptyBoxSpawns(string filePath)
        {
            using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
            
            writer.Write(BOX_SPAWN_VERSION);
            writer.Write(APP_VERSION);
            writer.Write(0); // mapCount = 0 (no boxes)
            
            Logger.Info("Generated empty box spawns file");
        }

        private static void GenerateDefaultSettings(string filePath)
        {
            using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
            
            writer.Write(SETTINGS_VERSION);
            writer.Write(APP_VERSION);
            
            // Basic spawn limits (defaults)
            writer.Write(200);   // MaxMobs
            writer.Write(5);     // MinRange
            writer.Write(10);    // MaxRange
            writer.Write(3);     // MaxCrowd
            
            // Spawn chances (doubles)
            writer.Write(0.25);  // WaterChance
            writer.Write(0.15);  // WeatherChance
            writer.Write(0.20);  // TimedChance
            writer.Write(0.60);  // CommonChance
            writer.Write(0.30);  // UncommonChance
            writer.Write(0.10);  // RareChance
            
            // Feature flags
            writer.Write(true);  // IsScaleSpawn
            writer.Write(false); // EnableRiftSpawn
            writer.Write(false); // EnableDebugSpawn
            
            Logger.Info("Generated default settings file");
        }

        #endregion

        #region Helper Methods

        private static void WriteStringList(BinaryWriter writer, List<string> list)
        {
            writer.Write(list.Count);
            foreach (var item in list)
            {
                writer.Write(item ?? string.Empty);
            }
        }

        #endregion

        #region Config Classes

        private record TileConfig(
            string Name,
            List<string> Common,
            List<string> Uncommon,
            List<string> Rare,
            List<string> Water,
            List<string> Weather,
            List<string> Timed,
            WeatherTypes WeatherTrigger = WeatherTypes.Rain,
            TimeNames TimedTrigger = TimeNames.Late_at_Night)
        {
            public TileConfig(string name, string[] common, string[] uncommon, string[] rare, 
                              string[] water, string[] weather, string[] timed,
                              WeatherTypes weatherTrigger = WeatherTypes.Rain,
                              TimeNames timedTrigger = TimeNames.Late_at_Night)
                : this(name, common.ToList(), uncommon.ToList(), rare.ToList(), water.ToList(), weather.ToList(), timed.ToList(), weatherTrigger, timedTrigger) { }
        }

        private record RegionConfig(
            List<string> Common,
            List<string> Uncommon,
            List<string> Rare,
            List<string> Water,
            List<string> Weather,
            List<string> Timed,
            WeatherTypes WeatherTrigger = WeatherTypes.Rain,
            TimeNames TimedTrigger = TimeNames.Late_at_Night)
        {
            public RegionConfig(string[] common, string[] uncommon, string[] rare,
                               string[] water, string[] weather, string[] timed,
                               WeatherTypes weatherTrigger = WeatherTypes.Rain,
                               TimeNames timedTrigger = TimeNames.Late_at_Night)
                : this(common.ToList(), uncommon.ToList(), rare.ToList(), water.ToList(), weather.ToList(), timed.ToList(), weatherTrigger, timedTrigger) { }
        }

        private record RegionData(string Name, int[] MapIds, RegionConfig Config);

        #endregion
    }
}
