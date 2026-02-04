using System;
using System.Collections.Generic;
using System.Drawing;

namespace Server.Custom.SpawnSystem
{
    public enum Frequency
    {
        Common,
        UnCommon,
        Rare
    }

    public class SpawnEntity
    {
        public int SpawnPriority { get; private set; } = 0;

        public Rectangle SpawnBox { get; set; } = new Rectangle();

        public string TimedSpawn { get; set; } = "None";

        public List<string> CommonSpawnList { get; set; } = new List<string>();

        public List<string> UnCommonSpawnList { get; set; } = new List<string>();

        public List<string> RareSpawnList { get; set; } = new List<string>();

        public int Position { get; set; }

        public SpawnEntity()
        {
        }

        public string GetRandomSpawn(Frequency freq)
        {
            switch (freq)
            {
                case Frequency.Common:
                    {
                        if (CommonSpawnList.Count > 0)
                        {
                            return CommonSpawnList[Utility.Random(CommonSpawnList.Count)];
                        }

                        return string.Empty;
                    }
                case Frequency.UnCommon:
                    {
                        if (UnCommonSpawnList.Count > 0)
                        {
                            return UnCommonSpawnList[Utility.Random(UnCommonSpawnList.Count)];
                        }

                        return string.Empty;
                    }
                case Frequency.Rare:
                    {
                        if (RareSpawnList.Count > 0)
                        {
                            return RareSpawnList[Utility.Random(RareSpawnList.Count)];
                        }

                        return string.Empty;
                    }
            }

            return string.Empty;
        }

        public void AddSpawn(string name, Frequency freq)
        {
            switch (freq)
            {
                case Frequency.Common:
                    {
                        if (!CommonSpawnList.Contains(name))
                        {
                            CommonSpawnList.Add(name);
                        }

                        break;
                    }

                case Frequency.UnCommon:
                    {
                        if (!UnCommonSpawnList.Contains(name))
                        {
                            UnCommonSpawnList.Add(name);
                        }

                        break;
                    }

                case Frequency.Rare:
                    {
                        if (!RareSpawnList.Contains(name))
                        {
                            RareSpawnList.Add(name);
                        }

                        break;
                    }
            }
        }

        public void RemoveSpawn(string name, Frequency freq)
        {
            switch (freq)
            {
                case Frequency.Common:
                    CommonSpawnList.Remove(name);
                    break;

                case Frequency.UnCommon:
                    UnCommonSpawnList.Remove(name);
                    break;

                case Frequency.Rare:
                    RareSpawnList.Remove(name);
                    break;
            }
        }

        public void UpdatePriority(List<SpawnEntity> allSpawns)
        {
            SpawnPriority = 0;

            foreach (var entity in allSpawns)
            {
                if (entity != this)
                {
                    if (entity.SpawnBox.Contains(SpawnBox))
                    {
                        SpawnPriority = Math.Max(SpawnPriority, entity.SpawnPriority + 1);
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"Spawn {Position}";
        }
    }
}
