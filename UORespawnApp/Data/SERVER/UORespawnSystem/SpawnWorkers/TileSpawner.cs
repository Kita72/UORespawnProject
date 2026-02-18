using System.Collections.Generic;

using Server.Custom.UORespawnSystem.Entities;
using Server.Custom.UORespawnSystem.SpawnUtility;
using Server.Custom.UORespawnSystem.SpawnHelpers;
using Server.Targeting;

namespace Server.Custom.UORespawnSystem
{
    internal static class TileSpawner
    {
        internal static string TryTileSpawn(Map map, Point3D location, bool isWater)
        {
            List<TileEntity> spawnList = new List<TileEntity>();

            if (UORespawnDataBase.TileSpawns.Count > 0)
            {
                spawnList.AddRange(UORespawnDataBase.TileSpawns[map]);
            }

            try
            {
                string tileName = string.Empty;

                if (isWater)
                {
                    tileName = SpawnWaterInfo.TryGetWetName(map, location);
                }
                else
                {
                    tileName = new LandTarget(location, map).Name;
                }

                if (string.IsNullOrEmpty(tileName) || tileName == "NoName")
                {
                    tileName = SpawnTileInfo.GetTileName(map.Tiles.GetLandTile(location.X, location.Y).ID);
                }

                if (tileName == "rock")
                {
                    tileName = Utility.RandomList("cave", "cave floor");
                }

                if (spawnList.Count > 0)
                {
                    TileEntity entity = spawnList.Find(t => t.Name == tileName);

                    if (entity != null)
                    {
                        return SpawnFactory.GetSpawnEntity(entity, map, location, isWater);
                    }
                }
            }
            catch
            {
                UORespawnUtility.SendConsoleMsg(System.ConsoleColor.DarkRed, "Factory => Tile Error!");
            }

            return string.Empty;
        }
    }
}
