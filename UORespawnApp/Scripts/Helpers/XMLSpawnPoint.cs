namespace UORespawnApp
{
    internal class XMLSpawnPoint
    {
        public GameMap Map { get; set; } = GameMap.Map0;
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
