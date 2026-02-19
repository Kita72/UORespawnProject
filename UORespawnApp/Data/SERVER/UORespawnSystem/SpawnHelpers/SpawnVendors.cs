using System;
using System.IO;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Custom.UORespawnSystem.Entities;
using Server.Custom.UORespawnSystem.Mobiles;
using Server.Custom.UORespawnSystem.SpawnUtility;
using Server.Engines.Quests.Naturalist;
using Server.Engines.Quests;
using System.Text;
using System.Linq;

namespace Server.Custom.UORespawnSystem.SpawnHelpers
{
    internal static class SpawnVendors
    {
        private static readonly Dictionary<int, List<VendorEntity>> SignLocations = new Dictionary<int, List<VendorEntity>>();

        internal static readonly List<int> VendorSpawnList = new List<int>();

        private static readonly string VendorSpawnFile = Path.Combine(UORespawnSettings.UOR_DATA, "UOR_VendorSpawn.txt");

        internal static void LoadAllSigns()
        {
            SignLocations.Clear();

            foreach (Map map in Map.Maps)
            {
                if (map != null && map != Map.Internal && !SignLocations.ContainsKey(map.MapID))
                {
                    SignLocations[map.MapID] = new List<VendorEntity>();
                }
            }

            int count = 0;

            foreach (var item in World.Items.Values)
            {
                if (item is Sign sign)
                {
                    var signType = GetSignType(sign);

                    if (ValidateSign(signType))
                    {
                        SignLocations[sign.Map.MapID].Add(new VendorEntity(signType, GetFacing(sign), sign.Location));

                        count++;
                    }
                }
            }

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, $"{count} Signs Loaded!");
        }

        private static SignFacing GetFacing(Sign sign)
        {
            if (sign.ItemID % 2 == 0)
            {
                return SignFacing.North;
            }
            else
            {
                return SignFacing.West;
            }
        }

        private static bool ValidateSign(SignType signType)
        {
            switch (signType)
            {
                case SignType.DarkWoodenPost: return false;
                case SignType.LightWoodenPost: return false;
                case SignType.MetalPostC: return false;
                case SignType.MetalPostB: return false;
                case SignType.MetalPostA: return false;
                case SignType.MetalPost: return false;
                case SignType.WoodenSign: return false;
                case SignType.BrassSign: return false;
            }

            return true;
        }

        private static SignType GetSignType(Sign sign)
        {
            var CleanedSignName = sign.ItemData.Name;

            if (CleanedSignName.Contains("\'") || CleanedSignName.Contains(" "))
            {
                CleanedSignName = CleanedSignName.Replace('\'', ' ').Replace(" ", "");
            }

            try
            {
                return (SignType)Enum.Parse(typeof(SignType), CleanedSignName);
            }
            catch
            {
                return SignType.WoodenSign;
            }   
        }

