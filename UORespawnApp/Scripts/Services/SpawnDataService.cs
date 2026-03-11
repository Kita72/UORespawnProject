using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Central service for managing all spawn data.
/// Replaces the static spawn dictionaries in Utility class.
/// Registered as a singleton in DI for shared state across components.
/// </summary>
public class SpawnDataService
{
    private readonly Lock _lock = new();

    private readonly Dictionary<int, List<BoxSpawnEntity>> _boxSpawns = [];
    private readonly Dictionary<int, List<TileSpawnEntity>> _tileSpawns = [];
    private readonly Dictionary<int, List<RegionSpawnEntity>> _regionSpawns = [];
    private readonly Dictionary<int, List<VendorEntity>> _vendorSpawns = [];

    /// <summary>
    /// Box spawn data indexed by MapId. Use service methods to add/clear entries.
    /// </summary>
    internal IReadOnlyDictionary<int, List<BoxSpawnEntity>> BoxSpawns => _boxSpawns;

    /// <summary>
    /// Tile spawn data indexed by MapId. Use service methods to add/clear entries.
    /// </summary>
    internal IReadOnlyDictionary<int, List<TileSpawnEntity>> TileSpawns => _tileSpawns;

    /// <summary>
    /// Region spawn data indexed by MapId. Use service methods to add/clear entries.
    /// </summary>
    internal IReadOnlyDictionary<int, List<RegionSpawnEntity>> RegionSpawns => _regionSpawns;

    /// <summary>
    /// Vendor spawn data indexed by MapId. Use service methods to add/clear entries.
    /// </summary>
    internal IReadOnlyDictionary<int, List<VendorEntity>> VendorSpawns => _vendorSpawns;

    /// <summary>
    /// Initializes all spawn dictionaries with empty lists for standard maps (0-5).
    /// </summary>
    public void InitializeAllSpawns()
    {
        lock (_lock)
        {
            InitializeBoxSpawns();
            InitializeTileSpawns();
            InitializeRegionSpawns();
            InitializeVendorSpawns();
        }
    }

    #region Box Spawns

    /// <summary>
    /// Initialize Box Spawns dictionary with empty lists for each map (0-5).
    /// </summary>
    public void InitializeBoxSpawns()
    {
        lock (_lock)
        {
            for (int i = 0; i <= 5; i++)
            {
                _boxSpawns[i] = [];
            }
        }
    }

    /// <summary>
    /// Adds a box spawn to the specified map.
    /// </summary>
    public void AddBoxSpawn(int mapId, BoxSpawnEntity entity)
    {
        lock (_lock)
        {
            if (!_boxSpawns.TryGetValue(mapId, out var list))
            {
                list = [];
                _boxSpawns[mapId] = list;
            }

            list.Add(entity);
        }
    }

    /// <summary>
    /// Clears all box spawn data.
    /// </summary>
    public void ClearBoxSpawns()
    {
        lock (_lock)
        {
            _boxSpawns.Clear();
        }
    }

    /// <summary>
    /// Gets the total count of box spawns across all maps.
    /// </summary>
    public int GetTotalBoxSpawnCount()
    {
        lock (_lock)
        {
            return _boxSpawns.Values.Sum(list => list.Count);
        }
    }

    /// <summary>
    /// Gets the box spawn list for a map, creating an empty list if none exists.
    /// </summary>
    internal List<BoxSpawnEntity> GetOrCreateBoxList(int mapId)
    {
        lock (_lock)
        {
            if (!_boxSpawns.TryGetValue(mapId, out var list))
            {
                list = [];
                _boxSpawns[mapId] = list;
            }
            return list;
        }
    }

    #endregion

    #region Tile Spawns

    /// <summary>
    /// Initialize Tile Spawns dictionary with empty lists for each map (0-5).
    /// </summary>
    public void InitializeTileSpawns()
    {
        lock (_lock)
        {
            for (int i = 0; i <= 5; i++)
            {
                _tileSpawns[i] = [];
            }
        }
    }

    /// <summary>
    /// Clears all tile spawn data.
    /// </summary>
    public void ClearTileSpawns()
    {
        lock (_lock)
        {
            _tileSpawns.Clear();
        }
    }

    /// <summary>
    /// Gets the total count of tile spawns across all maps.
    /// </summary>
    public int GetTotalTileSpawnCount()
    {
        lock (_lock)
        {
            return _tileSpawns.Values.Sum(list => list.Count);
        }
    }

    /// <summary>
    /// Gets the tile spawn list for a map, creating an empty list if none exists.
    /// </summary>
    internal List<TileSpawnEntity> GetOrCreateTileList(int mapId)
    {
        lock (_lock)
        {
            if (!_tileSpawns.TryGetValue(mapId, out var list))
            {
                list = [];
                _tileSpawns[mapId] = list;
            }
            return list;
        }
    }

    #endregion

    #region Region Spawns

    /// <summary>
    /// Initialize Region Spawns dictionary with empty lists for each map (0-5).
    /// </summary>
    public void InitializeRegionSpawns()
    {
        lock (_lock)
        {
            for (int i = 0; i <= 5; i++)
            {
                _regionSpawns[i] = [];
            }
        }
    }

    /// <summary>
    /// Clears all region spawn data.
    /// </summary>
    public void ClearRegionSpawns()
    {
        lock (_lock)
        {
            _regionSpawns.Clear();
        }
    }

    /// <summary>
    /// Gets the total count of region spawns across all maps.
    /// </summary>
    public int GetTotalRegionSpawnCount()
    {
        lock (_lock)
        {
            return _regionSpawns.Values.Sum(list => list.Count);
        }
    }

    /// <summary>
    /// Gets the region spawn list for a map, creating an empty list if none exists.
    /// </summary>
    internal List<RegionSpawnEntity> GetOrCreateRegionList(int mapId)
    {
        lock (_lock)
        {
            if (!_regionSpawns.TryGetValue(mapId, out var list))
            {
                list = [];
                _regionSpawns[mapId] = list;
            }
            return list;
        }
    }

    #endregion

    #region Vendor Spawns

    /// <summary>
    /// Initialize Vendor Spawns dictionary with empty lists for each map (0-5).
    /// </summary>
    public void InitializeVendorSpawns()
    {
        lock (_lock)
        {
            for (int i = 0; i <= 5; i++)
            {
                _vendorSpawns[i] = [];
            }
        }
    }

    /// <summary>
    /// Clears all vendor spawn data.
    /// </summary>
    public void ClearVendorSpawns()
    {
        lock (_lock)
        {
            _vendorSpawns.Clear();
        }
    }

    /// <summary>
    /// Gets the total count of vendor spawns across all maps.
    /// </summary>
    public int GetTotalVendorSpawnCount()
    {
        lock (_lock)
        {
            return _vendorSpawns.Values.Sum(list => list.Count);
        }
    }

    /// <summary>
    /// Gets the vendor list for a map, creating an empty list if none exists.
    /// Use this to safely get a mutable reference for in-place additions or removals.
    /// </summary>
    internal List<VendorEntity> GetOrCreateVendorList(int mapId)
    {
        lock (_lock)
        {
            if (!_vendorSpawns.TryGetValue(mapId, out var list))
            {
                list = [];
                _vendorSpawns[mapId] = list;
            }
            return list;
        }
    }

    #endregion
}

