using UORespawnApp.Scripts.DTO.Enums;
using UORespawnApp.Scripts.DTO.Models;

namespace UORespawnApp.Scripts.Entities
{
    /// <summary>
    /// Region-based spawn entity for UORespawn v2.0
    /// Spawns mobs within named server regions (e.g., "Britain", "Dungeon Despise")
    /// </summary>
    public class RegionSpawnEntity
    {
        /// <summary>
        /// Unique identifier for region spawn entry
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Region name (must match server-side Region.Name exactly)
        /// Case-insensitive matching on server
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Map ID this region spawn applies to
        /// Supports custom maps with IDs > 6
        /// </summary>
        public int MapId { get; set; }

        /// <summary>
        /// Region bounds for editor visualization (all rectangles that make up this region)
        /// Loaded from UOR_RegionList.txt for display on map
        /// </summary>
        public List<Rect> RegionBounds { get; set; } = new();

        /// <summary>
        /// Weather trigger type for WeatherSpawns list
        /// </summary>
        public WeatherTypes WeatherSpawn { get; set; } = WeatherTypes.None;

        /// <summary>
        /// Time-of-day trigger type for TimedSpawns list
        /// </summary>
        public TimeNames TimedSpawn { get; set; } = TimeNames.None;

        // 6 Spawn Lists (v2.0 structure)

        /// <summary>
        /// Water-based spawns (for water tiles in region)
        /// </summary>
        public List<string> WaterSpawns { get; set; } = new();

        /// <summary>
        /// Weather-triggered spawns
        /// </summary>
        public List<string> WeatherSpawns { get; set; } = new();

        /// <summary>
        /// Time-triggered spawns
        /// </summary>
        public List<string> TimedSpawns { get; set; } = new();

        /// <summary>
        /// Common spawns (primary list)
        /// </summary>
        public List<string> CommonSpawns { get; set; } = new();

        /// <summary>
        /// Uncommon spawns (secondary list)
        /// </summary>
        public List<string> UncommonSpawns { get; set; } = new();

        /// <summary>
        /// Rare spawns (tertiary list)
        /// </summary>
        public List<string> RareSpawns { get; set; } = new();

        /// <summary>
        /// Check if this region spawn has any data configured
        /// </summary>
        public bool HasSpawns()
        {
            return WaterSpawns.Count > 0 ||
                   WeatherSpawns.Count > 0 ||
                   TimedSpawns.Count > 0 ||
                   CommonSpawns.Count > 0 ||
                   UncommonSpawns.Count > 0 ||
                   RareSpawns.Count > 0;
        }

        /// <summary>
        /// Get total spawn count across all lists
        /// </summary>
        public int GetTotalSpawnCount()
        {
            return WaterSpawns.Count +
                   WeatherSpawns.Count +
                   TimedSpawns.Count +
                   CommonSpawns.Count +
                   UncommonSpawns.Count +
                   RareSpawns.Count;
        }

        /// <summary>
        /// Convert to RegionModel DTO for binary serialization
        /// </summary>
        public RegionModel ToRegionModel()
        {
            return new RegionModel
            {
                Name = Name,
                MapId = MapId,
                WeatherSpawn = WeatherSpawn,
                TimedSpawn = TimedSpawn,
                WaterSpawns = new List<string>(WaterSpawns),
                WeatherSpawns = new List<string>(WeatherSpawns),
                TimedSpawns = new List<string>(TimedSpawns),
                CommonSpawns = new List<string>(CommonSpawns),
                UncommonSpawns = new List<string>(UncommonSpawns),
                RareSpawns = new List<string>(RareSpawns)
            };
        }

        /// <summary>
        /// Create RegionSpawnEntity from RegionModel DTO
        /// </summary>
        public static RegionSpawnEntity FromRegionModel(RegionModel model, int id)
        {
            return new RegionSpawnEntity
            {
                Id = id,
                Name = model.Name ?? string.Empty,
                MapId = model.MapId,
                RegionBounds = new List<Rect>(), // Loaded separately from RegionDataUtility
                WeatherSpawn = model.WeatherSpawn,
                TimedSpawn = model.TimedSpawn,
                WaterSpawns = model.WaterSpawns != null ? new List<string>(model.WaterSpawns) : new(),
                WeatherSpawns = model.WeatherSpawns != null ? new List<string>(model.WeatherSpawns) : new(),
                TimedSpawns = model.TimedSpawns != null ? new List<string>(model.TimedSpawns) : new(),
                CommonSpawns = model.CommonSpawns != null ? new List<string>(model.CommonSpawns) : new(),
                UncommonSpawns = model.UncommonSpawns != null ? new List<string>(model.UncommonSpawns) : new(),
                RareSpawns = model.RareSpawns != null ? new List<string>(model.RareSpawns) : new()
            };
        }
    }

    /// <summary>
    /// Helper class for region information from UOR_RegionList.txt
    /// Used for editor display and validation
    /// Regions can have multiple rectangles (areas)
    /// </summary>
    public class RegionInfo
    {
        public string Name { get; set; } = string.Empty;
        public int MapId { get; set; }
        public List<Rect> Rectangles { get; set; } = new();

        /// <summary>
        /// Get first/primary bounds (for display)
        /// </summary>
        public Rect PrimaryBounds => Rectangles.FirstOrDefault();

        /// <summary>
        /// Check if a point is within any of this region's rectangles
        /// </summary>
        public bool Contains(int x, int y)
        {
            foreach (var rect in Rectangles)
            {
                if (x >= rect.X && x < (rect.X + rect.Width) &&
                    y >= rect.Y && y < (rect.Y + rect.Height))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get center point of primary bounds
        /// </summary>
        public (int X, int Y) GetCenter()
        {
            var primary = PrimaryBounds;
            return ((int)(primary.X + primary.Width / 2), (int)(primary.Y + primary.Height / 2));
        }

        /// <summary>
        /// Get total area covered by all rectangles
        /// </summary>
        public int GetTotalArea()
        {
            return (int)Rectangles.Sum(r => r.Width * r.Height);
        }
    }
}
