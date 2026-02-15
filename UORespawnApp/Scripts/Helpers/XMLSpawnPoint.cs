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

        // Legacy properties for backward compatibility
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public List<string> ObjectNames { get; set; } 

        public XMLSpawnPoint()
        {
            ObjectNames = [];
        }
    }
}
