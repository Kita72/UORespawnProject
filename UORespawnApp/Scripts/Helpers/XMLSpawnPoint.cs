namespace UORespawnApp.Scripts.Helpers
{
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

        // List of creature/spawn names from the spawner (cleaned of RND patterns)
        public List<string> SpawnNames { get; set; }

        public XMLSpawnPoint()
        {
            SpawnNames = [];
        }
    }
}
