using System;
using System.IO;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Commands;
using Server.Targeting;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using CPA = Server.CommandPropertyAttribute;

namespace Server.Custom.SpawnSystem
{
    internal static class SpawnSysUtility
    {
        internal static int Max_Mobs = SpawnSysSettings.MAX_MOBS;

        private static readonly int min_Range = SpawnSysSettings.MIN_RANGE;

        internal static int Max_Range = SpawnSysSettings.MAX_RANGE;

        private static readonly int max_Crowd = SpawnSysSettings.MAX_CROWD;

        private static readonly double isWater_Chance = SpawnSysSettings.CHANCE_ISWATER;

        internal static Point3D Default_Point = new Point3D(0, 0, 0);

        internal static async Task LoadSpawn(PlayerMobile pm, Map map, Point3D location)
        {
            await Task.Run(() =>
            {
                try
                {
                    var area_Pool = map.GetMobilesInRange(location, Max_Range);

                    int area_MobCount = 0;
                    foreach (var m in area_Pool)
                    {
                        area_MobCount++;
                    }

                    area_Pool.Free();

                    if (area_MobCount < Max_Mobs)
                    {
                        string mob_Name = string.Empty;

                        Point3D spawnPoint = Default_Point;

                        Region region = map.DefaultRegion;

                        bool isWater = false;

                        bool isGoodSpawn = false;

                        do
                        {
                            spawnPoint = GetSpawnPoint(location, min_Range, Max_Range, map);

                            // Cave : Use 'rock' spawn tile type!
                            if (spawnPoint.Z > pm.Location.Z + 20)
                            {
                                spawnPoint.Z = pm.Location.Z;
                            }

                            region = Region.Find(spawnPoint, map);

                            if (region != null && region != map.DefaultRegion)
                            {
                                if (!region.AllowSpawn())
                                {
                                    spawnPoint = Default_Point;
                                }
                            }

                            if (!IsCrowded(map, spawnPoint))
                            {
                                isWater = CanSpawnWater(map, spawnPoint);

                                if (isWater)
                                {
                                    isGoodSpawn = Utility.RandomDouble() < isWater_Chance;
                                }
                                else
                                {
                                    isGoodSpawn = map.CanSpawnMobile(spawnPoint.X, spawnPoint.Y, spawnPoint.Z);
                                }
                            }
                            else
                            {
                                isGoodSpawn = false;
                            }
                        }
                        while (!isGoodSpawn);

                        if (isGoodSpawn)
                        {
                            mob_Name = SpawnSysFactory.GetSpawnName(pm, map, region, spawnPoint, isWater);

                            if (!string.IsNullOrEmpty(mob_Name))
                            {
                                SpawnSysCore.EnqueueSpawn(pm, mob_Name, spawnPoint);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SendConsoleMsg(ConsoleColor.DarkRed, $"Spawn Location Error: {ex.Message}");
                }
            });
        }

        private static Point3D GetSpawnPoint(Point3D center, int minRange, int maxRange, Map map)
        {
            double angle = Utility.RandomDouble() * 2 * Math.PI;

            double distance = Utility.RandomMinMax(minRange, maxRange);

            int randomX = (int)(center.X + distance * Math.Cos(angle) * 2);

            int randomY = (int)(center.Y + distance * Math.Sin(angle) * 2);

            int mapWidth = map.Width;
            int mapHeight = map.Height;

            if (randomX < 0 || randomX > mapWidth || randomY < 0 || randomY > mapHeight)
            {
                return Default_Point;
            }

            return new Point3D(randomX, randomY, map.GetAverageZ(randomX, randomY));
        }

        private static bool IsCrowded(Map map, Point3D location)
        {
            if (location != Default_Point)
            {
                var mobiles = map.GetMobilesInRange(location, min_Range);

                int mobCount = 0;
                foreach (var m in mobiles)
                {
                    mobCount++;
                }

                mobiles.Free();

                return mobCount >= max_Crowd;
            }

            return true;
        }

        internal static bool CanSpawnWater(Map map, Point3D location)
        {
            bool isValid = Spawner.IsValidWater(map, location.X, location.Y, location.Z);

            if (!isValid)
            {
                isValid = Spawner.IsValidWater(map, location.X, location.Y, location.Z - 5);
            }

            return isValid;
        }

        internal static Mobile GetSpawn(ref List<Mobile> mobs, string spawn)
        {
            Type mob_Type = ScriptCompiler.FindTypeByName(Spawner.ParseType(spawn))?? typeof(Rat);

            Mobile mob = null;

            foreach (var m in mobs)
            {
                if (m.GetType().Name == mob_Type.Name)
                {
                    mob = m;
                    break;
                }
            }

            if (mob == null)
            {
                try
                {
                    mob = Build(mob_Type, CommandSystem.Split(mob_Type.Name)) as Mobile;
                }
                catch (Exception ex)
                {
                    SendConsoleMsg(ConsoleColor.DarkRed, $"Build Spawn Error: {ex.Message}");
                }
            }
            else
            {
                if (mobs.Contains(mob))
                {
                    mobs.Remove(mob);
                }
            }

            return mob;
        }

        private static ISpawnable Build(Type type, string[] args)
        {
            bool isISpawnable = typeof(ISpawnable).IsAssignableFrom(type);

            if (!isISpawnable)
            {
                return null;
            }

            Add.FixArgs(ref args);

            string[,] props = null;

            for (int i = 0; i < args.Length; ++i)
            {
                if (Insensitive.Equals(args[i], "set"))
                {
                    int remains = args.Length - i - 1;

                    if (remains >= 2)
                    {
                        props = new string[remains / 2, 2];

                        remains /= 2;

                        for (int j = 0; j < remains; ++j)
                        {
                            props[j, 0] = args[i + (j * 2) + 1];
                            props[j, 1] = args[i + (j * 2) + 2];
                        }

                        Add.FixSetString(ref args, i);
                    }

                    break;
                }
            }

            PropertyInfo[] realProps = null;

            if (props != null)
            {
                realProps = new PropertyInfo[props.GetLength(0)];

                PropertyInfo[] allProps = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                for (int i = 0; i < realProps.Length; ++i)
                {
                    PropertyInfo thisProp = null;

                    string propName = props[i, 0];

                    for (int j = 0; thisProp == null && j < allProps.Length; ++j)
                    {
                        if (Insensitive.Equals(propName, allProps[j].Name))
                            thisProp = allProps[j];
                    }

                    if (thisProp != null)
                    {
                        CPA attr = Properties.GetCPA(thisProp);

                        if (attr != null && AccessLevel.Spawner >= attr.WriteLevel && thisProp.CanWrite && !attr.ReadOnly)
                            realProps[i] = thisProp;
                    }
                }
            }

            ConstructorInfo[] ctors = type.GetConstructors();

            for (int i = 0; i < ctors.Length; ++i)
            {
                ConstructorInfo ctor = ctors[i];

                if (!Add.IsConstructable(ctor, AccessLevel.Spawner))
                    continue;

                ParameterInfo[] paramList = ctor.GetParameters();

                if (args.Length == paramList.Length)
                {
                    object[] paramValues = Add.ParseValues(paramList, args);

                    if (paramValues == null)
                        continue;

                    object built = ctor.Invoke(paramValues);

                    if (built != null && realProps != null)
                    {
                        for (int j = 0; j < realProps.Length; ++j)
                        {
                            if (realProps[j] == null)
                                continue;

                            Properties.InternalSetValue(built, realProps[j], props[j, 1]);
                        }
                    }

                    return (ISpawnable)built;
                }
            }

            return null;
        }

        internal static void CleanUpStatFiles(string folderPath)
        {
            try
            {
                string[] files = Directory.GetFiles(folderPath, "*.txt");

                foreach (string file in files)
                {
                    var fileInfo = new FileInfo(file);

                    int daysDifference = (DateTime.Now - fileInfo.CreationTime).Days;

                    if (daysDifference > 7)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                SendConsoleMsg(ConsoleColor.DarkRed, $"Clean Up Error: {ex.Message}");
            }
        }

        public static string TryGetWetName(Map map, Point3D location)
        {
            string tile = new LandTarget(location, map).Name;

            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(location.X, location.Y, false);

            for (int i = 0; i < staticTiles.Length; ++i)
            {
                var sT = new StaticTarget(location, staticTiles[i].ID);

                if (sT.Name == "water" || sT.Name == "blood")
                {
                    tile = sT.Name;
                }
            }

            return tile;
        }

        private static readonly List<int> nightLabels = new List<int>() { 1042957, 1042950, 1042951, 1042952 };

        internal static bool IsNight(Mobile from)
        {
            Clock.GetTime(from, out int label, out string time);

            if (nightLabels.Contains(label))
            {
                if (int.TryParse(time.Split(':').First(), out var hour) && (hour > 8 || hour < 6))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsSpawnTime(string timedSpawn, int hours)
        {
            // 00:00 AM - 00:59 AM : Witching hour
            // 01:00 AM - 03:59 AM : Middle of night
            // 04:00 AM - 07:59 AM : Early morning
            // 08:00 AM - 11:59 AM : Late morning
            // 12:00 PM - 12:59 PM : Noon
            // 01:00 PM - 03:59 PM : Afternoon
            // 04:00 PM - 07:59 PM : Early evening
            // 08:00 PM - 11:59 AM : Late at night

            if (hours >= 20)
            {
                return timedSpawn == "Late at night";
            }
            else if (hours >= 16)
            {
                return timedSpawn == "Early evening";
            }
            else if (hours >= 13)
            {
                return timedSpawn == "Afternoon";
            }
            else if (hours >= 12)
            {
                return timedSpawn == "Noon";
            }
            else if (hours >= 08)
            {
                return timedSpawn == "Late morning";
            }
            else if (hours >= 04)
            {
                return timedSpawn == "Early morning";
            }
            else if (hours >= 01)
            {
                return timedSpawn == "Middle of night";
            }
            else
            {
                return timedSpawn == "Witching hour";
            }
        }

        internal static void SendConsoleMsg(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;

            Console.WriteLine($"UORespawn: {message}");

            Console.ResetColor();
        }
    }
}
