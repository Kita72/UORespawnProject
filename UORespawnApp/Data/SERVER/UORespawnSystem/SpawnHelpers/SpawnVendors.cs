using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Server.Items;
using Server.Mobiles;
using Server.Custom.UORespawnSystem.Mobiles;
using Server.Custom.UORespawnSystem.SpawnUtility;

namespace Server.Custom.UORespawnSystem.SpawnHelpers
{
    internal static class SpawnVendors
    {
        private static Dictionary<int, List<(SignType, SignFacing, Point3D)>> SignLocations;

        private static Dictionary<int, List<(SignType, SignFacing, Point3D)>> HiveLocations;

        internal static List<int> VendorSpawnList;

        private static readonly string SignFile = UORespawnDir.SIGN_DATA_FILE;
        private static readonly string HiveFile = UORespawnDir.HIVE_DATA_FILE;
        private static readonly string VendorSpawnFile = UORespawnDir.VENDOR_SPAWN_FILE;

        internal static void VendorLoadInitialize()
        {
            SignLocations = new Dictionary<int, List<(SignType, SignFacing, Point3D)>>();
            HiveLocations = new Dictionary<int, List<(SignType, SignFacing, Point3D)>>();
            VendorSpawnList = new List<int>();

            LoadAllSigns();
            LoadAllHives();
        }

