namespace UORespawnApp
{
    internal static class WorldSpawnUtility
    {
        private static readonly string WorldSpawnFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_WorldSpawn.csv");

        private static readonly string StaticSpawnFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_StaticSpawn.csv");

        internal static readonly string StaticFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "UOR_StaticList.txt");

        internal static readonly string BestiaryFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "UOR_SpawnerList.txt");
        
        internal static readonly string CustomBestiaryFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_SpawnerList_Custom.txt");

        internal static readonly string CustomStaticFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_StaticList_Custom.txt");

        internal static List<WorldEntity> WorldSpawnList { get; private set; } = [];

        internal static void AddWorldEntity(WorldEntity entity)
        {
            if (WorldSpawnList.Find(we => we.MapHandle == entity.MapHandle) == null)
            {
                WorldSpawnList.Add(entity);
            }
        }

        internal static List<StaticEntity> StaticSpawnList { get; private set; } = [];

        internal static void AddStaticEntity(StaticEntity entity)
        {
            if (StaticSpawnList.Find(se => se.Name == entity.Name) == null)
            {
                StaticSpawnList.Add(entity);
            }
        }

        internal static List<string>? SpawnList { get; private set; }

        internal static async Task LoadSpawnList()
        {
            SpawnList ??= [];
            
            if (SpawnList.Count > 0)
            {
                return; // Already loaded
            }

            try
            {
                // Priority 1: Try to load from Custom file (user edits)
                if (File.Exists(CustomBestiaryFile))
                {
                    var lines = await File.ReadAllLinesAsync(CustomBestiaryFile);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            SpawnList.Add(line.Trim());
                        }
                    }
                    SpawnList.Sort();
                    Console.WriteLine($"Loaded {SpawnList.Count} creatures from custom bestiary");
                    return;
                }

                // Priority 2: Try to load from Resources/Raw folder
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("UOR_SpawnerList.txt");
                    using var reader = new StreamReader(stream);
                    
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            SpawnList.Add(line.Trim());
                        }
                    }
                    SpawnList.Sort();
                    Console.WriteLine($"Loaded {SpawnList.Count} creatures from bestiary file");
                }
            }
            catch
            {
                // Priority 3: Fallback to embedded bestiary list
                Console.WriteLine("Loading embedded bestiary list...");
                LoadEmbeddedBestiary();
            }
        }

        internal static async Task SaveCustomBestiary()
        {
            try
            {
                if (SpawnList == null || SpawnList.Count == 0)
                {
                    return;
                }

                // Ensure Data directory exists
                var dataDir = Path.GetDirectoryName(CustomBestiaryFile);
                if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                // Sort before saving
                SpawnList.Sort();

                // Save to custom file
                await File.WriteAllLinesAsync(CustomBestiaryFile, SpawnList);
                Console.WriteLine($"Saved {SpawnList.Count} creatures to custom bestiary");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving custom bestiary: {ex.Message}");
            }
        }

        internal static async Task<bool> ResetToDefaultBestiary()
        {
            try
            {
                // Delete custom file if it exists
                if (File.Exists(CustomBestiaryFile))
                {
                    File.Delete(CustomBestiaryFile);
                    Console.WriteLine("Deleted custom bestiary file");
                }

                // Clear and reload
                SpawnList = null;
                await LoadSpawnList();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting bestiary: {ex.Message}");
                return false;
            }
        }

        internal static void AddCreatureToBestiary(string creatureName)
        {
            SpawnList ??= [];

            var trimmedName = creatureName.Trim();

            if (!string.IsNullOrWhiteSpace(trimmedName) && !SpawnList.Contains(trimmedName))
            {
                SpawnList.Add(trimmedName);
                SpawnList.Sort();
            }
        }

        internal static void RemoveCreatureFromBestiary(string creatureName)
        {
            SpawnList?.Remove(creatureName);
        }

        private static void LoadEmbeddedBestiary()
        {
            SpawnList =
            [
                "ZombieSkeleton", "AbominableSnowman", "GarishGingerman", "HeadlessElf", "JackInTheBox",
                "JackThePumpkinKing", "NightmareFairy", "RabidReindeer", "StockingSerpent", "TwistedHolidayTree",
                "VelaTheSorceress", "WarehouseSuperintendent", "ShadowguardPirate", "EnsorcelledArmor", "LadyMinax",
                "GaryTheDungeonMaster", "GameMaster", "SlimTheFence", "EnsorcledWisp", "CorruptedWisp",
                "BexilPunchingBag", "CursedSoul", "DeadlyImp", "DiseasedCat", "FierceDragon",
                "InjuredWolf", "YoungNinja", "YoungRonin", "Henchman", "MilitiaFighter",
                "ConfettiEffectNPC", "EffectNPC", "ElectricEffectNPC", "ExplosionEffectNPC", "FireEffectNPC",
                "GlowEffectNPC", "MagicEffectNPC", "MistEffectNPC", "PoisonEffectNPC", "SmokeEffectNPC",
                "WaveEffectNPC", "WindEffectNPC", "AmbushNPC", "TownNPC", "WorldNPC",
                "ProgressiveMob", "PetParrot", "BaseTalismanSummon", "TyballsShadow", "Harrower",
                "HarrowerTentacles", "ServantOfSemidar", "Silvani", "PumpkinHead", "Abscess",
                "Drelgor", "Flurry", "GrimmochDrummel", "LysanderGathenwale", "Mistral",
                "MorgBergen", "NightTerror", "ShadowKnight", "TavaraSewel", "Tempest",
                "Thrasher", "AbysmalHorror", "AbyssalAbomination", "AcidElemental", "AcidSlug",
                "AgapiteElemental", "AirElemental", "Alligator", "AncientLich", "AncientWyrm",
                "AntLion", "ArcaneDaemon", "ArchDaemon", "ArcticOgreLord", "BakeKitsune",
                "Balron", "BattleChickenLizard", "Betrayer", "Bird", "TropicalBird",
                "BlackBear", "BlackSolenInfiltratorQueen", "BlackSolenInfiltratorWarrior", "BlackSolenQueen", "BlackSolenWarrior",
                "BlackSolenWorker", "BloodElemental", "BloodFox", "BloodWorm", "Boar",
                "Bogle", "Bogling", "BogThing", "BoneDemon", "BoneKnight",
                "BoneMagi", "Brigand", "BronzeElemental", "BrownBear", "BulbousPutrification",
                "Bull", "BullFrog", "Cat", "Centaur", "Changeling",
                "ChaosDaemon", "ChaosDragoon", "ChaosDragoonElite", "Chicken", "ChickenLizard",
                "ClanCA", "ClanCT", "ClanRC", "ClanRibbonPlagueRat", "ClanRS",
                "ClanSH", "ClanSS", "ClanSSW", "ClockworkScorpion", "ColdDrake",
                "CopperElemental", "CoralSnake", "CorporealBrume", "Corpser", "CorrosiveSlime",
                "CorruptedSoul", "Cougar", "Cow", "Crane", "CrimsonDrake",
                "CrystalDaemon", "CrystalElemental", "CrystalHydra", "CrystalLatticeSeeker", "CrystalVortex",
                "Cursed", "Cyclops", "Daemon", "DarkGuardian", "DarknightCreeper",
                "DarkWisp", "DeathwatchBeetle", "DeathwatchBeetleHatchling", "DeepSeaSerpent", "DemonKnight",
                "Devourer", "DireWolf", "Dog", "Dolphin", "Doppleganger",
                "Dragon", "DragonsFlameMage", "DragonWolf", "Drake", "DreadSpider",
                "DreamWraith", "DullCopperElemental", "Eagle", "EarthElemental", "EffetePutridGargoyle",
                "EffeteUndeadGargoyle", "Efreet", "ElderGazer", "ElfBrigand", "EliteNinja",
                "EnragedColossus", "EnragedEarthElemental", "EnslavedGargoyle", "EnslavedGoblinKeeper", "EnslavedGoblinMage",
                "EnslavedGoblinScout", "EnslavedGrayGoblin", "EnslavedGreenGoblin", "EnslavedGreenGoblinAlchemist", "EtherealWarrior",
                "Ettin", "EvilMage", "EvilMageLord", "Executioner", "ExodusMinion",
                "ExodusOverseer", "FairyDragon", "FanDancer", "FeralTreefellow", "Ferret",
                "FetidEssence", "FireAnt", "FireDaemon", "FireElemental", "FireGargoyle",
                "FleshGolem", "FleshRenderer", "ForgottenServant", "FrostDragon", "FrostMite",
                "FrostOoze", "FrostSpider", "FrostTroll", "Gaman", "GargishOutcast",
                "GargishRouser", "Gargoyle", "GargoyleDestroyer", "GargoyleEnforcer", "GargoyleGuardian",
                "GargoyleShade", "Gazer", "GazerLarva", "Ghoul", "GiantBlackWidow",
                "GiantIceWorm", "GiantRat", "GiantSerpent", "GiantSpider", "GiantToad",
                "GiantTurkey", "Gibberling", "Goat", "GoldenElemental", "Golem",
                "GolemController", "GoreFiend", "Gorilla", "GrayGoblin", "GrayGoblinKeeper",
                "GrayGoblinMage", "GreaterDragon", "GreaterMongbat", "GreaterPoisonElemental", "GreatHart",
                "GreenGoblin", "GreenGoblinAlchemist", "GreenGoblinScout", "Gregorio", "Gremlin",
                "GreyWolf", "GrizzlyBear", "Grubber", "Guardian", "Harpy",
                "HeadlessOne", "HellCat", "HellHound", "HighPlainsBoura", "Hind",
                "HordeMinion", "HungryCoconutCrab", "Hydra", "IceElemental", "IceFiend",
                "IceHound", "IceSerpent", "IceSnake", "Imp", "Impaler",
                "MLDryad", "InterredGrizzle", "IronBeetle", "JackRabbit", "Juggernaut",
                "JukaLord", "JukaMage", "JukaWarrior", "Jwilson", "Kappa",
                "KazeKemono", "Kepetch", "KepetchAmbusher", "KhaldunSummoner", "KhaldunZealot",
                "Kraken", "LadyOfTheSnow", "LavaElemental", "LavaLizard", "LavaSerpent",
                "LavaSnake", "LeatherWolf", "LeatherWolfFellow", "Leviathan", "Lich",
                "LichLord", "Lifestealer", "Lion", "Lizardman", "Llama",
                "LowlandBoura", "MaddeningHorror", "MantraEffervescence", "MeerCaptain", "MeerEternal",
                "MeerMage", "MeerWarrior", "Mimic", "MinionOfScelestus", "Minotaur",
                "MinotaurCaptain", "MinotaurScout", "Moloch", "Mongbat", "MoundOfMaggots",
                "MountainGoat", "Mummy", "Ogre", "OgreLord", "Oni",
                "OphidianArchmage", "OphidianKnight", "OphidianMage", "OphidianMatriarch", "OphidianWarrior",
                "Orc", "OrcBomber", "OrcBrute", "OrcCaptain", "OrcChopper",
                "OrcishLord", "OrcishMage", "OrcScout", "Ortanord", "OsseinRam",
                "PackHorse", "PackLlama", "Panther", "PatchworkSkeleton", "PestilentBandage",
                "Phoenix", "Pig", "PitFiend", "Pixie", "PlagueBeast",
                "PlagueBeastLord", "PlagueRat", "PlagueSpawn", "PlatinumDrake", "PoisonElemental",
                "PolarBear", "PredatorHellCat", "Protector", "PutridUndeadGargoyle", "PutridUndeadGuardian",
                "Quagmire", "Rabbit", "RagingGrizzlyBear", "RaiJu", "Raptor",
                "Rat", "Ratman", "RatmanArcher", "RatmanMage", "Ravager",
                "Reaper", "RedSolenInfiltratorQueen", "RedSolenInfiltratorWarrior", "RedSolenQueen", "RedSolenWarrior",
                "RedSolenWorker", "RestlessSoul", "RevenantLion", "Ronin", "RottingCorpse",
                "Rotworm", "RuddyBoura", "RuneBeetle", "SabertoothedTiger", "SandVortex",
                "SAPixie", "Satyr", "Savage", "SavageRider", "SavageShaman",
                "Scorpion", "SeaSerpent", "SentinelSpider", "SerpentineDragon", "SerpentsFangAssassin",
                "Sewerrat", "Shade", "ShadowDweller", "ShadowIronElemental", "ShadowWisp",
                "ShadowWyrm", "Sheep", "SilverSerpent", "SkeletalDragon", "SkeletalDrake",
                "SkeletalKnight", "SkeletalLich", "SkeletalMage", "Skeleton", "SkitteringHopper",
                "Skree", "Slime", "Slith", "Snake", "SnowElemental",
                "SnowLeopard", "SpectralArmour", "Spectre", "Spellbinder", "Squirrel",
                "StoneGargoyle", "StoneHarpy", "StoneMonster", "StoneSlith", "StrongMongbat",
                "StygianDrake", "Succubus", "SwampTentacle", "TanglingRoots", "TerathanAvenger",
                "TerathanDrone", "TerathanMatriarch", "TerathanWarrior", "TheButcher", "Parrot",
                "TigersClawThief", "TimberWolf", "Titan", "TormentedMinotaur", "ToxicElemental",
                "ToxicSlith", "TrapdoorSpider", "Treefellow", "TreefellowGuardian", "Triceratops",
                "Triton", "Troglodyte", "Troll", "TsukiWolf", "Turkey",
                "UndeadGargoyle", "UndeadGuardian", "UnfrozenMummy", "ValoriteElemental", "VampireBat",
                "VeriteElemental", "Virulent", "Viscera", "VoidManifestation", "VorpalBunny",
                "WailingBanshee", "Walrus", "WandererOfTheVoid", "WaterElemental", "WhippingVine",
                "WhiteWolf", "WhiteWyrm", "Wight", "Wisp", "WolfSpider",
                "Wraith", "Wyvern", "Yamandon", "YomotsuElder", "YomotsuPriest",
                "YomotsuWarrior", "Zombie", "Actor", "Aminia", "Artist",
                "BaseEscortable", "BaseHire", "GargishRefugee", "GargishWarrior", "Gypsy",
                "HarborMaster", "MysteriousWisp", "Ninja", "Paladin", "Samurai",
                "Sculptor", "Vollem", "BladeSpirits", "EnergyVortex", "GargoylePet",
                "SummonedAirElemental", "SummonedDaemon", "SummonedEarthElemental", "SummonedFireElemental", "SummonedWaterElemental",
                "MyrmidexQueen", "Zipactriotl", "IgnisFatalis", "TRex", "TigerCub",
                "VolcanoElemental", "Raider", "UnholyFamiliar", "HolyFamiliar", "BoundSoul",
                "SoulboundPirateCaptain", "SoulboundPirateRaider", "SoulboundSwashbuckler", "BaseShipCaptain", "MerchantCrew",
                "PirateCrew", "Allosaurus", "Anchisaur", "Archaeosaurus", "BritannianInfantry",
                "DesertScorpion", "Dimetrosaur", "DragonTurtleHatchling", "Gallusaurus", "GreaterPhoenix",
                "Infernus", "MyrmidexDrone", "MyrmidexLarvae", "MyrmidexWarrior", "Najasaurus",
                "Saurosaurus", "SilverbackGorilla", "DiabolicalSeaweed", "Djinn", "MercutioTheUnsavory",
                "ObsidianWyvern", "Paralithode", "RockMite", "SeaSnake", "Shadowlord",
                "ShipRat", "Sphynx", "CoraTheSorceress", "MudPie", "LesserFlameElemental",
                "LesserWindElemental", "BurningMage", "CrazedMage", "ChaosVortex", "UnboundEnergyVortex",
                "ClockworkExodus", "DupresChampion", "DupresKnight", "DupresSquire", "ExodusDrone",
                "ExodusJuggernaut", "ExodusMinionLord", "ExodusSentinel", "ExodusZealot", "Archmage",
                "CaveTrollWrong", "DemonicJailor", "Fezzik", "GooeyMaggots", "HungryOgre",
                "LizardmanDefender", "LizardmanSquatter", "PrisonRat", "SavagePackWolf", "Krampus",
                "KrampusMinion", "CultistAmbusher", "KhalAnkurWarriors", "KhaldunBlood", "ShadowFiend",
                "SkelementalKnight", "SkelementalMage", "DescicatedMyrmidexLarvae", "ArcaneFey", "ArcaneFiend",
                "NatureFury", "KotlAutomaton", "Macaw"
            ];
            
            SpawnList.Sort();
            Console.WriteLine($"? Loaded {SpawnList.Count} creatures from embedded bestiary");
        }

        internal static void SetSpawnList(List<string> list)
        {
            SpawnList = list;
        }

        internal static void SetStaticList(List<string> list)
        {
            StaticList = list;
        }

        internal static bool ValidSpawn(string? name)
        {
            ValidateList();

            if (!string.IsNullOrEmpty(name) && SpawnList != null)
            {
                if (SpawnList.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateList()
        {
            SpawnList ??= [];

            if (SpawnList.Count == 0)
            {
                if (File.Exists(BestiaryFile))
                {
                    var lines = File.ReadAllLines(BestiaryFile);

                    if (lines.Length > 0)
                    {
                        foreach (var mob in lines)
                        {
                            if (!string.IsNullOrEmpty(mob))
                            {
                                SpawnList.Add(mob);
                            }
                        }
                    }
                }
                else
                {
                    Settings.Bestiary ??= [];

                    if (Settings.Bestiary.Count > 0)
                    {
                        foreach (var mob in Settings.Bestiary)
                        {
                            if (mob != null)
                            {
                                SpawnList.Add(mob);
                            }
                        }
                    }
                }
            }
        }

        internal static List<string>? StaticList { get; private set; }

        internal static async Task LoadStaticList()
        {
            StaticList ??= [];
            
            if (StaticList.Count > 0)
            {
                return; // Already loaded
            }

            try
            {
                // Priority 1: Try to load from Custom file (user edits)
                if (File.Exists(CustomStaticFile))
                {
                    var lines = await File.ReadAllLinesAsync(CustomStaticFile);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            StaticList.Add(line.Trim());
                        }
                    }
                    StaticList.Sort();
                    Console.WriteLine($"Loaded {StaticList.Count} statics from custom list");
                    return;
                }

                // Priority 2: Try to load from Resources/Raw folder (MAUI package)
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("UOR_StaticList.txt");
                    using var reader = new StreamReader(stream);
                    
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            StaticList.Add(line.Trim());
                        }
                    }
                    
                    if (StaticList.Count > 0)
                    {
                        StaticList.Sort();
                        Console.WriteLine($"Loaded {StaticList.Count} statics from package file");
                        return;
                    }
                }

                // Priority 3: Try direct file path (development environment)
                if (File.Exists(StaticFile))
                {
                    var lines = await File.ReadAllLinesAsync(StaticFile);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            StaticList.Add(line.Trim());
                        }
                    }
                    StaticList.Sort();
                    Console.WriteLine($"Loaded {StaticList.Count} statics from direct file path");
                    return;
                }

                // Priority 4: Fallback to embedded static list (minimal safety net)
                Console.WriteLine("Loading minimal embedded static list as fallback...");
                LoadEmbeddedStaticList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading static list: {ex.Message}");
                LoadEmbeddedStaticList();
            }
        }

        internal static async Task SaveCustomStaticList()
        {
            try
            {
                if (StaticList == null || StaticList.Count == 0)
                {
                    return;
                }

                // Ensure Data directory exists
                var dataDir = Path.GetDirectoryName(CustomStaticFile);
                if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                // Sort before saving
                StaticList.Sort();

                // Save to custom file
                await File.WriteAllLinesAsync(CustomStaticFile, StaticList);
                Console.WriteLine($"Saved {StaticList.Count} statics to custom list");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving custom static list: {ex.Message}");
            }
        }

        internal static async Task<bool> ResetToDefaultStaticList()
        {
            try
            {
                // Delete custom file if it exists
                if (File.Exists(CustomStaticFile))
                {
                    File.Delete(CustomStaticFile);
                    Console.WriteLine("Deleted custom static list file");
                }

                // Clear and reload
                StaticList = null;
                await LoadStaticList();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting static list: {ex.Message}");
                return false;
            }
        }

        internal static void AddStaticToList(string staticName)
        {
            StaticList ??= [];

            var trimmedName = staticName.Trim();

            if (!string.IsNullOrWhiteSpace(trimmedName) && !StaticList.Contains(trimmedName))
            {
                StaticList.Add(trimmedName);
                StaticList.Sort();
            }
        }

        internal static void RemoveStaticFromList(string staticName)
        {
            StaticList?.Remove(staticName);
        }

        private static void LoadEmbeddedStaticList()
        {
            StaticList =
            [
                "tree", "rock", "mountain", "cave_entrance", "building", "sign", "stone_wall",
                "wooden_fence", "iron_gate", "fountain", "statue", "altar", "anvil", "forge",
                "loom", "spinning_wheel", "oven", "mill", "well", "bridge", "tower",
                "castle_wall", "dungeon_entrance", "shrine", "moongate", "obelisk"
            ];
            
            StaticList.Sort();
            Console.WriteLine($"?? Loaded {StaticList.Count} statics from embedded list");
        }

        internal static bool ValidStatic(string? name)
        {
            ValidateStaticList();

            if (!string.IsNullOrEmpty(name) && StaticList != null)
            {
                if (StaticList.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateStaticList()
        {
            if (StaticList == null || StaticList.Count == 0)
            {
                // Force synchronous load if not already loaded
                StaticList = [];
                
                if (File.Exists(CustomStaticFile))
                {
                    var lines = File.ReadAllLines(CustomStaticFile);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            StaticList.Add(line.Trim());
                        }
                    }
                }
                else if (File.Exists(StaticFile))
                {
                    var lines = File.ReadAllLines(StaticFile);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            StaticList.Add(line.Trim());
                        }
                    }
                }
                else
                {
                    LoadEmbeddedStaticList();
                }
            }
        }

        internal static async Task SaveWorldSpawnList()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
                }

                if (string.IsNullOrEmpty(Settings.ServUODataFolder) || !Directory.Exists(Settings.ServUODataFolder))
                {
                    await WorldSave(WorldSpawnFile);
                }
                else
                {
                    await WorldSave(WorldSpawnFile);

                    await WorldSave(Path.Combine(Settings.ServUODataFolder, "UOR_WorldSpawn.csv"));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving WorldSpawnList: {ex.Message}");
            }
        }

        private static async Task WorldSave(string filePath)
        {
            await using var streamWriter = new StreamWriter(filePath);

            foreach (var worldEntity in WorldSpawnList)
            {
                // Only save tiles that have actual spawns
                var tilesWithSpawns = worldEntity.WorldSpawn
                    .Where(kvp => kvp.Value.Any(t => !string.IsNullOrWhiteSpace(t.Name)))
                    .ToList();
                
                if (tilesWithSpawns.Count > 0)
                {
                    // Write map header with count of tiles that have spawns
                    await streamWriter.WriteLineAsync($"{(int)worldEntity.MapHandle},{tilesWithSpawns.Count}");

                    foreach (var kvp in tilesWithSpawns)
                    {
                        var tile = kvp.Key;

                        // Filter out empty or whitespace names before saving
                        var validSpawns = kvp.Value
                            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                            .Select(t => $"{t.Name}:{t.Freq}:{t.IsMob}");

                        var spawns = string.Join("*", validSpawns);
                        
                        // Only write the line if there are valid spawns
                        if (!string.IsNullOrEmpty(spawns))
                        {
                            await streamWriter.WriteLineAsync($"{tile}|{spawns}");
                        }
                    }
                }
            }
        }

        internal static async Task LoadWorldSpawnList()
        {
            try
            {
                Console.WriteLine($"=== LoadWorldSpawnList START ===");
                Console.WriteLine($"WorldSpawnFile path: {WorldSpawnFile}");
                Console.WriteLine($"File.Exists: {File.Exists(WorldSpawnFile)}");
                Console.WriteLine($"WorldSpawnList.Count before: {WorldSpawnList.Count}");
                
                if (File.Exists(WorldSpawnFile))
                {
                    Console.WriteLine($"File found, clearing WorldSpawnList...");
                    // Clear existing data to prevent duplicates
                    WorldSpawnList.Clear();
                    
                    // Initialize fresh world entities
                    InitiateWorldTileSpawn();
                    Console.WriteLine($"Initialized {WorldSpawnList.Count} world entities");

                    var lines = await File.ReadAllLinesAsync(WorldSpawnFile);
                    Console.WriteLine($"Read {lines.Length} lines from file");

                    WorldEntity? currentEntity = null;
                    int lineNumber = 0;
                    int spawnsAdded = 0;

                    foreach (var line in lines)
                    {
                        lineNumber++;
                        
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            Console.WriteLine($"Line {lineNumber}: Skipping empty line");
                            continue;
                        }
                        
                        var parts = line.Split(',');

                        if (parts.Length == 2)
                        {
                            var mapHandle = (GameMap)Utility.IsValidMapID(parts[0]);

                            currentEntity = WorldSpawnList.Find(e => e.MapHandle == mapHandle);
                            Console.WriteLine($"Line {lineNumber}: Switching to map {mapHandle}");
                        }
                        else
                        {
                            parts = line.Split('|');

                            // Use TryParse to handle malformed CSV data gracefully
                            if (parts.Length < 2)
                            {
                                Console.WriteLine($"WARNING: Invalid world spawn line format (expected |), skipping: {line}");
                                continue;
                            }

                            if (!Enum.TryParse<WorldTile>(parts[0], out var tile))
                            {
                                Console.WriteLine($"WARNING: Invalid WorldTile value '{parts[0]}' in world spawn data, skipping entry");
                                continue;
                            }

                            var spawnDetails = parts[1].Split('*');

                            foreach (var spawnDetail in spawnDetails)
                            {
                                var spawnParts = spawnDetail.Split(':');

                                if (spawnParts.Length >= 3)
                                {
                                    var name = spawnParts[0];
                                    
                                    // Skip empty or whitespace names to prevent ghost spawns
                                    if (string.IsNullOrWhiteSpace(name))
                                    {
                                        Console.WriteLine($"WARNING: Skipped empty spawn name in tile {tile}");
                                        continue;
                                    }
                                    
                                    if (!Enum.TryParse<Frequency>(spawnParts[1], out var freq))
                                    {
                                        Console.WriteLine($"WARNING: Invalid Frequency value '{spawnParts[1]}' for {name}, skipping entry");
                                        continue;
                                    }
                                    
                                    if (!bool.TryParse(spawnParts[2], out var isMob))
                                    {
                                        Console.WriteLine($"WARNING: Invalid boolean value '{spawnParts[2]}' for {name}, skipping entry");
                                        continue;
                                    }
                                    
                                    var tileEntity = new TileEntity(freq, name, isMob);

                                    currentEntity?.AddSpawn(tile, tileEntity);
                                    spawnsAdded++;
                                }
                            }
                        }
                    }
                    
                    Console.WriteLine($"=== LoadWorldSpawnList COMPLETE ===");
                    Console.WriteLine($"Total spawns added: {spawnsAdded}");
                    Console.WriteLine($"WorldSpawnList has {WorldSpawnList.Count} world entities");
                    Console.WriteLine($"Loaded world spawn data from {WorldSpawnFile}");
                }
                else
                {
                    Console.WriteLine($"=== LoadWorldSpawnList: FILE NOT FOUND ===");
                    Console.WriteLine($"Expected path: {WorldSpawnFile}");
                    // No file exists, initialize empty world entities
                    if (WorldSpawnList.Count < 6)
                    {
                        WorldSpawnList.Clear();
                        InitiateWorldTileSpawn();
                        Console.WriteLine("No world spawn file found, initialized empty data");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading WorldSpawnList: {ex.Message}");
            }
        }

        // SYNCHRONOUS version for startup - avoids deadlock with GetAwaiter().GetResult()
        internal static void LoadWorldSpawnListSync()
        {
            try
            {
                if (File.Exists(WorldSpawnFile))
                {
                    WorldSpawnList.Clear();
                    InitiateWorldTileSpawn();

                    var lines = File.ReadAllLines(WorldSpawnFile);  // SYNCHRONOUS

                    WorldEntity? currentEntity = null;
                    int spawnsAdded = 0;

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }
                        
                        var parts = line.Split(',');

                        if (parts.Length == 2)
                        {
                            var mapHandle = (GameMap)Utility.IsValidMapID(parts[0]);
                            currentEntity = WorldSpawnList.Find(e => e.MapHandle == mapHandle);
                        }
                        else
                        {
                            parts = line.Split('|');

                            if (parts.Length < 2) continue;

                            if (!Enum.TryParse<WorldTile>(parts[0], out var tile)) continue;

                            var spawnDetails = parts[1].Split('*');

                            foreach (var spawnDetail in spawnDetails)
                            {
                                var spawnParts = spawnDetail.Split(':');

                                if (spawnParts.Length >= 3)
                                {
                                    var name = spawnParts[0];
                                    
                                    if (string.IsNullOrWhiteSpace(name)) continue;
                                    
                                    if (!Enum.TryParse<Frequency>(spawnParts[1], out var freq)) continue;
                                    
                                    if (!bool.TryParse(spawnParts[2], out var isMob)) continue;
                                    
                                    var tileEntity = new TileEntity(freq, name, isMob);

                                    currentEntity?.AddSpawn(tile, tileEntity);
                                    spawnsAdded++;
                                }
                            }
                        }
                    }
                    
                    if (spawnsAdded > 0)
                    {
                        Console.WriteLine($"Loaded world spawn data: {WorldSpawnList.Count} maps with {spawnsAdded} spawns");
                    }
                }
                else
                {
                    if (WorldSpawnList.Count < 6)
                    {
                        WorldSpawnList.Clear();
                        InitiateWorldTileSpawn();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading WorldSpawnList: {ex.Message}");
            }
        }

        internal static async Task SaveStaticSpawnList()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
                }

                if (string.IsNullOrEmpty(Settings.ServUODataFolder) || !Directory.Exists(Settings.ServUODataFolder))
                {
                    await StaticSave(StaticSpawnFile);
                }
                else
                {
                    await StaticSave(StaticSpawnFile);

                    await StaticSave(Path.Combine(Settings.ServUODataFolder, "UOR_StaticSpawn.csv"));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving WorldSpawnList: {ex.Message}");
            }
        }

        private static async Task StaticSave(string filePath)
        {
            await using var streamWriter = new StreamWriter(filePath);

            foreach (var staticEntity in StaticSpawnList)
            {
                // Filter out empty or whitespace names before counting
                var validSpawns = staticEntity.Spawn
                    .Where(s => !string.IsNullOrWhiteSpace(s.name))
                    .ToList();
                
                if (validSpawns.Count > 0)
                {
                    await streamWriter.WriteLineAsync($"{staticEntity.Name},{validSpawns.Count}");

                    foreach (var (freq, name) in validSpawns)
                    {
                        await streamWriter.WriteLineAsync($"{freq},{name}");
                    }
                }
            }
        }

        internal static async Task LoadStaticSpawnList()
        {
            try
            {
                if (File.Exists(StaticSpawnFile))
                {
                    StaticSpawnList ??= [];

                    StaticSpawnList.Clear();

                    var lines = await File.ReadAllLinesAsync(StaticSpawnFile);

                    for (int index = 0; index < lines.Length;)
                    {
                        var parts = lines[index].Split(',');

                        if (parts.Length >= 2)
                        {
                            var staticName = parts[0];

                            var spawnCount = int.Parse(parts[1]);

                            List<(Frequency freq, string name)> spawn = [];

                            for (int i = 0; i < spawnCount; i++)
                            {
                                index++;

                                // Bounds check to prevent crash on malformed CSV
                                if (index >= lines.Length)
                                {
                                    Console.WriteLine($"ERROR: Static spawn file truncated for '{staticName}' - expected {spawnCount} spawns but file ended early");
                                    break;
                                }

                                var lineParts = lines[index].Split(',');

                                if (lineParts.Length == 2)
                                {
                                    var spawnName = lineParts[1];
                                    
                                    // Skip empty or whitespace names to prevent ghost spawns
                                    if (string.IsNullOrWhiteSpace(spawnName))
                                    {
                                        Console.WriteLine($"WARNING: Skipped empty spawn name for static {staticName}");
                                        continue;
                                    }
                                    
                                    if (Enum.TryParse(lineParts[0], out Frequency freq))
                                    {
                                        spawn.Add((freq, spawnName));
                                    }
                                }
                            }

                            StaticSpawnList.Add(new StaticEntity(staticName, spawn));
                        }

                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading WorldSpawnList: {ex.Message}");
            }
            finally
            {
                XMLSpawnUtility.LoadSpawnerList();
            }
        }

        // SYNCHRONOUS version for startup - avoids deadlock with GetAwaiter().GetResult()
        internal static void LoadStaticSpawnListSync()
        {
            try
            {
                if (File.Exists(StaticSpawnFile))
                {
                    StaticSpawnList ??= [];
                    StaticSpawnList.Clear();

                    var lines = File.ReadAllLines(StaticSpawnFile);  // SYNCHRONOUS

                    for (int index = 0; index < lines.Length;)
                    {
                        var parts = lines[index].Split(',');

                        if (parts.Length >= 2)
                        {
                            var staticName = parts[0];

                            if (!int.TryParse(parts[1], out var spawnCount))
                            {
                                index++;
                                continue;
                            }

                            List<(Frequency freq, string name)> spawn = [];

                            for (int i = 0; i < spawnCount; i++)
                            {
                                index++;

                                if (index >= lines.Length) break;

                                var lineParts = lines[index].Split(',');

                                if (lineParts.Length == 2)
                                {
                                    var spawnName = lineParts[1];
                                    
                                    if (string.IsNullOrWhiteSpace(spawnName)) continue;
                                    
                                    if (Enum.TryParse(lineParts[0], out Frequency freq))
                                    {
                                        spawn.Add((freq, spawnName));
                                    }
                                }
                            }

                            StaticSpawnList.Add(new StaticEntity(staticName, spawn));
                        }

                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading StaticSpawnList: {ex.Message}");
            }
            finally
            {
                XMLSpawnUtility.LoadSpawnerList();
            }
        }

        internal static void InitiateWorldTileSpawn()
        {
            for (int i = 0; i < 6; i++)
            {
                _ = new WorldEntity((GameMap)i);
            }
        }

        internal static string ConvertListString(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            text = text.ToLower().Trim();

            if (text == "void")
            {
                return "_void";
            }

            if (text.Contains(' '))
            {
                text = text.Replace(' ', '_');
            }

            return text;
        }
    }
}
