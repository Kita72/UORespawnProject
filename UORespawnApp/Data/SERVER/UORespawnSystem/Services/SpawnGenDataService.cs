using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Server.Custom.UORespawnSystem.Mobiles;
using Server.Custom.UORespawnSystem.SpawnUtility;
using Server.Custom.UORespawnSystem.SpawnHelpers;
using Server.Engines.Doom;
using Server.Mobiles;

namespace Server.Custom.UORespawnSystem.Services
{
    internal static class SpawnGenDataService
    {
        internal static void InitializeSpawnData()
        {
            GenBestiaryList();
            GenRegionList();
            GenSpawnerList();
            GenVendorList();
            SpawnVendors.VendorLoadInitialize();
        }

        // Bestiary List
        private static void GenBestiaryList()
        {
            SaveToFile(Path.Combine(UORespawnDir.BESTIARY_LIST_FILE), typeof(BaseCreature));
        }

        private static void SaveToFile(string filePath, Type type)
        {
            try
            {
                var allTypes = Assembly.GetExecutingAssembly().GetTypes();

                var types = allTypes.Where(t => IsValidSpawn(t, type)).Select(t => t.Name).ToList();

                types.Sort();

                if (types.Count > 0)
                {
                    File.WriteAllLines(filePath, types);
                }

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, $"Bestiary List - Initialized");
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Failed to generate bestiary list - {ex.Message}");

                return;
            }
        }

        private static bool IsValidSpawn(Type t, Type type)
        {
            if (t.Name == nameof(RiftMob) || t.Name == nameof(PlaceHolder)) { return false; }

            if (t.Name.EndsWith("EffectNPC") || t.Name == nameof(AmbushNPC)) { return true; }

            if (t.Name == nameof(GameMaster) || t.Name.StartsWith("Summoned")) { return false; }

            if (t.IsClass)
            {
                if (!t.IsAbstract)
                {
                    if (t.BaseType == type)
                    {
                        return t.GetConstructors().Any(c => c.GetParameters().Length == 0);
                    }
                }
            }

            return false;
        }

        private static void GenRegionList()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                string name;
                Map map;
                Rectangle3D[] area;

                string[] areaParts;
                string location;

                int lastMap = 0;

                for (int i = 0; i < Region.Regions.Count; i++)
                {
                    name = Region.Regions[i].Name;
                    map = Region.Regions[i].Map;
                    area = Region.Regions[i].Area;

                    // Clean off bad regions from end of regions.xml
                    if (map.MapID != lastMap)
                    {
                        if (map.MapID > lastMap) lastMap++;

                        if (map.MapID < lastMap) continue;
                    }

                    if (!string.IsNullOrEmpty(name) && UORespawnUtility.IsValidRegionName(name))
                    {
                        for (int j = 0; j < area.Length; j++)
                        {
                            areaParts = area[j].ToString().Split(',');

                            location = $"{areaParts[0]},{areaParts[1].TrimStart()},{area[j].Width},{area[j].Height})";

                            sb.AppendLine($"{map.MapID}:{name}:{location}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Failed to generate region list - {ex.Message}");
            }

            if (sb.Length > 0)
            {
                File.WriteAllText(Path.Combine(UORespawnDir.REGIONS_LIST_FILE), sb.ToString());

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Region List - Initialized");
            }
            else
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Region List - Empty");
            }
        }

