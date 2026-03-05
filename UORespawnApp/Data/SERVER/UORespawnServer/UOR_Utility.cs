using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Server.Misc;
using Server.Items;
using Server.Mobiles;
using Server.Commands;
using Server.Targeting;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Items;
using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Managers;
using Server.Custom.UORespawnServer.Interfaces;
using Server.Custom.UORespawnServer.Mobiles;
using Server.Custom.UORespawnServer.Helpers;
using Server.Custom.UORespawnServer.Spawners;

using CPA = Server.CommandPropertyAttribute;
using Server.Engines.Harvest;

namespace Server.Custom.UORespawnServer
{
    internal static class UOR_Utility
    {
        private static Dictionary<string, Type> _TypeCache;

        /// <summary>
        /// Gets all spawn owned by UOR_MobSpawner (on-demand query).
        /// Replaces the old _AllSpawns tracking list.
        /// </summary>
        internal static List<BaseCreature> GetAllSpawn()
        {
            return UOR_MobSpawner.GetAllSpawn();
        }

        /// <summary>
        /// Gets count of all UOR mob spawn.
        /// </summary>
        internal static int GetSpawnCount()
        {
            return UOR_MobSpawner.GetCount();
        }

        /// <summary>
        /// Deletes all spawn owned by UOR_MobSpawner.
        /// Used on SHUTDOWN to clean up all mob spawn.
        /// </summary>
        internal static int ClearAllSpawns()
        {
            return UOR_MobSpawner.CleanupAll();
        }

        internal static void InitializeUtility()
        {
            _TypeCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            InitializeQuedLocationList();

            SendMsg(ConsoleColor.Green, "UTILITY-[Initialized]");
        }

        internal static Region GetRegion(Map map, Point3D location)
        {
            return Region.Find(location, map);
        }

        internal static bool IsValidRegion(string name)
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

        internal static LandTile GetTile(Map map, Point3D location)
        {
            return map.Tiles.GetLandTile(location.X, location.Y);
        }

        internal static string GetTileName(Map map, Point3D location)
        {
            return TileHelper.GetTileName(GetTile(map, location).ID, map, location);
        }

        internal static bool HasWater(Map map, Point3D location, out WaterTypes waterType)
        {
            if (GetTileName(map, location) == "water")
            {
                waterType = GetWaterType(GetTile(map, location).ID);

                return true;
            }

            if (GetStatic(map, location, "water") != null)
            {
                waterType = GetWaterType(GetTile(map, location).ID);

                return true;
            }

            waterType = WaterTypes.Shallow;

            return false;
        }

        private static WaterTypes GetWaterType(int id)
        {
            bool water = false;

            int[] deepWater = TileHelper.GetDeepWater();

            for (int i = 0; !water && i < deepWater.Length; i += 2)
            {
                water = (id >= deepWater[i] && id <= deepWater[i + 1]);
            }

            return  water ? WaterTypes.Deep : WaterTypes.Shallow;
        }

        internal static bool HasWeather(Map map, Point3D location, out WeatherTypes weatherType)
        {
            Point2D start = new Point2D(location.X - 1, location.Y - 1);
            Point2D end = new Point2D(start.X + 3, start.Y + 3);
            Rectangle2D spot = new Rectangle2D(start, end);

            weatherType = GetWeatherType(Weather.GetWeatherList(map).Find(w => w.IntersectsWith(spot)));

            return weatherType != WeatherTypes.None;
        }

        private static WeatherTypes GetWeatherType(Weather weather)
        {
            if (weather == null) return WeatherTypes.None;

            if (weather.ChanceOfPercipitation > 0)
            {
                if (weather.ChanceOfExtremeTemperature > 0)
                {
                    if (weather.Temperature > 0) return WeatherTypes.Storm;
                    if (weather.Temperature < 0) return WeatherTypes.Blizzard;
                }
                else
                {
                    if (weather.Temperature > 0) return WeatherTypes.Rain;
                    if (weather.Temperature < 0) return WeatherTypes.Snow;
                }
            }

            return WeatherTypes.None;
        }