        internal static void LoadAllSigns()
        {
            SignLocations.Clear();

            foreach (Map map in Map.Maps)
            {
                if (map != null && map != Map.Internal && !SignLocations.ContainsKey(map.MapID))
                {
                    SignLocations[map.MapID] = new List<(SignType, SignFacing, Point3D)>();
                }
            }

            int count = 0;

            if (File.Exists(SignFile))
            {
                var lines = File.ReadLines(SignFile);

                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var value = line.Split(':');

                        if (value.Length >= 6)
                        {
                            int mapId = Int32.Parse(value[0]);
                            SignType signType = (SignType)Enum.Parse(typeof(SignType), value[1]);
                            SignFacing facing = (SignFacing)Enum.Parse(typeof(SignFacing), value[2]);
                            int x = Int32.Parse(value[3]);
                            int y = Int32.Parse(value[4]);
                            int z = Int32.Parse(value[5]);

                            if (SignLocations.ContainsKey(mapId))
                            {
                                SignLocations[mapId].Add((signType, facing, new Point3D(x, y, z)));
                                count++;
                            }
                        }
                    }
                }
            }
            else
            {
                Dictionary<int, List<Point2D>> UniqueLocations = new Dictionary<int, List<Point2D>>();

                foreach (var item in World.Items.Values)
                {
                    if (item is Sign sign)
                    {
                        var signType = GetSignType(sign);

                        var point2D = new Point2D(sign.Location.X, sign.Location.Y);

                        if (ValidateSign(signType))
                        {
                            if (!UniqueLocations.ContainsKey(sign.Map.MapID))
                            {
                                UniqueLocations.Add(sign.Map.MapID, new List<Point2D>());
                            }

                            if (!UniqueLocations[sign.Map.MapID].Contains(point2D))
                            {
                                SignLocations[sign.Map.MapID].Add((signType, GetFacing(sign), sign.Location));

                                UniqueLocations[sign.Map.MapID].Add(point2D);

                                count++;
                            }
                        }
                    }
                }

                SaveAllSigns();
            }

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, $"Vendor Signs - {count} Loaded");
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

        private static void SaveAllSigns()
        {
            SB.Clear();

            foreach (var kvp in SignLocations)
            {
                int mapId = kvp.Key;

                foreach (var entity in kvp.Value)
                {
                    SB.AppendLine($"{mapId}:{entity.Item1}:{entity.Item2}:{entity.Item3.X}:{entity.Item3.Y}:{entity.Item3.Z}");
                }
            }

            File.WriteAllText(SignFile, SB.ToString());
        }

        public static void LoadAllHives()
        {
            HiveLocations.Clear();

            foreach (Map map in Map.Maps)
            {
                if (map != null && map != Map.Internal && !HiveLocations.ContainsKey(map.MapID))
                {
                    HiveLocations[map.MapID] = new List<(SignType, SignFacing, Point3D)>();
                }
            }

            int hiveCount = 0;

            if (File.Exists(HiveFile))
            {
                var lines = File.ReadLines(HiveFile);

                if (lines != null)
                {
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var value = line.Split(':');

                        if (value.Length >= 4)
                        {
                            int mapId = Int32.Parse(value[0]);
                            int x = Int32.Parse(value[1]);
                            int y = Int32.Parse(value[2]);
                            int z = Int32.Parse(value[3]);

                            if (HiveLocations.ContainsKey(mapId))
                            {
                                HiveLocations[mapId].Add((SignType.MetalPost, SignFacing.North, new Point3D(x, y, z)));

                                hiveCount++;
                            }
                        }
                    }
                }
            }
            else
            {
                var hiveList = UORespawnUtility.GetStaticLocationsList("beehive"); // ITEMID 2330

                if (hiveList != null && hiveList.Count > 0)
                {
                    foreach (var hive in hiveList)
                    {
                        HiveLocations[hive.Item1].Add((SignType.MetalPost, SignFacing.North, hive.Item2));
                    }
                }

                SaveAllHives();
            }

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, $"Beehives - {hiveCount} Loaded");
        }

        private static void SaveAllHives()
        {
            SB.Clear();

            foreach (var kvp in HiveLocations)
            {
                int mapId = kvp.Key;

                foreach (var entity in kvp.Value)
                {
                    SB.AppendLine($"{mapId}:{entity.Item3.X}:{entity.Item3.Y}:{entity.Item3.Z}");
                }
            }

            File.WriteAllText(HiveFile, SB.ToString());
        }

        internal static void TrySpawnVendors(bool isLoaded)
        {
            var vendorsData = UORespawnDataBase.VendorSpawns;

            if (vendorsData.Count == 0) return;

            if (UORespawnSettings.ENABLE_VENDOR_SPAWN)
            {
                if (!isLoaded)
                {
                    int signCount = 0;
                    int hiveCount = 0;

                    foreach (Map map in Map.Maps)
                    {
                        if (map != null)
                        {
                            if (vendorsData.ContainsKey(map) && vendorsData[map]?.Count > 0)
                            {
                                foreach (var entity in vendorsData[map])
                                {
                                    entity.Spawn(map);

                                    signCount++;
                                }
                            }
                        }
                    }

                    AddVendorSpawn();

                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"{signCount} Signs Spawned with Vendors!");
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"{hiveCount} Hives Spawned with Vendors!");
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

        internal static int CleanUpVendors()
        {
            int count = 0;

            if (VendorSpawnList.Count > 0)
            {
                for (int i = 0; i < VendorSpawnList.Count; i++)
                {
                    if (World.FindMobile(VendorSpawnList[i]) is BaseCreature bc)
                    {
                        if (bc != null && !bc.Deleted)
                        {
                            bc.Delete();

                            count++;
                        }
                    }
                }

                VendorSpawnList.Clear();
            }

            if (File.Exists(VendorSpawnFile))
            {
                File.Delete(VendorSpawnFile);
            }

            return count;
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

                if (VendorSpawnList.Count > 0 && World.FindMobile(VendorSpawnList[0]) == null)
                {
                    CleanUpVendors();

                    File.Delete(VendorSpawnFile);

                    VendorSpawnList.Clear();

                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "Vendor Spawn -> Refreshed!");
                }
            }

            return VendorSpawnList.Count > 0;
        }

        internal static void ToggleVendorWorking()
        {
            bool isEnabled = UORespawnSettings.ENABLE_VENDOR_NIGHT;

            for (int i = 0; i < VendorSpawnList.Count; i++)
            {
                if (World.FindMobile(VendorSpawnList[i]) is Mobile m)
                {
                    if (m != null)
                    {
                        if (m is BaseVendor bv)
                        {
                            bv.Hidden = isEnabled && SpawnTimeInfo.IsNight(bv);

                            bv.CantWalk = bv.Hidden;

                            if (!isEnabled)
                            {
                                NPCUtility.CheckNightDress(bv);
                            }
                        }
                        else
                        {
                            NPCUtility.CheckNightDress(m);
                        }
                    }
                }
            }
        }
    }
}
