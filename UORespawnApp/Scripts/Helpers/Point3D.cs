namespace UORespawnApp.Scripts.Helpers
{
    internal struct Point3D(int x, int y, int z)
    {
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
        public int Z { get; set; } = z;
    }
}