        internal static TimeTypes GetTime(Map map, Point3D location)
        {
            Clock.GetTime(map, location.X, location.Y, out int label, out int _);

            return (TimeTypes)label;
        }

        internal static bool IsNight(Map map, Point3D location)
        {
            switch (GetTime(map, location))
            {
                case TimeTypes.Witching_Hour: return true;
                case TimeTypes.Middle_of_Night: return true;
                case TimeTypes.Early_Morning: return false;
                case TimeTypes.Late_Morning: return false;
                case TimeTypes.Noon: return false;
                case TimeTypes.Afternoon: return false;
                case TimeTypes.Early_Evening: return false;
                case TimeTypes.Late_at_Night: return true;
            }

            return false;
        }

        private static StaticTarget GetStaticTarget(Point3D location, int id)
        {
            return new StaticTarget(location, id);
        }

        internal static StaticTarget GetStatic(Map map, Point3D location, string name)
        {
            var statics = map.Tiles.GetStaticTiles(location.X, location.Y);

            for (int i = 0; i < statics.Length; i++)
            {
                if (GetStaticTarget(location, statics[i].ID) is StaticTarget st)
                {
                    if (st.Name == name)
                    {
                        return st;
                    }
                }
            }

            return null;
        }

        private static Dictionary<int, List<StaticTarget>> AllStatics;

        internal static List<(int, Point3D)> GetStaticList(string name)
        {
            List<(int, Point3D)> locations = new List<(int, Point3D)>();

            StaticTarget staticTarg;

            if (AllStatics == null)
            {
                AllStatics = new Dictionary<int, List<StaticTarget>>();

                for (int m = 0; m < Map.Maps.Length; m++)
                {
                    if (Map.Maps[m] == null || Map.Maps[m] == Map.Internal)
                        continue;

                    AllStatics.Add(m, new List<StaticTarget>());

                    for (int w = 0; w < Map.Maps[m].Width; w++)
                    {
                        for (int h = 0; h < Map.Maps[m].Height; h++)
                        {
                            staticTarg = GetStatic(Map.Maps[m], new Point3D(w, h, 0), name);

                            AllStatics[m].Add(staticTarg);

                            if (staticTarg?.Name == name)
                            {
                                locations.Add((m, staticTarg.Location));
                            }
                        }
                    }
                }
            }
            else
            {
                for (int m = 0; m < AllStatics.Count; m++)
                {
                    staticTarg = AllStatics[m].Find(s => s.Name == name);

                    if (staticTarg?.Name == name)
                    {
                        locations.Add((m, staticTarg.Location));
                    }
                }
            }

            return locations;
        }

        internal static Dictionary<Map, List<Rectangle2D>> QuedLocations { get; set; }

        private const int QUEUED_LOCATION_RADIUS = 4;

        private const int MAX_QUEUED_LOCATIONS_PER_MAP = 2000;

        private static void InitializeQuedLocationList()
        {
            if (QuedLocations == null || QuedLocations.Count < 6)
            {
                QuedLocations = new Dictionary<Map, List<Rectangle2D>>();

                for (int i = 0; i < 6; i++)
                {
                    QuedLocations[Map.Maps[i]] = new List<Rectangle2D>();
                }
            }
        }

        internal static void QueueSpawnLocation(Map map, Point3D location)
        {
            if (map == null || map == Map.Internal || location == Point3D.Zero)
            {
                return;
            }

            if (!QuedLocations.TryGetValue(map, out var list) || list == null)
            {
                return;
            }

            list.Add(GetSpawnBox(location, QUEUED_LOCATION_RADIUS));

            if (list.Count > MAX_QUEUED_LOCATIONS_PER_MAP)
            {
                list.RemoveAt(0);
            }
        }

        internal static void ReleaseQueuedSpawnLocation(Map map, Point3D location)
        {
            if (map == null || map == Map.Internal || location == Point3D.Zero)
            {
                return;
            }

            if (QuedLocations != null && QuedLocations.TryGetValue(map, out var list) && list != null)
            {
                list.Remove(GetSpawnBox(location, QUEUED_LOCATION_RADIUS));
            }
        }

