namespace UORespawnApp.Scripts.Entities
{
    /// <summary>
    /// Metadata describing a spawn pack.
    /// </summary>
    public class SpawnPackMetadata
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageFileName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime? PublishedOn { get; set; }
        public bool IsApproved { get; set; }
    }

    /// <summary>
    /// Computed statistics for a spawn pack.
    /// </summary>
    public class SpawnPackStats
    {
        /// <summary>Total spawn entries (creatures) across all box spawns.</summary>
        public int BoxSpawnCount { get; set; }
        /// <summary>Total spawn entries (creatures) across all tile spawns.</summary>
        public int TileSpawnCount { get; set; }
        /// <summary>Total spawn entries (creatures) across all region spawns.</summary>
        public int RegionSpawnCount { get; set; }
        /// <summary>Total vendor entries across all vendor spawns.</summary>
        public int VendorSpawnCount { get; set; }

        /// <summary>Number of box spawn locations.</summary>
        public int BoxLocationCount { get; set; }
        /// <summary>Number of tile spawn locations.</summary>
        public int TileLocationCount { get; set; }
        /// <summary>Number of region spawn locations.</summary>
        public int RegionLocationCount { get; set; }
        /// <summary>Number of vendor spawn locations (signs/hives).</summary>
        public int VendorLocationCount { get; set; }

        public int TotalSpawnEntries { get; set; }
        public int UniqueCreatureCount { get; set; }
        public int MapCount { get; set; }
        public Dictionary<string, int> SpawnTypeCounts { get; set; } = [];
    }

    /// <summary>
    /// Spawn pack record used by the editor UI.
    /// </summary>
    public class SpawnPackInfo
    {
        /// <summary>
        /// Runtime GUID for in-app tracking. Not persisted - assigned fresh on each load.
        /// Used to uniquely identify packs within the current app session.
        /// </summary>
        public Guid RuntimeId { get; set; } = Guid.NewGuid();

        public SpawnPackMetadata Metadata { get; set; } = new();
        public SpawnPackStats Stats { get; set; } = new();
        public string PackFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user has favorited this pack. Hydrated from Settings.FavoritePackIds on load.
        /// Not stored in pack metadata — persisted in app Preferences keyed by Metadata.Id.
        /// </summary>
        public bool IsFavorite { get; set; }
    }
}
