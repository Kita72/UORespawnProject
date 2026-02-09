using System.Collections.Generic;
using System.Linq;

using static Server.Custom.SpawnSystem.SpawnSysSettings;

namespace Server.Custom.SpawnSystem
{
    internal static class WaterSpawn
    {
        internal static string TryWaterSpawn(Map map, Region region, Point3D location, bool isWater)
        {
            if (isWater)
            {
                string spawn = BoxSpawn.TryBoxSpawn(map, location);

                if (!string.IsNullOrEmpty(spawn))
                {
                    return spawn;
                }

                string tileName = SpawnSysUtility.TryGetWetName(map, location);

                try
                {
                    if (string.IsNullOrEmpty(spawn))
                    {
                        if (SpawnSysDataBase.WorldSpawns.Count > 0)
                        {
                            var entity = SpawnSysDataBase.WorldSpawns.Find(e => e.MapHandle == map);

                            if (entity == null)
                            {
                                return string.Empty;
                            }

                            var spawnList = entity.GetSpawnList(tileName, GetFreq(Utility.RandomDouble()));

                            if (spawnList.Count > 0)
                            {
                                List<TileEntity> refinedList = new List<TileEntity>();

                                if (Utility.RandomDouble() < (CHANCE_CREATURE * SpawnSysFactory.NightMod))
                                {
                                    refinedList.AddRange(spawnList.Where(s => s.IsMob).ToList());
                                }
                                else
                                {
                                    refinedList.AddRange(spawnList.Where(s => !s.IsMob).ToList());
                                }

                                if (refinedList.Count > 0)
                                {
                                    spawn = refinedList[Utility.Random(refinedList.Count - 1)].Name;
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(spawn))
                    {
                        return spawn;
                    }
                }
                catch
                {
                    SpawnSysUtility.SendConsoleMsg(System.ConsoleColor.DarkRed, "Factory => Water Error!");
                }
            }

            return string.Empty;
        }
    }
}
