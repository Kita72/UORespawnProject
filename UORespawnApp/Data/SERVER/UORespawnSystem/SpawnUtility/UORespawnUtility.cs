using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Commands;
using Server.Targeting;

using Server.Custom.UORespawnSystem.Mobiles;
using Server.Custom.UORespawnSystem.Services;
using Server.Custom.UORespawnSystem.SpawnHelpers;
using Server.Custom.UORespawnSystem.Interfaces;
using Server.Custom.UORespawnSystem.Enums;

using CPA = Server.CommandPropertyAttribute;
using System.Linq;
using Server.Network;

namespace Server.Custom.UORespawnSystem.SpawnUtility
{
    internal static class UORespawnUtility
    {
        internal static readonly Point3D Default_Point = new Point3D(0, 0, 0);

        private static readonly Dictionary<string, Type> _TypeCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        internal static bool IsValidPlayer(PlayerMobile pm)
        {
            return (pm != null && !pm.Deleted && pm.Map != null && pm.Map != Map.Internal);
        }

        internal static void SetSpawnList(ISpawnEntity entity, Frequency freq, List<string> spawnlist)
        {
            switch (freq)
            {
                case Frequency.Water:
                    {
                        if (entity.WaterSpawnList == null) entity.WaterSpawnList = new ArrayList();

                        entity.WaterSpawnList.AddRange(spawnlist);

                        break;
                    }
                case Frequency.Weather:
                    {
                        if (entity.WeatherSpawnList == null) entity.WeatherSpawnList = new ArrayList();

                        entity.WeatherSpawnList.AddRange(spawnlist);

                        break;
                    }
                case Frequency.Timed:
                    {
                        if (entity.TimedSpawnList == null) entity.TimedSpawnList = new ArrayList();

                        entity.TimedSpawnList.AddRange(spawnlist);

                        break;
                    }
                case Frequency.Common:
                    {
                        if (entity.CommonSpawnList == null) entity.CommonSpawnList = new ArrayList();

                        entity.CommonSpawnList.AddRange(spawnlist);

                        break;
                    }
                case Frequency.UnCommon:
                    {
                        if (entity.UnCommonSpawnList == null) entity.UnCommonSpawnList = new ArrayList();

                        entity.UnCommonSpawnList.AddRange(spawnlist);

                        break;
                    }
                case Frequency.Rare:
                    {
                        if (entity.RareSpawnList == null) entity.RareSpawnList = new ArrayList();

                        entity.RareSpawnList.AddRange(spawnlist);

                        break;
                    }
            }
        }