        internal static SpawnEntity Locate(PlayerMobile pm, LocationEntity entity)
        {
            try
            {
                int min = UOR_Settings.MIN_RANGE;
                int max = UOR_Settings.MIN_RANGE;
                bool lava = false;

                // 1. Single Loop Strategy: Keep looking until VALID or MAX_ATTEMPTS
                while (entity.ATTEMPTS++ < UOR_Settings.MAX_SPAWN_CHECKS)
                {
                    entity.LOCATION = GetSpawnPoint(pm.Location, min, max, pm.Map, out bool isWater, out lava);

                    if (Utility.RandomDouble() > UOR_Settings.CHANCE_WATER && !IsWaterLimit(isWater))
                    {
                        if (UOR_Settings.ENABLE_DEBUG) entity.REASON = "[Skipped-Water]";
                        continue;
                    }

                    if (entity.LOCATION == Point3D.Zero)
                    {
                        if (UOR_Settings.ENABLE_DEBUG) entity.REASON = "[ZERO]";
                        continue;
                    }

                    // 2. Crowded
                    if (IsCrowded(pm.Map, entity.LOCATION))
                    {
                        if (UOR_Settings.ENABLE_DEBUG) entity.REASON = "[Crowded]";
                        continue;
                    }

                    // 3. Queue Check - Don't spawn on top of recent spawn locations
                    bool isQueued = false;
                    var queuedRects = QuedLocations[pm.Map];
                    var checkPoint = new Point2D(entity.LOCATION.X, entity.LOCATION.Y);

                    for (int i = 0; i < queuedRects.Count; i++)
                    {
                        if (queuedRects[i].Contains(checkPoint))
                        {
                            isQueued = true;
                            break;
                        }
                    }

                    if (isQueued)
                    {
                        if (UOR_Settings.ENABLE_DEBUG) entity.REASON = "[X-Qued]";
                        continue;
                    }

                    // If we reached here, the location is valid!
                    entity.VALID = true;
                    break;
                }

                // 4. Final Processing
                if (entity.VALID)
                {
                    entity.REGION = Region.Find(entity.LOCATION, pm.Map);

                    // Add to queue so the next 'Locate' call avoids this area
                    QueueSpawnLocation(pm.Map, entity.LOCATION);

                    return new SpawnEntity(pm.Map, entity.LOCATION) { HitLava = lava };
                }
                else if (UOR_Settings.ENABLE_DEBUG)
                {
                    var flag = new DebugFlag()
                    {
                        Map = pm.Map,
                        Location = entity.LOCATION
                    };

                    if (entity.REGION != null && entity.REGION.Name != null)
                    {
                        flag.SetInfo(entity.PLAYER, entity.REGION.Name, GetTileName(pm.Map, entity.LOCATION), entity.REASON);
                    }
                    else
                    {
                        flag.SetInfo(entity.PLAYER, "NULL", GetTileName(pm.Map, entity.LOCATION), entity.REASON);
                    }
                }
            }
            catch (Exception ex)
            {
                SendMsg(ConsoleColor.DarkRed, $"Locate()-[{pm.Name}]: {ex.Message}");
            }

            return null;
        }

        private static bool IsWaterLimit(bool isWater)
        {
            if (isWater)
            {
                return GetAllSpawn().Count(bc => bc.CanSwim) > (UOR_Settings.MAX_CROWD * UOR_Settings.SCALE_MOD);
            }

            return isWater;
        }

        internal static Rectangle2D GetSpawnBox(Point3D loc, int rad)
        {
            return new Rectangle2D(loc.X - rad, loc.Y - rad, rad * 2, rad * 2);
        }

