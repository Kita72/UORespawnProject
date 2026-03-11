using System;
using System.IO;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Custom.UORespawnServer.Entities;

namespace Server.Custom.UORespawnServer.Services
{
    internal class StatsService
    {
        private readonly List<StatEntity> SpawnStats;

        public StatsService()
        {
            SpawnStats = new List<StatEntity>();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"STATS-[Created]");
        }

        internal void AddStats(PlayerMobile pm, Mobile spawn)
        {
            if (SpawnStats == null) return;

            if (SpawnStats.Count > UOR_Settings.MAX_STAT_SIZE)
            {
                SpawnStats.RemoveAt(0);
            }

            StatEntity stat = new StatEntity
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

            SpawnStats.Add(stat);
        }

        internal void Save()
        {
            if (SpawnStats == null) return;

            if (SpawnStats.Count > 0)
            {
                int count = SpawnStats.Count;

                var lines = new System.Text.StringBuilder();

                foreach (var spawn in SpawnStats)
                    lines.Append(spawn.ToString());

                File.AppendAllText(GetFileName(), lines.ToString());

                SpawnStats.Clear();

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"STATS-[{count} Created]");
            }

            UOR_Utility.CleanUpOldFiles(UOR_DIR.STAT_DIR);
        }

        private string GetFileName()
        {
            var time = DateTime.Now;

            return Path.Combine(UOR_DIR.STAT_DIR, $"{time.Year}_{time.DayOfYear}.txt");
        }
    }
}
