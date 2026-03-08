using System;
using System.IO;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Custom.UORespawnServer.Entities;

namespace Server.Custom.UORespawnServer.Services;
internal class StatsService
{
    private readonly List<StatEntity> _spawnStats;

    public StatsService()
    {
        _spawnStats = [];

        UOR_Utility.SendMsg(ConsoleColor.Yellow, $"STATS-[Created]");
    }

    internal void AddStats(PlayerMobile pm, Mobile spawn)
    {
        if (_spawnStats == null){ return; }

        if (_spawnStats.Count > UOR_Settings.MAX_STAT_SIZE)
        {
            _spawnStats.RemoveAt(0);
        }

        StatEntity stat = new()
        {
            Time = DateTime.Now,
            Player = pm.Name,
            MapId = pm.Map.MapID,
            P_LocX = pm.Location.X,
            P_LocY = pm.Location.Y,
            S_LocX = spawn.Location.X,
            S_LocY = spawn.Location.Y,
            Spawn = spawn.GetType().Name
        };

        _spawnStats.Add(stat);
    }

    internal void Save()
    {
        if (_spawnStats == null){ return; }

        if (_spawnStats.Count > 0)
        {
            int count = _spawnStats.Count;

            foreach (var spawn in _spawnStats)
            {
                File.AppendAllText(GetFileName(), spawn.ToString());
            }

            _spawnStats.Clear();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"STATS-[{count} Created]");
        }

        UOR_Utility.CleanUpOldFiles(UOR_DIR.STAT_DIR);
    }

    private static string GetFileName()
    {
        var time = DateTime.Now;

        return Path.Combine(UOR_DIR.STAT_DIR, $"{time.Year}_{time.DayOfYear}.txt");
    }
}
