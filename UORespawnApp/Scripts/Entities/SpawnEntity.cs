namespace UORespawnApp
{
    public class SpawnEntity
    {
        public int Position { get; set; }

        public string TimedSpawn { get; set; } = "None";

        public Rect SpawnBox { get; set; } = new();

        public List<string> CommonSpawnList { get; set; } = [];

        public List<string> UnCommonSpawnList { get; set; } = [];

        public List<string> RareSpawnList { get; set; } = [];

        public int Priority { get; set; }

        public void UpdatePriority(List<SpawnEntity> entities)
        {
            Priority = entities.Count;
        }
    }
}