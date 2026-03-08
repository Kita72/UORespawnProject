using System;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Managers;

namespace Server.Custom.UORespawnServer;
internal static class TileSpawner
{
    internal static string TryTileSpawn(SpawnEntity spawn)
    {
        string tileName = spawn.TileName;

        if (string.IsNullOrEmpty(tileName))
            return string.Empty;

        if (!SpawnManager.TileLookup.TryGetValue(spawn.Facet, out var tileDict))
        {
            return string.Empty;
        }

        try
        {
            if (tileDict.TryGetValue(tileName, out TileEntity entity))
            {
                string spawnName = UOR_Utility.GetSpawnName(entity, spawn);

                return spawnName;
            }

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"TILE SPAWN-[Doesn't Exist: {tileName}]");
        }
        catch (Exception ex)
        {
            UOR_Utility.SendMsg(ConsoleColor.DarkRed, $"Factory => Tile Error: {ex.Message}");
        }

        return string.Empty;
    }
}