        internal static void LoadSpawn(PlayerMobile pm, Map map, Point3D location)
        {
            string mob_Name;

            Region region = map.DefaultRegion;
            Point3D spawnPoint = Default_Point;

            bool isWater = false;
            bool isGoodSpawn;

            int attempts = 0;

            try
            {
                do
                {
                    if (attempts++ > UORespawnSettings.MAX_SPAWN_CHECKS)
                    {
                        isGoodSpawn = false;
                        break;
                    }

                    spawnPoint = GetSpawnPoint(location, UORespawnSettings.MIN_RANGE, UORespawnSettings.MAX_RANGE, map, pm.Direction);

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
                        isWater = SpawnWaterInfo.CanSpawnWater(map, spawnPoint);

                        if (isWater)
                        {
                            isGoodSpawn = isWater;
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
                    mob_Name = SpawnFactory.GetSpawnName(pm, map, region, spawnPoint, isWater);

                    if (!string.IsNullOrEmpty(mob_Name))
                    {
                        UORespawnCore.EnqueueSpawn(pm, mob_Name, spawnPoint);

                        SpawnFactory.AddStats(pm.Name, map.Name, new Point2D(pm.Location.X, pm.Location.Y), new Point2D(spawnPoint.X, spawnPoint.Y), mob_Name);
                    }
                }
            }
            catch (Exception ex)
            {
                SendConsoleMsg(ConsoleColor.DarkRed, $"Spawn Location Error for player {pm?.Name ?? "Unknown"}: {ex.Message}");
            }
        }

        internal static Point3D GetSpawnPoint(Point3D center, int min, int max, Map map)
        {
            var rndX = Utility.RandomMinMax(min, max);
            var rndY = Utility.RandomMinMax(min, max);

            int x = center.X + (Utility.RandomBool() ? rndX : -rndX);
            int y = center.Y + (Utility.RandomBool() ? rndY : -rndY);

            return new Point3D(x, y, map.GetAverageZ(x, y));
        }

        internal static Point3D GetSpawnPoint(Point3D center, int min, int max, Map map, Direction direction)
        {
            var range = Utility.RandomMinMax(min, max);

            var roll = Utility.RandomMinMax(0, 3);

            if (roll == 0)
            {
                direction = Utility.RandomList(
                    direction,
                    Direction.North,
                    Direction.East,
                    Direction.South,
                    Direction.West,
                    Direction.Up,
                    Direction.Left,
                    Direction.Down,
                    Direction.Right,
                    direction);
            }

            var mod = (Utility.RandomBool() ? roll : -roll);

            int x;
            int y;

            switch (direction & Direction.Mask)
            {
                case Direction.North:
                    x = center.X + mod;
                    y = center.Y - range;
                    break;
                case Direction.Right:
                    x = center.X + range + mod;
                    y = center.Y - range;
                    break;
                case Direction.East:
                    x = center.X + range;
                    y = center.Y + mod;
                    break;
                case Direction.Down:
                    x = center.X + range + mod;
                    y = center.Y + range;
                    break;
                case Direction.South:
                    x = center.X + mod;
                    y = center.Y + range;
                    break;
                case Direction.Left:
                    x = center.X - range;
                    y = center.Y + range + mod;
                    break;
                case Direction.West:
                    x = center.X - range;
                    y = center.Y + mod;
                    break;
                case Direction.Up:
                    x = center.X - range;
                    y = center.Y - range + mod;
                    break;
                default:
                    return GetSpawnPoint(center, min, max, map);
            }

            return new Point3D(x, y, map.GetAverageZ(x, y));
        }

        private static bool IsCrowded(Map map, Point3D location)
        {
            if (location != Default_Point)
            {
                var mobiles = map.GetMobilesInRange(location, UORespawnSettings.MIN_RANGE);

                int mobCount = 0;

                foreach (var m in mobiles)
                {
                    if (m is BaseCreature bc && bc.GetType() != typeof(BaseVendor))
                        mobCount++;
                }

                mobiles.Free();

                return mobCount >= UORespawnSettings.MAX_CROWD;
            }

            return true;
        }

        internal static string GetSpawnFromList(ISpawnEntity entity, Frequency freq)
        {
            if (!ValidateSpawnList(entity, freq)) return string.Empty;

            switch (freq)
            {
                case Frequency.Water:
                    {
                        if (UORespawnSettings.CHANCE_WATER > Utility.RandomDouble())
                        {
                            return entity.WaterSpawnList[Utility.Random(entity.WaterSpawnList.Count)].ToString();
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                case Frequency.Weather:
                    {
                        if (UORespawnSettings.CHANCE_WEATHER > Utility.RandomDouble())
                        {
                            return entity.WeatherSpawnList[Utility.Random(entity.WeatherSpawnList.Count)].ToString();
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                case Frequency.Timed:
                    {
                        if (UORespawnSettings.CHANCE_TIMED > Utility.RandomDouble()) // TODO: Need to add a Timed Chance to settings
                        {
                            return entity.TimedSpawnList[Utility.Random(entity.TimedSpawnList.Count)].ToString(); ;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                case Frequency.Common:
                    {
                        if (UORespawnSettings.CHANCE_COMMON > Utility.RandomDouble())
                        {
                            return entity.CommonSpawnList[Utility.Random(entity.CommonSpawnList.Count)].ToString(); ;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                case Frequency.UnCommon:
                    {
                        if (UORespawnSettings.CHANCE_UNCOMMON > Utility.RandomDouble())
                        {
                            return entity.UnCommonSpawnList[Utility.Random(entity.UnCommonSpawnList.Count)].ToString(); ;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                case Frequency.Rare:
                    {
                        if (UORespawnSettings.CHANCE_RARE > Utility.RandomDouble())
                        {
                            return entity.RareSpawnList[Utility.Random(entity.RareSpawnList.Count)].ToString(); ;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
            }

            return string.Empty;
        }

        private static bool ValidateSpawnList(ISpawnEntity entity, Frequency freq)
        {
            switch (freq)
            {
                case Frequency.Water:       return entity.WaterSpawnList != null;
                case Frequency.Weather:     return entity.WeatherSpawnList != null;
                case Frequency.Timed:       return entity.TimedSpawnList != null;
                case Frequency.Common:      return entity.CommonSpawnList != null;
                case Frequency.UnCommon:    return entity.UnCommonSpawnList != null;
                case Frequency.Rare:        return entity.RareSpawnList != null;
            }

            return false;
        }

        internal static string ConvertTileName(string tileName)
        {
            if (tileName.ToLower() == "void")
            {
                tileName = "_void";
            }
            else
            {
                tileName = tileName.Replace(' ', '_');
            }

            return tileName;
        }

        /// <summary>
        /// Create a new spawn
        /// </summary>
        internal static Mobile CreateSpawn(string spawnName, bool isTracked = true)
        {
            string parsedName = Spawner.ParseType(spawnName);

            if (!_TypeCache.TryGetValue(parsedName, out Type mob_Type))
            {
                mob_Type = ScriptCompiler.FindTypeByName(parsedName) ?? typeof(PlaceHolder);

                _TypeCache[parsedName] = mob_Type;
            }

            Mobile mob = null;

            try
            {
                mob = Build(mob_Type, CommandSystem.Split(mob_Type.Name)) as Mobile;

                // Track Spawned
                if (isTracked)
                {
                    UORespawnTracker.AddTrackedSpawn(mob.Serial.Value);
                }
            }
            catch (Exception ex)
            {
                SendConsoleMsg(ConsoleColor.Red, $"ERROR: Failed to create spawn type '{parsedName}' - {ex.Message}");
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

        internal static List<(int, Point3D)> GetStaticLocationsList(string name)
        {
            List<(int, Point3D)> locations = new List<(int, Point3D)>();

            StaticTarget staticTarg;

            StaticTile[] targ;

            Point3D loc;

            for (int l = 0; l < 6; l++)
            {
                for (int i = 0; i < Map.Maps[l].Width; i++)
                {
                    for (int j = 0; j < Map.Maps[l].Width; j++)
                    {
                        targ = Map.Maps[l].Tiles.GetStaticTiles(i, j);

                        if (targ.Length > 0)
                        {
                            for (int k = 0; k < targ.Length; k++)
                            {
                                loc = new Point3D(i, j, targ[k].Z);

                                staticTarg = new StaticTarget(loc, targ[k].ID);

                                if (staticTarg.Name == name)
                                {
                                    locations.Add((l, loc));
                                }
                            }
                        }
                    }
                }
            }

            return locations;
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
                    SendConsoleMsg(ConsoleColor.Green, $"FILE CLEANUP: Deleted {count} old files");
                }
            }
            catch (Exception ex)
            {
                SendConsoleMsg(ConsoleColor.Red, $"ERROR: File cleanup failed - {ex.Message}");
            }
        }

        internal static bool IsValidRegionName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (name.ToLower().EndsWith(" quest")) return false;

                if (name.ToLower().EndsWith(" skill")) return false;

                if (name.ToLower().StartsWith("khaldun ")) return false;

                return true;
            }

            return false;
        }

        private static Dictionary<int, List<Point3D>> graveStones = new Dictionary<int, List<Point3D>>();

        internal static bool HasGraveStone(Map map, Point3D location)
        {
            if (graveStones == null && graveStones.Count == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    graveStones[i] = new List<Point3D>();
                }

                var graveList = GetStaticLocationsList("grave");

                foreach (var grave in graveList)
                {
                    graveStones[grave.Item1].Add(grave.Item2);
                }

                var graveStoneList = GetStaticLocationsList("gravestone");

                foreach (var graveStone in graveStoneList)
                {
                    graveStones[graveStone.Item1].Add(graveStone.Item2);
                }
            }

            if (graveStones != null && graveStones.Count > 0)
            {
                if (graveStones[map.MapID].Contains(location)) return true;
            }

            return false;
        }

        /// <summary>
        /// Send a console message with color-based routing
        /// - Blue/Green/Cyan = Console only (system messages)
        /// - Red/Yellow = Buffer to file if debug enabled (debug messages)
        /// </summary>
        internal static void SendConsoleMsg(ConsoleColor color, string message)
        {
            if (IsSystemColor(color))
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"[UORespawn]: {message}");
                Console.ResetColor();
            }

            SpawnDebugService.BufferDebugMessage(color, message);
        }

        /// <summary>
        /// Check if a color represents a system message (console only)
        /// </summary>
        private static bool IsSystemColor(ConsoleColor color)
        {
            return color == ConsoleColor.Blue ||
                   color == ConsoleColor.DarkBlue ||
                   color == ConsoleColor.Green ||
                   color == ConsoleColor.DarkGreen ||
                   color == ConsoleColor.Cyan ||
                   color == ConsoleColor.DarkCyan;
        }
    }
}
