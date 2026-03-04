namespace UORespawnApp
{
    /// <summary>
    /// Defines the type of XML spawner for color-coding and functionality control.
    /// </summary>
    internal enum SpawnerType
    {
        /// <summary>Standard creature spawner (green) - can be deleted/added</summary>
        Regular,
        /// <summary>Spawner with no creatures defined (red) - can be deleted</summary>
        Empty,
        /// <summary>Treasure chest spawner (purple) - read-only, cannot modify</summary>
        Treasure,
        /// <summary>Quest NPC spawner (blue) - read-only, cannot modify</summary>
        Quest
    }

    internal class XMLSpawnPoint
    {
        /// <summary>
        /// Serial number of the spawner item (for identification in commands)
        /// </summary>
        public string Serial { get; set; } = string.Empty;

        public int Map { get; set; }

        // Center coordinates (original spawner location)
        public int CenterX { get; set; }
        public int CenterY { get; set; }

        // HomeRange as radius for circle visualization
        public int Radius { get; set; }

        // MaxCount from spawner
        public int MaxCount { get; set; }

        // Legacy properties for backward compatibility
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // List of creature/spawn names from the spawner (cleaned of RND patterns)
        public List<string> SpawnNames { get; set; }

        /// <summary>
        /// The type of spawner for color-coding and functionality.
        /// </summary>
        public SpawnerType Type { get; set; } = SpawnerType.Regular;

        /// <summary>
        /// Whether this spawner can be modified (deleted/added).
        /// Only Regular and Empty spawners can be modified.
        /// </summary>
        public bool CanModify => Type == SpawnerType.Regular || Type == SpawnerType.Empty;

        public XMLSpawnPoint()
        {
            SpawnNames = [];
        }
    }
}
