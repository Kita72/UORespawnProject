namespace UORespawnApp
{
    internal class XMLSpawnPoint
    {
        public int Map { get; set; }
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
