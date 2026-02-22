using System;
using System.Collections.Generic;

namespace UORespawnApp.Scripts.Entities
{
    /// <summary>
    /// Metadata describing a spawn pack.
    /// </summary>
    [Serializable]
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
    [Serializable]
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
        public Dictionary<string, int> SpawnTypeCounts { get; set; } = new();
    }

    /// <summary>
    /// Spawn pack record used by the editor UI.
    /// </summary>
    [Serializable]
    public class SpawnPackInfo
    {
        public SpawnPackMetadata Metadata { get; set; } = new();
        public SpawnPackStats Stats { get; set; } = new();
        public string PackFolderPath { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public bool IsLiked { get; set; }

        /// <summary>
        /// Indicates if an approved pack has been modified from its original backup.
        /// Only applicable for approved packs with a backup zip available.
        /// </summary>
        public bool IsModified { get; set; }
    }
}
