namespace UORespawnApp
{
    public class TileEntity(Frequency freq, string name, bool isMob)
    {
        public Frequency Freq { get; private set; } = freq;

        public string Name { get; private set; } = name;

        public bool IsMob { get; private set; } = isMob;

        public override string ToString()
        {
            return Name;
        }
    }
}
