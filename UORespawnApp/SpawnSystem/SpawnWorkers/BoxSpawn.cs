using System.Collections.Generic;
using System.Linq;

using Server.Items;

using static Server.Custom.SpawnSystem.SpawnSysSettings;

namespace Server.Custom.SpawnSystem
{
    internal static class BoxSpawn
    {
        internal static string TryBoxSpawn(Map map, Point3D location)
        {
            try
            {
                List<SpawnEntity> spawnList = new List<SpawnEntity>();

                if (SpawnSysDataBase.Spawns.Count > 0)
                {
                    spawnList.AddRange(SpawnSysDataBase.Spawns[map]);
                }

                if (spawnList.Count > 0)
                {
                    var allLocs = spawnList.Where(se => se.SpawnBox.Contains(location.X, location.Y)).ToList();

                    if (allLocs != null && allLocs.Count > 0)
                    {
                        SpawnEntity prioritySpawn = null;

                        foreach (var loc in allLocs)
                        {
                            if (prioritySpawn == null)
                            {
                                prioritySpawn = loc;
                            }
                            else if (loc.Position > prioritySpawn.Position)
                            {
                                prioritySpawn = loc;
                            }
                        }

                        if (prioritySpawn != null)
                        {
                            var freq = GetFreq(Utility.RandomDouble());

                            if (prioritySpawn.TimedSpawn != "None")
                            {
                                Clock.GetTime(map, location.X, location.Y, out int hour, out int _);

                                if (!SpawnSysUtility.IsSpawnTime(prioritySpawn.TimedSpawn, hour))
                                {
                                    return string.Empty;
                                }
                            }

                            List<string> entityList = new List<string>();

                            switch (freq)
                            {
                                case Frequency.Common: entityList.AddRange(prioritySpawn.CommonSpawnList); break;
                                case Frequency.UnCommon: entityList.AddRange(prioritySpawn.UnCommonSpawnList); break;
                                case Frequency.Rare: entityList.AddRange(prioritySpawn.RareSpawnList); break;
                            }

                            if (entityList.Count > 0)
                            {
                                return entityList[Utility.Random(entityList.Count)];
                            }
                        }
                    }
                }
            }
            catch
            {
                SpawnSysUtility.SendConsoleMsg(System.ConsoleColor.DarkRed, "Factory => Box Error!");
            }

            return string.Empty;
        }
    }
}
