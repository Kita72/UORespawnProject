using System;
using System.IO;
using System.Collections.Generic;

namespace Server.Custom.UORespawnServer.Managers
{
    /// <summary>
    /// Lightweight spawn-position record stored in the spatial grid.
    /// </summary>
    internal struct SpawnLocation
    {
        internal readonly int X;
        internal readonly int Y;
        internal readonly int Z;
        internal readonly bool IsWater;

        internal SpawnLocation(int x, int y, int z, bool isWater = false) { X = x; Y = y; Z = z; IsWater = isWater; }

        internal Point3D ToPoint3D() => new Point3D(X, Y, Z);
    }

    /// <summary>
    /// Pre-computes and caches valid land spawn locations for every spawnable map.
    /// 
    /// BUILD (first run or manual rebuild):
    ///   Scans every SCAN_STRIDE tile in X/Y, validates with map.CanSpawnMobile(), and
    ///   writes the results to a compact binary file.
    /// 
    /// LOAD (subsequent starts):
    ///   Reads the binary file in milliseconds and builds a spatial hash grid so
    ///   GetInRange() can answer "give me a random valid land tile near (x,y)" in O(1).
    /// 
    /// SPATIAL GRID:
    ///   Dictionary&lt;Point2D, List&lt;SpawnLocation&gt;&gt; per map.
    ///   Cell size = CELL_SIZE tiles; GetInRange checks the cell ring that covers minRange..maxRange.
    /// </summary>
    internal static class SpawnLocationCache
    {
        // ── tunables ─────────────────────────────────────────────────────────
        private const int SCAN_STRIDE  = 20;   // tiles between sampled points (= min buffer)
        private const int CELL_SIZE    = 32;   // spatial-hash cell side in tiles
        private const int CACHE_VERSION = 1;
        // ─────────────────────────────────────────────────────────────────────

        private static Dictionary<Map, Dictionary<Point2D, List<SpawnLocation>>> _Cells;
        private static Dictionary<Map, List<SpawnLocation>> _AllLocations;

        internal static bool IsReady { get; private set; }

        internal static int TotalCount
        {
            get
            {
                if (_AllLocations == null) return 0;

                int n = 0;

                foreach (var kv in _AllLocations)
                    n += kv.Value.Count;

                return n;
            }
        }

        // ── public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Load from binary cache file, or scan and build if the file is absent/stale.
        /// Call this during server startup, after World.Load() has completed.
        /// </summary>
        internal static void Initialize()
        {
            _Cells        = new Dictionary<Map, Dictionary<Point2D, List<SpawnLocation>>>();
            _AllLocations = new Dictionary<Map, List<SpawnLocation>>();
            IsReady       = false;

            string path = UOR_DIR.SPAWN_LOCATION_CACHE_FILE;

            if (File.Exists(path) && TryLoadFromFile(path))
            {
                BuildSpatialGrid();
                IsReady = true;

                UOR_Utility.SendMsg(ConsoleColor.Green,
                    $"SPAWN CACHE-[Loaded {TotalCount:N0} locations across {_AllLocations.Count} maps]");
            }
            else
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow,
                    "SPAWN CACHE-[Building... scanning maps (one-time)]");

                BuildCache();
                SaveToFile(path);
                BuildSpatialGrid();
                IsReady = true;

