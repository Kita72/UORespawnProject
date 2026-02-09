using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Server.Items;
using Server.Mobiles;
using Server.Commands;
using Server.Targeting;

using static Server.Custom.SpawnSystem.SpawnSysSettings;
using CPA = Server.CommandPropertyAttribute;

namespace Server.Custom.SpawnSystem
{
    internal static class SpawnSysUtility
    {
        internal static Point3D Default_Point = new Point3D(0, 0, 0);

        private static readonly Dictionary<string, Type> _TypeCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        internal static void LoadSpawn(PlayerMobile pm, Map map, Point3D location)
        {
            if (map == null || map == Map.Internal) return;

            try
            {
                IPooledEnumerable eable = map.GetMobilesInRange(location, MAX_RANGE);

                int area_MobCount = 0;

                foreach (Mobile m in eable)
                {
                    if (m.Player || (m is BaseCreature bc && (bc.Controlled || bc.IsStabled))) continue;
                    area_MobCount++;
                }

                eable.Free();

                if (area_MobCount < MAX_MOBS)
                {
                    string mob_Name = string.Empty;

                    Point3D spawnPoint = Default_Point;

                    Region region = map.DefaultRegion;

                    bool isWater = false;

                    bool isGoodSpawn = false;

                    int attempts = 0;
                    int maxAttempts = 10;

                    do
                    {
                        if (attempts++ > maxAttempts)
                        {
                            isGoodSpawn = false;
                            break;
                        }

                        spawnPoint = GetSpawnPoint(location, MIN_RANGE, MAX_RANGE, map);

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
                                isGoodSpawn = Utility.RandomDouble() < CHANCE_WATER;
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
                var mobiles = map.GetMobilesInRange(location, MIN_RANGE);

                int mobCount = 0;
                foreach (var m in mobiles)
                {
                    mobCount++;
                }

                mobiles.Free();

                return mobCount >= MAX_CROWD;
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
            string parsedName = Spawner.ParseType(spawn);

            if (!_TypeCache.TryGetValue(parsedName, out Type mob_Type))
            {
                mob_Type = ScriptCompiler.FindTypeByName(parsedName);

                if (mob_Type == null) mob_Type = typeof(Rat); // Fallback

                _TypeCache[parsedName] = mob_Type;
            }

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

        internal static void CleanUpOldFiles(string folderPath, int days = 7)
        {
            try
            {
                string[] files = Directory.GetFiles(folderPath, "*.txt");

                int count = 0;

                foreach (string file in files)
                {
                    var fileInfo = new FileInfo(file);

                    int daysDifference = (DateTime.Now - fileInfo.CreationTime).Days;

                    if (daysDifference > days)
                    {
                        File.Delete(file);

                        count++;
                    }
                }

                if (count > 0)
                {
                    SendConsoleMsg(ConsoleColor.DarkRed, $"Cleaned Up {count} Files!");
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

            Console.WriteLine($"[UORespawn]: {message}");

            Console.ResetColor();
        }
    }
}
