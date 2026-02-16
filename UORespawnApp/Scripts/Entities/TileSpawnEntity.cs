namespace UORespawnApp.Scripts.Entities
{
    /// <summary>
    /// Tile-based spawn entity for UORespawn v2.0
    /// Spawns mobs based on ground tile type (grass, snow, swamp, etc.)
    /// </summary>
    public class TileSpawnEntity
    {
        /// <summary>
        /// Unique identifier for tile spawn entry
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Tile name (e.g., "Grass", "Snow", "Swamp", "Cave Floor")
        /// Must match TileNames enum on server side
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Map ID this tile spawn applies to
        /// Supports custom maps with IDs > 6
        /// </summary>
        public int MapId { get; set; }

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
        /// Water-based spawns (for water tiles)
        /// </summary>
        public List<string> WaterSpawns { get; set; } = [];

        /// <summary>
        /// Weather-triggered spawns
        /// </summary>
        public List<string> WeatherSpawns { get; set; } = [];

        /// <summary>
        /// Time-triggered spawns
        /// </summary>
        public List<string> TimedSpawns { get; set; } = [];

        /// <summary>
        /// Common spawns (primary list)
        /// </summary>
        public List<string> CommonSpawns { get; set; } = [];

        /// <summary>
        /// Uncommon spawns (secondary list)
        /// </summary>
        public List<string> UncommonSpawns { get; set; } = [];

        /// <summary>
        /// Rare spawns (tertiary list)
        /// </summary>
        public List<string> RareSpawns { get; set; } = [];

        /// <summary>
        /// Check if this tile spawn has any data configured
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
    }
}
