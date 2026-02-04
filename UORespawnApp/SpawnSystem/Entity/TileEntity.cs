namespace Server.Custom.SpawnSystem
{
    public class TileEntity
    {
        public Frequency Freq { get; private set; } = Frequency.Common;

        public string Name { get; private set; } = string.Empty;

        public bool IsMob { get; private set; } = false;

        public TileEntity(Frequency freq, string name, bool isMob)
        {
            Freq = freq;

            Name = name;

            IsMob = isMob;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
