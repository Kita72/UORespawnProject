using System;
using System.IO;
using System.Linq;
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

namespace Server.Custom.UORespawnServer;
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
            if (name.InsensitiveEndsWith(" quest"))
            {
                return false;
            }

            if (name.InsensitiveEndsWith(" skill"))
            {
                return false;
            }

            if (name.InsensitiveStartsWith("khaldun "))
            {
                return false;
            }

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

    internal static bool HasWeather(Map map, Rectangle2D spot, out WeatherTypes weatherType)
    {
        weatherType = GetWeatherType(Weather.GetWeatherList(map).Find(w => w.IntersectsWith(spot)));

        return weatherType != WeatherTypes.None;
    }

    private static WeatherTypes GetWeatherType(Weather weather)
    {
        if (weather == null)
        {
            return WeatherTypes.None;
        }

        if (weather.ChanceOfPrecipitation > 0)
        {
            if (weather.ChanceOfExtremeTemperature > 0)
            {
                if (weather.Temperature > 0)
                {
                    return WeatherTypes.Storm;
                }

                if (weather.Temperature < 0)
                {
                    return WeatherTypes.Blizzard;
                }
            }
            else
            {
                if (weather.Temperature > 0)
                {
                    return WeatherTypes.Rain;
                }

                if (weather.Temperature < 0)
                {
                    return WeatherTypes.Snow;
                }
            }
        }

        return WeatherTypes.None;
    }

    internal static TimeTypes GetTime(Map map, Point3D location)
    {
        Clock.GetTime(map, location.X, location.Y, out int label, out int _);

        return (TimeTypes)label;
    }

    internal static bool IsNight(Map map, Point3D location) =>
        GetTime(map, location) switch
        {
            TimeTypes.Witching_Hour or TimeTypes.Middle_of_Night or TimeTypes.Late_at_Night => true,
            _ => false
        };

    private static StaticTarget GetStaticTarget(Point3D location, int id)
    {
        return new StaticTarget(location, id);
    }

    internal static StaticTarget GetStatic(Map map, Point3D location, string name)
    {
        foreach (var tile in map.Tiles.GetStaticTiles(location.X, location.Y))
        {
            if (GetStaticTarget(location, tile.ID) is StaticTarget st && st.Name == name)
            {
                return st;
            }
        }

        return null;
    }

    private static Dictionary<string, List<(int, Point3D)>> _StaticIndex;

    internal static List<(int, Point3D)> GetStaticList(string name)
    {
        _StaticIndex ??= BuildStaticIndex();

        return _StaticIndex.TryGetValue(name, out var list) ? list : [];
    }

    private static Dictionary<string, List<(int, Point3D)>> BuildStaticIndex()
    {
        var index = new Dictionary<string, List<(int, Point3D)>>(StringComparer.OrdinalIgnoreCase);

        for (int m = 0; m < Map.Maps.Length; m++)
        {
            var map = Map.Maps[m];

            if (map == null || map == Map.Internal)
            {
                continue;
            }

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    foreach (var tile in map.Tiles.GetStaticTiles(x, y))
                    {
                        var tileName = GetStaticTarget(new Point3D(x, y, 0), tile.ID).Name;

                        if (string.IsNullOrEmpty(tileName))
                        {
                            continue;
                        }

                        if (!index.TryGetValue(tileName, out var list))
                        {
                            list = [];
                            index[tileName] = list;
                        }

                        list.Add((m, new Point3D(x, y, 0)));
                    }
                }
            }
        }

        SendMsg(ConsoleColor.Cyan, $"STATICS-[Indexed {index.Count} unique tile names]");

        return index;
    }

    internal static SpawnEntity Locate(RespawnerEntity respawner, LocationEntity entity)
    {
        var pm = respawner._Player;

        int min = UOR_Settings.MIN_RANGE;
        int max = UOR_Settings.MAX_RANGE;

        bool lava = false;

        try
        {
            // 1. Single Loop Strategy: Keep looking until VALID or MAX_ATTEMPTS
            bool wantWater = entity.CHANCE < UOR_Settings.CHANCE_WATER;

            while (entity.ATTEMPTS++ < UOR_Settings.MAX_SPAWN_CHECKS)
            {
                entity.LOCATION = GetBestSpawnPoint(pm.Map, pm.Location, min, max, wantWater, out bool isWater, out lava);

                if (wantWater && IsWaterLimit(isWater))
                {
                    if (UOR_Settings.ENABLE_DEBUG)
                    {
                        entity.REASON = "[Skipped-Water]";
                    }

                    continue;
                }

                if (entity.LOCATION == Point3D.Zero)
                {
                    if (UOR_Settings.ENABLE_DEBUG)
                    {
                        entity.REASON = "[ZERO]";
                    }

                    continue;
                }

                // 2. Crowded
                if (IsCrowded(pm.Map, entity.LOCATION))
                {
                    if (UOR_Settings.ENABLE_DEBUG)
                    {
                        entity.REASON = "[Crowded]";
                    }

                    continue;
                }

                // 3. Queue Check - Don't spawn too close to player's recent spawn locations
                if (respawner.IsLocationTooClose(entity.LOCATION, UOR_Settings.MIN_RANGE))
                {
                    if (UOR_Settings.ENABLE_DEBUG)
                    {
                        entity.REASON = "[X-Qued]";
                    }

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

                return new SpawnEntity(pm.Map, entity.LOCATION) { IsLava = lava };
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
        if (!isWater)
        {
            return true; // land tile — skip for water-seeking spawn
        }

        return UOR_MobSpawner.CountSwimmers() >= (UOR_Settings.MAX_CROWD * UOR_Settings.SCALE_MOD);
    }

    internal static Rectangle2D GetSpawnBox(Point3D loc, int rad)
    {
        return new Rectangle2D(loc.X - rad, loc.Y - rad, rad * 2, rad * 2);
    }

    /// <summary>
    /// Cache-first spawn point selection.
    /// Pass <paramref name="wantWater"/> = true to request a water tile.
    /// Falls back to the random-donut scan on a cache miss.
    /// </summary>
    internal static Point3D GetBestSpawnPoint(Map map, Point3D center, int min, int max, bool wantWater, out bool isWater, out bool isLava)
    {
        if (!SpawnLocationCache.IsReady)
            return GetSpawnPoint(center, min, max, map, out isWater, out isLava);

        Point3D cached = SpawnLocationCache.GetInRange(map, center, min, max, wantWater);

        if (cached != Point3D.Zero)
        {
            isWater = wantWater;
            isLava  = false;
            return cached;
        }

        return GetSpawnPoint(center, min, max, map, out isWater, out isLava);
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

    internal static bool PlayersInRange(Map map, Point3D location, int range)
    {
        foreach (var _ in map.GetClientsInRange(location, range))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Counts non-vendor spawn in range for crowd checking.
    /// Excludes all BaseVendor subclasses - vendors add town life but shouldn't block main spawn.
    /// </summary>
    internal static int SpawnInRange(Map map, Point3D location, int range)
    {
        int count = 0;

        foreach (var m in map.GetMobilesInRange(location, range))
        {
            if (m is not BaseVendor || m is WanderingHealer)
            {
                count++;
            }
        }

        return count;
    }

    internal static bool IsValidSpawn(int serial, out Mobile spawn)
    {
        var m = World.FindEntity<Mobile>((Serial)(uint)serial);
        if (m != null && !m.Deleted && m.Alive)
        {
            spawn = m;

            if (m is BaseCreature bc)
            {
                return !bc.Controlled;
            }

            return true;
        }

        spawn = null;

        return false;
    }

    internal static string GetSpawnName(ISpawnEntity entity, SpawnEntity spawn)
    {
        if (entity == null)
        {
            return string.Empty;
        }

        switch (spawn.FrequencyType)
        {
            case FrequencyTypes.Water:
                if (entity.WaterList?.Count > 0)
                {
                    return $"{entity.WaterList.RandomElement()}";
                }

                break;

            case FrequencyTypes.Weather:
                if (spawn.WeatherType != entity.WeatherType)
                {
                    return string.Empty;
                }

                if (entity.WeatherList?.Count > 0)
                {
                    return $"{entity.WeatherList.RandomElement()}";
                }

                break;

            case FrequencyTypes.Timed:
                if (spawn.TimeType != entity.TimedType)
                {
                    return string.Empty;
                }

                if (entity.TimedList?.Count > 0)
                {
                    return $"{entity.TimedList.RandomElement()}";
                }

                break;

            case FrequencyTypes.Common:
                if (entity.CommonList?.Count > 0)
                {
                    return $"{entity.CommonList.RandomElement()}";
                }

                break;

            case FrequencyTypes.UnCommon:
                if (entity.UnCommonList?.Count > 0)
                {
                    return $"{entity.UnCommonList.RandomElement()}";
                }

                break;

            case FrequencyTypes.Rare:
                if (entity.RareList?.Count > 0)
                {
                    return $"{entity.RareList.RandomElement()}";
                }

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

        string parsedName = spawnName;

        if (!_TypeCache.TryGetValue(parsedName, out Type mob_Type))
        {
            mob_Type = AssemblyHandler.FindTypeByName(parsedName);

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
            mob = Build(mob_Type) as Mobile;

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

    private static ISpawnable Build(Type type)
    {
        if (!typeof(ISpawnable).IsAssignableFrom(type))
        {
            return null;
        }

        return type.CreateInstance<ISpawnable>(ci => Attributes.IsConstructible(ci, AccessLevel.Player));
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
