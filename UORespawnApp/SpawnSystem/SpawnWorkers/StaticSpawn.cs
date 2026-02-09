using System.Linq;

using Server.Targeting;

using static Server.Custom.SpawnSystem.SpawnSysSettings;

namespace Server.Custom.SpawnSystem
{
    internal static class StaticSpawn
    {
        internal static string TryStaticSpawn(Map map, Point3D location)
        {
            var objectPool = map.GetObjectsInRange(location, MIN_RANGE);

            try
            {
                var objects = objectPool.ToList();

                if (objects.Count > 0 && Utility.RandomDouble() < (CHANCE_STATIC * SpawnSysFactory.NightMod))
                {
                    foreach (var obj in objects)
                    {
                        if (obj is StaticTarget st && SpawnSysDataBase.StaticSpawns.Count > 0)
                        {
                            foreach (var entity in SpawnSysDataBase.StaticSpawns)
                            {
                                if (entity.Name == st.Name)
                                {
                                    objectPool.Free();

                                    return entity.GetRandomSpawn(GetFreq(Utility.RandomDouble()));
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                SpawnSysUtility.SendConsoleMsg(System.ConsoleColor.DarkRed, "Factory => Statics Error!");
            }

            objectPool.Free();

            return string.Empty;
        }
    }
}