        internal static Point3D GetSpawnPoint(Point3D center, int min, int max, Map map, out bool isWater, out bool isLava)
        {
            if (map == null || map == Map.Internal)
            {
                isWater = false;

                isLava = false;

                return Point3D.Zero;
            }

            // 1. Get a random angle and distance for a circular "donut" distribution
            double angle = Utility.RandomDouble() * 2.0 * Math.PI;
            double dist = min + (Utility.RandomDouble() * (max - min));

            int x = center.X + (int)(Math.Cos(angle) * dist);
            int y = center.Y + (int)(Math.Sin(angle) * dist);

            // 2. Clamp to map boundaries to prevent OutOfBounds exceptions
            x = Math.Max(0, Math.Min(x, map.Width - 1));
            y = Math.Max(0, Math.Min(y, map.Height - 1));

            // 3. Get the Z and validate the tile
            int z = map.GetAverageZ(x, y);

            // CanSpawnMobile is the best 'all-in-one' check for ServUO. 
            // It checks for LOS, static blocks, and if the surface is walkable.
            if (map.CanSpawnMobile(x, y, z))
            {
                isWater = false;

                isLava = false;

                return new Point3D(x, y, z);
            }
            else
            {
                var specialPoint = new Point3D(x, y, z);

                var name = GetTileName(map, specialPoint);

                if (!string.IsNullOrEmpty(name))
                {
                    switch (name)
                    {
                        case "water":
                            {
                                isWater = true;

                                isLava = false;

                                return specialPoint;
                            }

                        case "lava":
                            {
                                isWater = false;

                                isLava = true;

                                return Point3D.Zero;
                            }
                    }
                }
            }

            // Fallback: If no valid spot found, return Point3D.Zero or center
            isWater = false;

            isLava = false;

            return Point3D.Zero;
        }

        private static bool IsCrowded(Map map, Point3D location)
        {
            if (location != Point3D.Zero)
            {
                return SpawnInRange(map, location, UOR_Settings.MIN_RANGE) > UOR_Settings.MAX_CROWD;
            }

            return true;
        }

        internal static bool CanSpawnWater(Map map, Point3D location)
        {
            return Spawner.IsValidWater(map, location.X, location.Y, location.Z);
        }

        internal static bool PlayersInRange(Map map, Point3D location, int range)
        {
            var players = map.GetClientsInRange(location, range);

            bool hasPlayers = players.Any();

            players.Free();

            return hasPlayers;
        }

        /// <summary>
        /// Counts non-vendor spawn in range for crowd checking.
        /// Excludes all BaseVendor subclasses - vendors add town life but shouldn't block main spawn.
        /// </summary>
        internal static int SpawnInRange(Map map, Point3D location, int range)
        {
            var spawns = map.GetMobilesInRange(location, range)
                            .Where(m => !(m is BaseVendor))
                            .ToList();

            int count = spawns.Count;

            return count;
        }

        internal static bool IsValidPlayer(PlayerMobile pm)
        {
            return pm != null && !pm.Deleted && pm.Map != null && pm.Map != Map.Internal;
        }

        internal static bool IsValidSpawn(int serial, out Mobile mobile)
        {
            if (World.Mobiles.ContainsKey(serial))
            {
                if (World.Mobiles[serial] is Mobile m && !m.Deleted && m.Alive)
                {
                    if (m is BaseCreature bc)
                    {
                        mobile = bc;

                        return !bc.Controlled;
                    }
                    mobile = m;

                    return true;
                }
            }

            mobile = null;

            return false;
        }

        internal static string GetSpawnName(ISpawnEntity entity, SpawnEntity spawn)
        {
            if (entity == null)
                return string.Empty;

            switch (spawn.FrequencyType)
            {
                case FrequencyTypes.Water:
                    if (entity.WaterList?.Count > 0)
                        return $"{entity.WaterList[Utility.Random(entity.WaterList.Count)]}";
                    break;

                case FrequencyTypes.Weather:
                    if (spawn.WeatherType != entity.WeatherType) 
                        return string.Empty;
                    if (entity.WeatherList?.Count > 0)
                        return $"{entity.WeatherList[Utility.Random(entity.WeatherList.Count)]}";
                    break;

                case FrequencyTypes.Timed:
                    if (spawn.TimeType != entity.TimedType) 
                        return string.Empty;
                    if (entity.TimedList?.Count > 0)
                        return $"{entity.TimedList[Utility.Random(entity.TimedList.Count)]}";
                    break;

                case FrequencyTypes.Common:
                    if (entity.CommonList?.Count > 0)
                        return $"{entity.CommonList[Utility.Random(entity.CommonList.Count)]}";
                    break;

                case FrequencyTypes.UnCommon:
                    if (entity.UnCommonList?.Count > 0)
                        return $"{entity.UnCommonList[Utility.Random(entity.UnCommonList.Count)]}";
                    break;

                case FrequencyTypes.Rare:
                    if (entity.RareList?.Count > 0)
                        return $"{entity.RareList[Utility.Random(entity.RareList.Count)]}";
                    break;
            }

            return string.Empty;
        }

