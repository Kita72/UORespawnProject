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

    /// <summary>
    /// Box spawn data indexed by MapId.
    /// Each map contains a list of BoxSpawnEntity objects.
    /// </summary>
    internal Dictionary<int, List<BoxSpawnEntity>> BoxSpawns { get; private set; } = [];

    /// <summary>
    /// Tile spawn data indexed by MapId.
    /// Each map contains a list of TileSpawnEntity objects.
    /// </summary>
    internal Dictionary<int, List<TileSpawnEntity>> TileSpawns { get; private set; } = [];

    /// <summary>
    /// Region spawn data indexed by MapId.
    /// Each map contains a list of RegionSpawnEntity objects.
    /// </summary>
    internal Dictionary<int, List<RegionSpawnEntity>> RegionSpawns { get; private set; } = [];

    /// <summary>
    /// Vendor spawn data indexed by MapId.
    /// Each map contains a list of VendorEntity objects.
    /// </summary>
    internal Dictionary<int, List<VendorEntity>> VendorSpawns { get; private set; } = [];

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
                BoxSpawns[i] = [];
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
            if (!BoxSpawns.TryGetValue(mapId, out var list))
            {
                list = [];
                BoxSpawns[mapId] = list;
            }

            if (!list.Contains(entity))
            {
                list.Add(entity);
            }
        }
    }

    /// <summary>
    /// Clears all box spawn data.
    /// </summary>
    public void ClearBoxSpawns()
    {
        lock (_lock)
        {
            BoxSpawns.Clear();
        }
    }

    /// <summary>
    /// Gets the total count of box spawns across all maps.
    /// </summary>
    public int GetTotalBoxSpawnCount()
    {
        lock (_lock)
        {
            return BoxSpawns.Values.Sum(list => list.Count);
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
                TileSpawns[i] = [];
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
            TileSpawns.Clear();
        }
    }

    /// <summary>
    /// Gets the total count of tile spawns across all maps.
    /// </summary>
    public int GetTotalTileSpawnCount()
    {
        lock (_lock)
        {
            return TileSpawns.Values.Sum(list => list.Count);
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
                RegionSpawns[i] = [];
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
            RegionSpawns.Clear();
        }
    }

    /// <summary>
    /// Gets the total count of region spawns across all maps.
    /// </summary>
    public int GetTotalRegionSpawnCount()
    {
        lock (_lock)
        {
            return RegionSpawns.Values.Sum(list => list.Count);
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
                VendorSpawns[i] = [];
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
            VendorSpawns.Clear();
        }
    }

    /// <summary>
    /// Gets the total count of vendor spawns across all maps.
    /// </summary>
    public int GetTotalVendorSpawnCount()
    {
        lock (_lock)
        {
            return VendorSpawns.Values.Sum(list => list.Count);
        }
    }

    #endregion
}

