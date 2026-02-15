using UORespawnApp.Scripts.DTO.Enums;
using UORespawnApp.Scripts.DTO.Models;

namespace UORespawnApp.Scripts.Entities
{
    /// <summary>
    /// Box-based spawn entity for UORespawn v2.0
    /// Supports 6 spawn list categories with Weather and Timed triggers
    /// </summary>
    public class BoxSpawnEntity
    {
        /// <summary>
        /// Unique position/ID for this spawn box
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Priority for spawn processing (lower = higher priority)
        /// Server processes boxes by priority order
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Map ID this spawn box belongs to (0=Felucca, 1=Trammel, etc.)
        /// Supports custom maps with IDs > 6
        /// </summary>
        public int MapId { get; set; }

        /// <summary>
        /// Rectangular spawn area on the map
        /// </summary>
        public Rect SpawnBox { get; set; } = new();

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
        /// Water-based spawns (lakes, oceans, rivers)
        /// Checked when spawn location is water tile
        /// </summary>
        public List<string> WaterSpawns { get; set; } = [];

        /// <summary>
        /// Weather-triggered spawns (rain, snow, storm, blizzard)
        /// Only spawns when WeatherSpawn condition matches current weather
        /// </summary>
        public List<string> WeatherSpawns { get; set; } = [];

        /// <summary>
        /// Time-triggered spawns (witching hour, noon, etc.)
        /// Only spawns when TimedSpawn condition matches current time
        /// </summary>
        public List<string> TimedSpawns { get; set; } = [];

        /// <summary>
        /// Common spawns (100% chance by default)
        /// Primary spawn list for general mobs
        /// </summary>
        public List<string> CommonSpawns { get; set; } = [];

        /// <summary>
        /// Uncommon spawns (50% chance by default)
        /// Secondary spawn list for less frequent mobs
        /// </summary>
        public List<string> UncommonSpawns { get; set; } = [];

        /// <summary>
        /// Rare spawns (10% chance by default)
        /// Tertiary spawn list for rare/special mobs
        /// </summary>
        public List<string> RareSpawns { get; set; } = [];

        /// <summary>
        /// Update priority based on position in entity list
        /// Called when boxes are reordered
        /// </summary>
        public void UpdatePriority(List<BoxSpawnEntity> entities)
        {
            Priority = entities.Count;
        }

        /// <summary>
        /// Check if this spawn box has any spawn data configured
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
        /// Convert to BoxModel DTO for binary serialization
        /// </summary>
        public BoxModel ToBoxModel()
        {
            return new BoxModel
            {
                Id = Position,
                SpawnPriority = Priority,
                MapId = MapId,
                X = (int)SpawnBox.X,
                Y = (int)SpawnBox.Y,
                Width = (int)SpawnBox.Width,
                Height = (int)SpawnBox.Height,
                WeatherSpawn = WeatherSpawn,
                TimedSpawn = TimedSpawn,
                WaterSpawns = [.. WaterSpawns],
                WeatherSpawns = [.. WeatherSpawns],
                TimedSpawns = [.. TimedSpawns],
                CommonSpawns = [.. CommonSpawns],
                UncommonSpawns = [.. UncommonSpawns],
                RareSpawns = [.. RareSpawns]
            };
        }

        /// <summary>
        /// Create BoxSpawnEntity from BoxModel DTO
        /// </summary>
        public static BoxSpawnEntity FromBoxModel(BoxModel model, int mapId)
        {
            return new BoxSpawnEntity
            {
                Position = model.Id,
                Priority = model.SpawnPriority,
                MapId = mapId,
                SpawnBox = new Rect(model.X, model.Y, model.Width, model.Height),
                WeatherSpawn = model.WeatherSpawn,
                TimedSpawn = model.TimedSpawn,
                WaterSpawns = model.WaterSpawns != null ? [.. model.WaterSpawns] : new(),
                WeatherSpawns = model.WeatherSpawns != null ? [.. model.WeatherSpawns] : new(),
                TimedSpawns = model.TimedSpawns != null ? [.. model.TimedSpawns] : new(),
                CommonSpawns = model.CommonSpawns != null ? [.. model.CommonSpawns] : new(),
                UncommonSpawns = model.UncommonSpawns != null ? [.. model.UncommonSpawns] : new(),
                RareSpawns = model.RareSpawns != null ? [.. model.RareSpawns] : new()
            };
        }
    }
}