        private static void GenSpawnerList()
        {
            List<string> allSpawners = new List<string>();

            var spawnerList = World.Items.Values.Where(s => s is ISpawner);

            foreach (var spawner in spawnerList)
            {
                if (spawner is Spawner s && s is ISpawner spwnr)
                {
                    // Get spawn names from the spawner's spawn objects
                    string spawnNames = string.Empty;

                    if (s.SpawnObjects != null && s.SpawnObjects.Count > 0)
                    {
                        spawnNames = string.Join("|", s.SpawnObjects.Select(so => so.SpawnName));
                    }

                    allSpawners.Add($"{s.Map}:{s.X}:{s.Y}:{spwnr.HomeRange}:{s.MaxCount}:{spawnNames}");
                }

                if (spawner is XmlSpawner xml && xml is ISpawner xspwnr)
                {
                    // Get spawn names from the XmlSpawner's spawn objects
                    string spawnNames = string.Empty;

                    if (xml.SpawnObjects != null && xml.SpawnObjects.Length > 0)
                    {
                        spawnNames = string.Join("|", xml.SpawnObjects.Select(so => so.TypeName));
                    }

                    allSpawners.Add($"{xml.Map}:{xml.X}:{xml.Y}:{xspwnr.HomeRange}:{xml.MaxCount}:{spawnNames}");
                }
            }

            if (allSpawners.Count > 0)
            {
                File.WriteAllLines(Path.Combine(UORespawnDir.SPAWNERS_LIST_FILE), allSpawners);

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Spawner List - Initialized");
            }
            else
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Region List - Empty");
            }
        }

        private static void GenVendorList()
        {
            StringBuilder sb = new StringBuilder();

            // Vendors
            sb.AppendLine(nameof(Alchemist));
            sb.AppendLine(nameof(AnimalTrainer));
            sb.AppendLine(nameof(Architect));
            sb.AppendLine(nameof(Armorer));
            sb.AppendLine(nameof(Baker));
            sb.AppendLine(nameof(Banker));
            sb.AppendLine(nameof(Bard));
            sb.AppendLine(nameof(Barkeeper));
            sb.AppendLine(nameof(Beekeeper));
            sb.AppendLine(nameof(Blacksmith));
            sb.AppendLine(nameof(Bowyer));
            sb.AppendLine(nameof(Butcher));
            sb.AppendLine(nameof(Carpenter));
            sb.AppendLine(nameof(Cobbler));
            sb.AppendLine(nameof(Cook));
            sb.AppendLine(nameof(CustomHairstylist));
            sb.AppendLine(nameof(Farmer));
            sb.AppendLine(nameof(Fisherman));
            sb.AppendLine(nameof(Furtrader));
            sb.AppendLine(nameof(Gardener));
            sb.AppendLine(nameof(Glassblower));
            sb.AppendLine(nameof(GolemCrafter));
            sb.AppendLine(nameof(HairStylist));
            sb.AppendLine(nameof(Healer));
            sb.AppendLine(nameof(Herbalist));
            sb.AppendLine(nameof(InnKeeper));
            sb.AppendLine(nameof(IronWorker));
            sb.AppendLine(nameof(Jeweler));
            sb.AppendLine(nameof(LeatherWorker));
            sb.AppendLine(nameof(Mage));
            sb.AppendLine(nameof(Mapmaker));
            sb.AppendLine(nameof(Miner));
            sb.AppendLine(nameof(Monk));
            sb.AppendLine(nameof(Mystic));
            sb.AppendLine(nameof(Necromancer));
            sb.AppendLine(nameof(Provisioner));
            sb.AppendLine(nameof(Rancher));
            sb.AppendLine(nameof(Ranger));
            sb.AppendLine(nameof(RealEstateBroker));
            sb.AppendLine(nameof(Scribe));
            sb.AppendLine(nameof(Shipwright));
            sb.AppendLine(nameof(StoneCrafter));
            sb.AppendLine(nameof(Tailor));
            sb.AppendLine(nameof(Tanner));
            sb.AppendLine(nameof(TavernKeeper));
            sb.AppendLine(nameof(Thief));
            sb.AppendLine(nameof(Tinker));
            sb.AppendLine(nameof(Veterinarian));
            sb.AppendLine(nameof(Waiter));
            sb.AppendLine(nameof(Weaponsmith));
            sb.AppendLine(nameof(Weaver));

            // Guildmasters
            sb.AppendLine(nameof(BardGuildmaster));
            sb.AppendLine(nameof(BlacksmithGuildmaster));
            sb.AppendLine(nameof(FisherGuildmaster));
            sb.AppendLine(nameof(HealerGuildmaster));
            sb.AppendLine(nameof(MageGuildmaster));
            sb.AppendLine(nameof(MerchantGuildmaster));
            sb.AppendLine(nameof(MinerGuildmaster));
            sb.AppendLine(nameof(RangerGuildmaster));
            sb.AppendLine(nameof(TailorGuildmaster));
            sb.AppendLine(nameof(ThiefGuildmaster));
            sb.AppendLine(nameof(TinkerGuildmaster));
            sb.AppendLine(nameof(WarriorGuildmaster));

            File.WriteAllText(Path.Combine(UORespawnDir.VENDORS_LIST_FILE), sb.ToString());

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Vendor List - Initialized");
        }
    }
}