        internal static void AddSpawnToWorld(Mobile spawn, Map map, Point3D location, bool isRecycled = false)
        {
            if (!isRecycled)
            {
                spawn.OnBeforeSpawn(location, map);
            }

            spawn.MoveToWorld(location, map);

            if (!isRecycled)
            {
                spawn.OnAfterSpawn();
            }

            // Assign UOR_MobSpawner ownership via ISpawner pattern
            // Only claim if not already owned (recycled spawn keep their ISpawner)
            if (spawn is BaseCreature bc && bc.Spawner != UOR_MobSpawner.Instance)
            {
                UOR_MobSpawner.Instance.Claim(bc, location);
            }

            // Visual effect
            if (UOR_Settings.ENABLE_SPAWN_EFFECTS)
            {
                Effects.SendLocationEffect(spawn.Location, spawn.Map, 0x375A, 15, 0, 0);
            }
        }

        /// <summary>
        /// Create a new spawn
        /// </summary>
        internal static Mobile CreateSpawn(string spawnName)
        {
            if (string.IsNullOrEmpty(spawnName))
            {
                return null;
            }

            string parsedName = Spawner.ParseType(spawnName);

            if (!_TypeCache.TryGetValue(parsedName, out Type mob_Type))
            {
                mob_Type = ScriptCompiler.FindTypeByName(parsedName);

                if (mob_Type == null)
                {
                    if (UOR_Settings.ENABLE_DEBUG)
                    {
                        mob_Type = typeof(PlaceHolder); 
                    }
                    else
                    {
                        mob_Type = typeof(WanderingHealer);
                    }
                }

                _TypeCache[parsedName] = mob_Type;
            }

            Mobile mob = null;

            try
            {
                mob = Build(mob_Type, CommandSystem.Split(mob_Type.Name)) as Mobile;

                if (mob is PlaceHolder ph)
                {
                    ph.SpawnName = $"{spawnName}:{parsedName}";
                }
            }
            catch (Exception ex)
            {
                SendMsg(ConsoleColor.Red, $"CreateSpawn()-[{parsedName}]: {ex.Message}");
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

        internal static void CleanUpOldFiles(string path, int days = 7)
        {
            try
            {
                string[] files = Directory.GetFiles(path, "*.txt");

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
                    SendMsg(ConsoleColor.Green, $"FILE CLEANUP-[Deleted {count} old files]");
                }
            }
            catch (Exception ex)
            {
                SendMsg(ConsoleColor.Red, $"FILE CLEANUP: {ex.Message}");
            }
        }

        internal static void SendMsg(ConsoleColor color, string message)
        {
            bool alwaysEmit = IsSystemColor(color) || IsErrorColor(color);
            bool shouldEmit = alwaysEmit || UOR_Settings.ENABLE_DEBUG;

            if (shouldEmit)
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"[UORespawn]: {message}");
                Console.ResetColor();
                LogManager.LogMessage(color, message);
            }
        }

        /// <summary>
        /// Check if a color represents a system message (console only)
        /// </summary>
        private static bool IsSystemColor(ConsoleColor color)
        {
            return  color == ConsoleColor.Magenta ||
                    color == ConsoleColor.DarkMagenta ||
                    color == ConsoleColor.Blue ||
                    color == ConsoleColor.DarkBlue ||
                    color == ConsoleColor.Cyan ||
                    color == ConsoleColor.DarkCyan;
        }

        private static bool IsErrorColor(ConsoleColor color)
        {
            return color == ConsoleColor.Red ||
                   color == ConsoleColor.DarkRed;
        }
    }
}
