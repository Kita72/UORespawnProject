using UORespawnApp.Scripts.Utilities;
using UORespawnApp.Scripts.Constants;

namespace UORespawnApp
{
    internal static class BestiarySpawnUtility
    {
        // Bestiary files (text-based, server-generated)
        internal static readonly string BestiaryFile = Path.Combine(PathConstants.ResourcesRawPath, PathConstants.SPAWNER_LIST_FILENAME);
        internal static readonly string CustomBestiaryFile = Path.Combine(PathConstants.LocalDataPath, PathConstants.SPAWNER_LIST_FILENAME.Replace(".txt", "_Custom.txt"));

        internal static List<string>? BestiaryNameList { get; private set; }

        /// <summary>
        /// Clear spawn list to force reload from server-generated file
        /// </summary>
        internal static void ClearSpawnList()
        {
            BestiaryNameList?.Clear();
        }

        internal static async Task LoadSpawnList()
        {
            BestiaryNameList ??= [];
            
            if (BestiaryNameList.Count > 0)
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
                            BestiaryNameList.Add(line.Trim());
                        }
                    }
                    BestiaryNameList.Sort();
                    Logger.Info($"Loaded {BestiaryNameList.Count} creatures from custom bestiary");
                    return;
                }
                else
                {
                    // Priority 2: Fallback to embedded bestiary list
                    Logger.Info("Loading embedded bestiary list...");
                    LoadEmbeddedBestiary();
                }
            }
            catch
            {
                Logger.Info("Error Loading bestiary list...");
            }
        }

        internal static async Task SaveCustomBestiary()
        {
            try
            {
                if (BestiaryNameList == null || BestiaryNameList.Count == 0)
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
                BestiaryNameList.Sort();

                // Save to custom file
                await File.WriteAllLinesAsync(CustomBestiaryFile, BestiaryNameList);
                Logger.Info($"Saved {BestiaryNameList.Count} creatures to custom bestiary");
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving custom bestiary", ex);
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
                    Logger.Info("Deleted custom bestiary file");
                }

                // Clear and reload
                BestiaryNameList = null;
                await LoadSpawnList();
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error resetting bestiary", ex);
                return false;
            }
        }

        internal static void AddCreatureToBestiary(string creatureName)
        {
            BestiaryNameList ??= [];

            var trimmedName = creatureName.Trim();

            if (!string.IsNullOrWhiteSpace(trimmedName) && !BestiaryNameList.Contains(trimmedName))
            {
                BestiaryNameList.Add(trimmedName);
                BestiaryNameList.Sort();
            }
        }

        internal static void RemoveCreatureFromBestiary(string creatureName)
        {
            BestiaryNameList?.Remove(creatureName);
        }

        private static void LoadEmbeddedBestiary()
        {
            BestiaryNameList =
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
            
            BestiaryNameList.Sort();

            Logger.Info($"Loaded {BestiaryNameList.Count} creatures from embedded bestiary");
        }
    }
}
