namespace UORespawnApp
{
    internal class XMLSpawnPoint
    {
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

        // List of creature/spawn names from the spawner
        public List<string> SpawnNames { get; set; } 

        public XMLSpawnPoint()
        {
            SpawnNames = [];
        }
    }
}
