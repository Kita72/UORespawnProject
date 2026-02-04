using System.Collections.Generic;
using System.Linq;

namespace Server.Custom.SpawnSystem
{
    public class StaticEntity
    {
        public string Name { get; private set; }

        public List<(Frequency freq, string name)> Spawn { get; private set; }

        public StaticEntity(string name, List<(Frequency freq, string name)> mobs)
        {
            Name = name;

            Spawn = mobs;
        }

        public string GetRandomSpawn(Frequency freq)
        {
            var freqList = Spawn.Where(e => e.freq == freq).ToList();

            if (freqList.Count > 0)
            {
                return freqList[Utility.Random(freqList.Count - 1)].name;
            }

            return string.Empty;
        }
    }
}