                UOR_Utility.SendMsg(ConsoleColor.Green,
                    $"SPAWN CACHE-[Built {TotalCount:N0} locations across {_AllLocations.Count} maps - saved to disk]");
            }
        }

        /// <summary>
        /// Force-rebuilds the cache and overwrites the file.
        /// Use when the map layout changes or the file becomes corrupt.
        /// </summary>
        internal static void Rebuild()
        {
            _Cells.Clear();
            _AllLocations.Clear();
            IsReady = false;

            UOR_Utility.SendMsg(ConsoleColor.Yellow, "SPAWN CACHE-[Rebuilding...]");

            BuildCache();
            SaveToFile(UOR_DIR.SPAWN_LOCATION_CACHE_FILE);
            BuildSpatialGrid();
            IsReady = true;

            UOR_Utility.SendMsg(ConsoleColor.Green,
                $"SPAWN CACHE-[Rebuilt {TotalCount:N0} locations - saved to disk]");
        }

        /// <summary>
        /// Clear in-memory data (call before a reload).
        /// </summary>
        internal static void Clear()
        {
            _Cells?.Clear();
            _AllLocations?.Clear();
            IsReady = false;
        }

        /// <summary>
        /// Returns a random cached location within minRange..maxRange tiles of <paramref name="center"/>,
        /// or <see cref="Point3D.Zero"/> if none is found.
        /// Pass <paramref name="wantWater"/> = true to select from water tiles only.
        /// O(1) grid lookup + small constant candidate list.
        /// </summary>
        internal static Point3D GetInRange(Map map, Point3D center, int minRange, int maxRange, bool wantWater = false)
        {
            if (!IsReady || map == null || !_Cells.TryGetValue(map, out var cells))
                return Point3D.Zero;

            int cellRadius = (maxRange / CELL_SIZE) + 1;
            var centerCell = GetCell(center.X, center.Y);

            int minSq = minRange * minRange;
            int maxSq = maxRange * maxRange;

            var candidates = new List<SpawnLocation>(64);

            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dy = -cellRadius; dy <= cellRadius; dy++)
                {
                    var key = new Point2D(centerCell.X + dx, centerCell.Y + dy);

                    if (!cells.TryGetValue(key, out var list))
                        continue;

                    foreach (var loc in list)
                    {
                        if (loc.IsWater != wantWater)
                            continue;

                        int ddx = loc.X - center.X;
                        int ddy = loc.Y - center.Y;
                        int dsq = ddx * ddx + ddy * ddy;

                        if (dsq >= minSq && dsq <= maxSq)
                            candidates.Add(loc);
                    }
                }
            }

            if (candidates.Count == 0)
                return Point3D.Zero;

            return candidates[Utility.Random(candidates.Count)].ToPoint3D();
        }

        // ── private: build ───────────────────────────────────────────────────

        private static void BuildCache()
        {
            foreach (var map in GetSpawnableMaps())
            {
                var locations = new List<SpawnLocation>(4096);

                int w = map.Width;
                int h = map.Height;

                int landCount  = 0;
                int waterCount = 0;

                for (int x = 0; x < w; x += SCAN_STRIDE)
                {
                    for (int y = 0; y < h; y += SCAN_STRIDE)
                    {
                        int z = map.GetAverageZ(x, y);

                        if (map.CanSpawnMobile(x, y, z))
                        {
                            locations.Add(new SpawnLocation(x, y, z));
                            landCount++;
                        }
                        else
                        {
                            string tileName = UOR_Utility.GetTileName(map, new Point3D(x, y, z));

                            if (tileName == "water")
                            {
                                locations.Add(new SpawnLocation(x, y, z, isWater: true));
                                waterCount++;
                            }
                        }
                    }
                }

                if (locations.Count > 0)
                {
                    _AllLocations[map] = locations;

                    UOR_Utility.SendMsg(ConsoleColor.Cyan,
                        $"SPAWN CACHE-[{map.Name}: {landCount:N0} land, {waterCount:N0} water]");
                }
            }
        }

        private static void BuildSpatialGrid()
        {
            foreach (var kv in _AllLocations)
            {
                var map       = kv.Key;
                var locations = kv.Value;
                var cells     = new Dictionary<Point2D, List<SpawnLocation>>(locations.Count / 4);

                foreach (var loc in locations)
                {
                    var cell = GetCell(loc.X, loc.Y);

                    if (!cells.TryGetValue(cell, out var list))
                    {
                        list = new List<SpawnLocation>(8);
                        cells[cell] = list;
                    }

                    list.Add(loc);
                }

                _Cells[map] = cells;
            }
        }

        private static Point2D GetCell(int x, int y)
        {
            return new Point2D(x / CELL_SIZE, y / CELL_SIZE);
        }

        private static Map[] GetSpawnableMaps()
        {
            var result = new List<Map>();

            foreach (var map in Map.Maps)
            {
                if (map != null && map != Map.Internal && map.Width > 0 && map.Height > 0)
                    result.Add(map);
            }

            return result.ToArray();
        }

        // ── private: file I/O ────────────────────────────────────────────────

        private static void SaveToFile(string path)
        {
            try
            {
                using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
                {
                    writer.Write(CACHE_VERSION);
                    writer.Write(_AllLocations.Count);

                    foreach (var kv in _AllLocations)
                    {
                        writer.Write(kv.Key.MapID);
                        writer.Write(kv.Value.Count);

                        foreach (var loc in kv.Value)
                        {
                            writer.Write(loc.X);
                            writer.Write(loc.Y);
                            writer.Write(loc.Z);
                            writer.Write(loc.IsWater);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"SPAWN CACHE-[Save failed: {ex.Message}]");
            }
        }

        private static bool TryLoadFromFile(string path)
        {
            try
            {
                using (var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)))
                {
                    int version = reader.ReadInt32();

                    if (version != CACHE_VERSION)
                    {
                        UOR_Utility.SendMsg(ConsoleColor.Yellow,
                            $"SPAWN CACHE-[Version mismatch (file={version}, expected={CACHE_VERSION}) - rebuilding]");

                        return false;
                    }

                    int mapCount = reader.ReadInt32();

                    for (int m = 0; m < mapCount; m++)
                    {
                        int mapId       = reader.ReadInt32();
                        int locCount    = reader.ReadInt32();

                        Map map = Map.Maps[mapId];

                        if (map == null || map == Map.Internal)
                        {
                            // skip entries for unknown maps (3 ints + 1 bool per record)
                            for (int i = 0; i < locCount; i++)
                            {
                                reader.ReadInt32();
                                reader.ReadInt32();
                                reader.ReadInt32();
                                reader.ReadBoolean();
                            }

                            continue;
                        }

                        var locations = new List<SpawnLocation>(locCount);

                        for (int i = 0; i < locCount; i++)
                        {
                            int  x       = reader.ReadInt32();
                            int  y       = reader.ReadInt32();
                            int  z       = reader.ReadInt32();
                            bool isWater = reader.ReadBoolean();

                            locations.Add(new SpawnLocation(x, y, z, isWater));
                        }

                        _AllLocations[map] = locations;
                    }
                }

                return _AllLocations.Count > 0;
            }
            catch (Exception ex)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"SPAWN CACHE-[Load failed: {ex.Message}] - rebuilding");

                _AllLocations.Clear();

                return false;
            }
        }
    }
}
