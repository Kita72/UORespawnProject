using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.UORespawnSystem.SpawnHelpers
{
    internal static class SpawnWaterInfo
    {

        public static string TryGetWetName(Map map, Point3D location)
        {
            string tile = new LandTarget(location, map).Name;

            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(location.X, location.Y, false);

            for (int i = 0; i < staticTiles.Length; ++i)
            {
                var sT = new StaticTarget(location, staticTiles[i].ID);

                if (sT.Name == "water" || sT.Name == "blood")
                {
                    tile = sT.Name;
                }
            }

            return tile;
        }

        internal static bool CanSpawnWater(Map map, Point3D location)
        {
            bool isValid = Spawner.IsValidWater(map, location.X, location.Y, location.Z);

            if (!isValid)
            {
                isValid = Spawner.IsValidWater(map, location.X, location.Y, location.Z - 5);
            }

            return isValid;
        }
    }
}
