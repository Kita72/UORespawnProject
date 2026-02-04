namespace UORespawnApp
{
    public class StaticEntity(string name, List<(Frequency freq, string name)> spawn)
    {
        public string Name { get; private set; } = name;

        public List<(Frequency freq, string name)> Spawn { get; private set; } = spawn;

        internal void AddSpawn((Frequency freq, string name) mob)
        {
            bool exists = false;

            foreach (var spawn in Spawn)
            {
                if (spawn.freq == mob.freq)
                {
                    if (spawn.name == mob.name)
                    {
                        exists = true;
                    }
                }
            }

            if (!exists)
            {
                Spawn?.Add(mob);
            }
        }

        internal void RemoveSpawn((Frequency freq, string name) mob)
        {
            if (Spawn != null)
            {
                var freqList = Spawn.FindAll(m => m.freq == mob.freq);

                var existingMob = freqList.FindAll(m => m.name == mob.name);

                if (existingMob.Count > 0)
                {
                    Spawn.Remove(mob);
                }
            }
        }
    }
}
