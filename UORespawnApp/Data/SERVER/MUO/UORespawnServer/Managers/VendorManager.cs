using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using Server.Items;

namespace Server.Custom.UORespawnServer.Managers;
internal static class VendorManager
{
    internal static Dictionary<int, List<(SignType, SignFacing, Point3D)>> SignLocations;
    internal static Dictionary<int, List<(SignType, SignFacing, Point3D)>> HiveLocations;

    private static readonly StringBuilder SB = new();

    private static readonly string SignFile = UOR_DIR.SIGN_DATA_FILE;
    private static readonly string HiveFile = UOR_DIR.HIVE_DATA_FILE;

    internal static void VendorDataInitialize()
    {
        UOR_Utility.SendMsg(ConsoleColor.Green, $"VENDORS-[Loading...");

        SignLocations = [];
        HiveLocations = [];

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
                SignLocations[map.MapID] = [];
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
                    if (string.IsNullOrWhiteSpace(line)){ continue; }

                    var value = line.Split(':');

                    if (value.Length >= 6)
                    {
                        int mapId = int.Parse(value[0]);
                        SignType signType = Enum.Parse<SignType>(value[1]);
                        SignFacing facing = Enum.Parse<SignFacing>(value[2]);
                        int x = int.Parse(value[3]);
                        int y = int.Parse(value[4]);
                        int z = int.Parse(value[5]);

                        if (SignLocations.TryGetValue(mapId, out List<(SignType, SignFacing, Point3D)> sign))
                        {
                            sign.Add((signType, facing, new Point3D(x, y, z)));
                            count++;
                        }
                    }
                }
            }
        }
        else
        {
            Dictionary<int, List<Point2D>> UniqueLocations = [];

            foreach (var item in World.Items.Values)
            {
                if (item is Sign sign)
                {
                    var signType = GetSignType(sign);

                    var point2D = new Point2D(sign.Location.X, sign.Location.Y);

                    if (ValidateSign(signType))
                    {
                        if (!UniqueLocations.TryGetValue(sign.Map.MapID, out List<Point2D> value))
                        {
                            value = [];
                            UniqueLocations.Add(sign.Map.MapID, value);
                        }

                        if (!value.Contains(point2D))
                        {
                            SignLocations[sign.Map.MapID].Add((signType, GetFacing(sign), sign.Location));
                            value.Add(point2D);

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
        return signType switch
        {
            SignType.DarkWoodenPost => false,
            SignType.LightWoodenPost => false,
            SignType.MetalPostC => false,
            SignType.MetalPostB => false,
            SignType.MetalPostA => false,
            SignType.MetalPost => false,
            _ => true,
        };
    }

    private static SignType GetSignType(Sign sign)
    {
        var CleanedSignName = sign.ItemData.Name;

        if (CleanedSignName.Contains('\'') || CleanedSignName.Contains(' '))
        {
            CleanedSignName = CleanedSignName.Replace('\'', ' ').Replace(" ", "");
        }

        try
        {
            return Enum.Parse<SignType>(CleanedSignName);
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

    internal static void LoadAllHives()
    {
        HiveLocations.Clear();

        foreach (Map map in Map.Maps)
        {
            if (map != null && map != Map.Internal && !HiveLocations.ContainsKey(map.MapID))
            {
                HiveLocations[map.MapID] = [];
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
                    if (string.IsNullOrWhiteSpace(line)){ continue; }

                    var value = line.Split(':');

                    if (value.Length >= 4)
                    {
                        int mapId = int.Parse(value[0]);
                        int x = int.Parse(value[1]);
                        int y = int.Parse(value[2]);
                        int z = int.Parse(value[3]);

                        if (HiveLocations.TryGetValue(mapId, out List<(SignType, SignFacing, Point3D)> hive))
                        {
                            hive.Add((SignType.MetalPost, SignFacing.North, new Point3D(x, y, z)));

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
                    hiveCount++;
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
