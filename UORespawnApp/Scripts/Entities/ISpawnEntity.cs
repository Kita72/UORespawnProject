namespace UORespawnApp.Scripts.Entities
{
    /// <summary>
    /// Interface for spawn entities that share the common 6-list spawn structure.
    /// Implemented by BoxSpawnEntity, TileSpawnEntity, and RegionSpawnEntity.
    /// 
    /// All spawn types use:
    /// - 6 spawn lists: Water, Weather, Timed, Common, Uncommon, Rare
    /// - 2 trigger enums: WeatherSpawn (weather conditions), TimedSpawn (time-of-day)
    /// 
    /// This interface enables:
    /// - Consistent spawn list handling across all spawn types
    /// - Shared UI components for spawn list editing
    /// - Generic spawn validation and synchronization
    /// </summary>
    public interface ISpawnEntity
    {
        /// <summary>
        /// Map ID this spawn applies to (0=Felucca, 1=Trammel, etc.)
        /// Supports custom maps with IDs > 5
        /// </summary>
        int MapId { get; set; }

        /// <summary>
        /// Weather trigger type for WeatherSpawns list.
        /// Only spawns when this condition matches current weather.
        /// </summary>
        WeatherTypes WeatherSpawn { get; set; }

        /// <summary>
        /// Time-of-day trigger type for TimedSpawns list.
        /// Only spawns when this condition matches current game time.
        /// </summary>
        TimeNames TimedSpawn { get; set; }

        // ==================== 6 SPAWN LISTS ====================

        /// <summary>
        /// Water-based spawns (lakes, oceans, rivers).
        /// Checked when spawn location is a water tile.
        /// </summary>
        List<string> WaterSpawns { get; set; }

        /// <summary>
        /// Weather-triggered spawns (rain, snow, storm, blizzard).
        /// Only spawns when WeatherSpawn condition matches current weather.
        /// </summary>
        List<string> WeatherSpawns { get; set; }

        /// <summary>
        /// Time-triggered spawns (witching hour, noon, etc.).
        /// Only spawns when TimedSpawn condition matches current time.
        /// </summary>
        List<string> TimedSpawns { get; set; }

        /// <summary>
        /// Common spawns (100% chance by default).
        /// Primary spawn list for general mobs.
        /// </summary>
        List<string> CommonSpawns { get; set; }

        /// <summary>
        /// Uncommon spawns (50% chance by default).
        /// Secondary spawn list for less frequent mobs.
        /// </summary>
        List<string> UncommonSpawns { get; set; }

        /// <summary>
        /// Rare spawns (10% chance by default).
        /// Tertiary spawn list for rare/special mobs.
        /// </summary>
        List<string> RareSpawns { get; set; }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Check if this spawn entity has any spawn data configured.
        /// Returns true if any of the 6 spawn lists contains entries.
        /// </summary>
        bool HasSpawns();

        /// <summary>
        /// Get total spawn count across all 6 lists.
        /// Useful for UI display and validation.
        /// </summary>
        int GetTotalSpawnCount();
    }
}