        internal static void TrySpawnVendors(bool isLoaded)
        {
            if (SignLocations.Count == 0) return;

            if (UORespawnSettings.ENABLE_VENDOR_SPAWN)
            {
                if (!isLoaded)
                {
                    int count = 0;

                    foreach (Map map in Map.Maps)
                    {
                        if (map != null)
                        {
                            if (SignLocations.ContainsKey(map.MapID) && SignLocations[map.MapID]?.Count > 0)
                            {
                                foreach (var entity in SignLocations[map.MapID])
                                {
                                    entity.Spawn(map);

                                    count++;
                                }
                            }
                        }
                    }

                    AddVendorSpawn();

                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"{count} Signs Spawned with Vendors!");
                }
            }
            else
            {
                if (isLoaded)
                {
                    CleanUpVendors();

                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "Removed All Vendors!");
                }
            }
        }

        internal static void CleanUpVendors()
        {
            if (VendorSpawnList.Count > 0)
            {
                for (int i = 0; i < VendorSpawnList.Count; i++)
                {
                    if (World.Mobiles[VendorSpawnList[i]] is BaseCreature bc)
                    {
                        if (!bc.Deleted)
                        {
                            bc.Delete();
                        }
                    }
                }

                VendorSpawnList.Clear();
            }
        }

        private static readonly StringBuilder SB = new StringBuilder();

        internal static void AddVendorSpawn()
        {
            SB.Clear();

            if (VendorSpawnList?.Count > 0)
            {
                foreach (var spawn in VendorSpawnList)
                {
                    SB.AppendLine(spawn.ToString());
                }

                File.WriteAllText(VendorSpawnFile, SB.ToString());
            }
        }

        internal static bool LoadVendorSpawn()
        {
            if (File.Exists(VendorSpawnFile))
            {
                VendorSpawnList.Clear();

                foreach (var line in File.ReadAllLines(VendorSpawnFile))
                {
                    VendorSpawnList.Add(Int32.Parse(line.Split(' ').First()));
                }
            }

            return VendorSpawnList.Count() > 0;
        }

        internal static List<BaseCreature> GetVendors(SignType sign)
        {
            switch (sign)
            {
                // Vendors
                case SignType.Library:      return new List<BaseCreature> { new KeeperOfChivalry(), new Mapmaker(), new Naturalist(), new TownNPC() };
                case SignType.Bakery:       return new List<BaseCreature> { new Baker(), new Cook(), new Beekeeper(), new TownNPC() };
                case SignType.Tailor:       return new List<BaseCreature> { new Furtrader(), new LeatherWorker(), new Tailor(), new Tanner(), new Weaver(), new TownNPC() };
                case SignType.Tinker:       return new List<BaseCreature> { new Glassblower(), new GolemCrafter(), new Tinker() };
                case SignType.Butcher:      return new List<BaseCreature> { new Butcher(), new Cook(), new TownNPC() };
                case SignType.Healer:       return new List<BaseCreature> { new Healer(), new Scribe(), new TownNPC() };
                case SignType.Mage:         return new List<BaseCreature> { new Alchemist(), new Mage(), new TownNPC() };
                case SignType.Woodworker:   return new List<BaseCreature> { new Architect(), new Carpenter(), new RealEstateBroker(), new TownNPC() };
                case SignType.Customs:      return new List<BaseCreature> { new Fisherman(), new HarborMaster(), new Mapmaker(), new TownNPC() };
                case SignType.Inn:          return new List<BaseCreature> { new InnKeeper(), new Cook(), new TownNPC() };
                case SignType.Shipwright:   return new List<BaseCreature> { new Fisherman(), new Shipwright(), new TownNPC() };
                case SignType.Stables:      return new List<BaseCreature> { new AnimalTrainer(), new Veterinarian(), new TownNPC() };
                case SignType.BarberShop:   return new List<BaseCreature> { new HairStylist(), new Noble(), new TownNPC() };
                case SignType.Bard:         return new List<BaseCreature> { new Bard(), new Actor(), new TownNPC() };
                case SignType.Fletcher:     return new List<BaseCreature> { new Bowyer(), new Huntsman(), new TownNPC() };
                case SignType.Armourer:     return new List<BaseCreature> { new Armorer(), new Weaponsmith(), new TownNPC() };
                case SignType.Jeweler:      return new List<BaseCreature> { new Jeweler(), new Glassblower(), new TownNPC() };
                case SignType.Tavern:       return new List<BaseCreature> { new Barkeeper(), new Cook(), new TavernKeeper(), new TownNPC() };
                case SignType.ReagentShop:  return new List<BaseCreature> { new Alchemist(), new Herbalist(), new Scribe(), new TownNPC() };
                case SignType.Blacksmith:   return new List<BaseCreature> { new Blacksmith(), new IronWorker(), new TownNPC() };
                case SignType.Painter:      return new List<BaseCreature> { new Artist(), new HireBeggar(), new TownNPC() };
                case SignType.Provisioner:  return new List<BaseCreature> { new Cobbler(), new Provisioner(), new TownNPC() };
                case SignType.Bowyer:       return new List<BaseCreature> { new Bowyer(), new Huntsman(), new TownNPC() };
                // Guild Masters
                case SignType.ArmamentsGuild:       return new List<BaseCreature> { new BlacksmithGuildmaster(), new Tinker(), new TownNPC() };
                case SignType.ArmourersGuild:       return new List<BaseCreature> { new BlacksmithGuildmaster(), new Armorer(), new TownNPC() };
                case SignType.BlacksmithsGuild:     return new List<BaseCreature> { new BlacksmithGuildmaster(), new Blacksmith(), new TownNPC() };
                case SignType.WeaponsGuild:         return new List<BaseCreature> { new BlacksmithGuildmaster(), new Weaponsmith(), new TownNPC() };
                case SignType.BardicGuild:          return new List<BaseCreature> { new BardGuildmaster(), new Bard(), new TownNPC() };
                case SignType.BartersGuild:         return new List<BaseCreature> { new MerchantGuildmaster(), new Merchant(), new TownNPC() };
                case SignType.ProvisionersGuild:    return new List<BaseCreature> { new MerchantGuildmaster(), new Provisioner(), new TownNPC() };
                case SignType.TradersGuild:         return new List<BaseCreature> { new MerchantGuildmaster(), new Furtrader(), new TownNPC() };
                case SignType.CooksGuild:           return new List<BaseCreature> { new MerchantGuildmaster(), new Cook(), new TownNPC() };
                case SignType.HealersGuild:         return new List<BaseCreature> { new HealerGuildmaster(), new Healer(), new TownNPC() };
                case SignType.MagesGuild:           return new List<BaseCreature> { new MageGuildmaster(), new Mage(), new TownNPC() };
                case SignType.SorcerersGuild:       return new List<BaseCreature> { new MageGuildmaster(), new Mage(), new TownNPC() };
                case SignType.IllusionistGuild:     return new List<BaseCreature> { new MageGuildmaster(), new Mage(), new TownNPC() };
                case SignType.MinersGuild:          return new List<BaseCreature> { new MinerGuildmaster(), new Miner(), new TownNPC() };
                case SignType.ArchersGuild:         return new List<BaseCreature> { new RangerGuildmaster(), new Huntsman(), new TownNPC() };
                case SignType.SeamensGuild:         return new List<BaseCreature> { new FisherGuildmaster(), new Fisherman(), new TownNPC() };
                case SignType.FishermensGuild:      return new List<BaseCreature> { new FisherGuildmaster(), new Fisherman(), new TownNPC() };
                case SignType.SailorsGuild:         return new List<BaseCreature> { new FisherGuildmaster(), new Mapmaker(), new TownNPC() };
                case SignType.ShipwrightsGuild:     return new List<BaseCreature> { new MerchantGuildmaster(), new Shipwright(), new TownNPC() };
                case SignType.TailorsGuild:         return new List<BaseCreature> { new TailorGuildmaster(), new Tailor(), new TownNPC() };
                case SignType.ThievesGuild:         return new List<BaseCreature> { new ThiefGuildmaster(), new Thief(), new TownNPC() };
                case SignType.RoguesGuild:          return new List<BaseCreature> { new ThiefGuildmaster(), new Huntsman(), new TownNPC() };
                case SignType.AssassinsGuild:       return new List<BaseCreature> { new ThiefGuildmaster(), new Thief(), new TownNPC() };
                case SignType.TinkersGuild:         return new List<BaseCreature> { new TinkerGuildmaster(), new Tinker(), new TownNPC() };
                case SignType.WarriorsGuild:        return new List<BaseCreature> { new WarriorGuildmaster(), new HireFighter(), new TownNPC() };
                case SignType.CavalryGuild:         return new List<BaseCreature> { new WarriorGuildmaster(), new HirePaladin(), new TownNPC() };
                case SignType.FightersGuild:        return new List<BaseCreature> { new WarriorGuildmaster(), new HireFighter(), new TownNPC() };
                case SignType.MerchantsGuild:       return new List<BaseCreature> { new MerchantGuildmaster(), new Merchant(), new TownNPC() };
                // Misc
                case SignType.Bank:     return new List<BaseCreature> { new Banker(), new Minter(), new TownNPC() };
                case SignType.Theatre:  return new List<BaseCreature> { new Actor(), new Noble(), new Bard() };

                default: return new List<BaseCreature> { new Healer(), new Noble(), new TownNPC() };
            }
        }
    }
}
