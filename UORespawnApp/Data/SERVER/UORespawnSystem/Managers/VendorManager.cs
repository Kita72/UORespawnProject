using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using Server.Items;

namespace Server.Custom.UORespawnServer.Managers
{
    internal static class VendorManager
    {
        internal static Dictionary<int, List<(SignType, SignFacing, Point3D)>> SignLocations;
        internal static Dictionary<int, List<(SignType, SignFacing, Point3D)>> HiveLocations;

        private static readonly StringBuilder SB = new StringBuilder();

        private static readonly string SignFile = UOR_DIR.SIGN_DATA_FILE;
        private static readonly string HiveFile = UOR_DIR.HIVE_DATA_FILE;

        internal static void VendorDataInitialize()
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, $"VENDORS-[Loading...");

            SignLocations = new Dictionary<int, List<(SignType, SignFacing, Point3D)>>();
            HiveLocations = new Dictionary<int, List<(SignType, SignFacing, Point3D)>>();

            LoadAllSigns();
            LoadAllHives();

            UOR_Utility.SendMsg(ConsoleColor.Green, $"VENDORS-[Loaded]");
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

            UOR_Utility.SendMsg(ConsoleColor.Green, $"VENDOR SIGNS-[{count} Loaded]");
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
                var hiveList = UOR_Utility.GetStaticList("beehive"); // ITEMID 2330

                if (hiveList != null && hiveList.Count > 0)
                {
                    foreach (var hive in hiveList)
                    {
                        HiveLocations[hive.Item1].Add((SignType.MetalPost, SignFacing.North, hive.Item2));
                    }
                }

                SaveAllHives();
            }

            UOR_Utility.SendMsg(ConsoleColor.Green, $"BEEHIVES-[{hiveCount} Loaded]");
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
    }
}
