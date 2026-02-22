using System;
using System.Linq;

using Server.Commands;
using Server.Custom.UORespawnSystem.Gumps;
using Server.Custom.UORespawnSystem.Services;
using Server.Custom.UORespawnSystem.SpawnUtility;

namespace Server.Custom.UORespawnSystem.Commands
{
    internal static class UORespawnCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("SpawnAdmin", AccessLevel.GameMaster, new CommandEventHandler(SpawnAdmin_OnCommand));
            CommandSystem.Register("UpdateRespawn", AccessLevel.Administrator, new CommandEventHandler(UpdateSpawn_OnCommand));
            CommandSystem.Register("DebugRespawn", AccessLevel.Administrator, new CommandEventHandler(ToggleDebug_OnCommand));
            CommandSystem.Register("TrackRespawn", AccessLevel.Administrator, new CommandEventHandler(TrackRespawn_OnCommand));
            CommandSystem.Register("ClearRespawn", AccessLevel.Administrator, new CommandEventHandler(ClearRespawn_OnCommand));
            CommandSystem.Register("ReloadRespawn", AccessLevel.Administrator, new CommandEventHandler(ReloadSpawn_OnCommand));
            CommandSystem.Register("PushRespawnStats", AccessLevel.Administrator, new CommandEventHandler(PushRespawnStats_OnCommand));
            CommandSystem.Register("SpawnMetrics", AccessLevel.Administrator, new CommandEventHandler(SpawnMetrics_OnCommand));
            CommandSystem.Register("SpawnMetricsReset", AccessLevel.Administrator, new CommandEventHandler(SpawnMetricsReset_OnCommand));
            CommandSystem.Register("SpawnStatus", AccessLevel.GameMaster, new CommandEventHandler(SpawnStatus_OnCommand));
            CommandSystem.Register("SpawnReload", AccessLevel.Administrator, new CommandEventHandler(SpawnReload_OnCommand));
            CommandSystem.Register("SpawnPause", AccessLevel.Administrator, new CommandEventHandler(SpawnPause_OnCommand));
            CommandSystem.Register("SpawnResume", AccessLevel.Administrator, new CommandEventHandler(SpawnResume_OnCommand));

            // NEW: Recycle system commands
            CommandSystem.Register("SpawnRecycleStats", AccessLevel.GameMaster, new CommandEventHandler(SpawnRecycleStats_OnCommand));
            CommandSystem.Register("ClearRecycle", AccessLevel.Administrator, new CommandEventHandler(ClearRecycle_OnCommand));
        }

        // SpawnAdmin - Opens the admin GUI
        [Usage("SpawnAdmin")]
        [Description("Opens the UORespawn Admin Control Panel GUI.")]
        public static void SpawnAdmin_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                from.SendGump(new SpawnAdminGump());
                from.SendMessage(0x35, "UORespawn Admin Panel opened.");
            }
            else
            {
                from.SendMessage(0x22, "You do not have permission to use this command.");
            }
        }

        // PushRespawn
        [Usage("UpdateRespawn")]
        [Description("UORespawn: Update Spawn")]
        public static void UpdateSpawn_OnCommand(CommandEventArgs e)
        {
            UORespawnCore.UpdateWorldSpawn();
        }

        // DebugRespawn
        [Usage("DebugRespawn")]
        [Description("UORespawn: Turn Debug On/Off")]
        public static void ToggleDebug_OnCommand(CommandEventArgs e)
        {
            UORespawnSettings.ENABLE_DEBUG = !UORespawnSettings.ENABLE_DEBUG;

            string state = UORespawnSettings.ENABLE_DEBUG ? "ON" : "OFF";

            // Flush debug log when turning OFF
            if (!UORespawnSettings.ENABLE_DEBUG)
            {
                SpawnDebugService.FlushToFile("Debug toggled OFF by command");
            }

            e.Mobile.SendMessage($"UORespawn Debug - {state}");
        }

        // TrackRespawn
        [Usage("TrackRespawn")]
        [Description("UORespawn: View Spawn Statistics")]
        public static void TrackRespawn_OnCommand(CommandEventArgs e)
        {
            int activeSpawns = UORespawnCore.GetActiveSpawnCount();
            int recycleCount = UORespawnCore.GetRecyclePoolCount();
            int playerCount = UORespawnCore.GetActivePlayerCount();

            e.Mobile.SendMessage(68, "===== UORespawn Statistics =====");
            e.Mobile.SendMessage(85, $"Active Spawns: {activeSpawns}");
            e.Mobile.SendMessage(85, $"Recycle Pool: {recycleCount}");
            e.Mobile.SendMessage(85, $"Active Players: {playerCount}");
            e.Mobile.SendMessage(68, "================================");

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"{e.Mobile.Name} checked spawn stats - Active: {activeSpawns}, Recycled: {recycleCount}, Players: {playerCount}");
        }

        // ClearRespawn
        [Usage("ClearRespawn")]
        [Description("UORespawn: Clear All Tracked Spawns (Deletes all spawned creatures)")]
        public static void ClearRespawn_OnCommand(CommandEventArgs e)
        {
            int deletedCount = UORespawnCore.ClearAllSpawns("User - Command");

            e.Mobile.SendMessage(38, $"UORespawn: Cleared and deleted {deletedCount} tracked spawns!");

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"{e.Mobile.Name} cleared all spawns - Deleted: {deletedCount} mobs");
        }

        // ReloadRespawn
        [Usage("ReloadRespawn")]
        [Description("UORespawn: Reload Spawn")]
        public static void ReloadSpawn_OnCommand(CommandEventArgs e)
        {
            UORespawnDataBase.ReLoadSpawns();

            e.Mobile.SendMessage($"UORespawn Reloaded!");
        }

        // PushRespawnStats
        [Usage("PushRespawnStats")]
        [Description("UORespawn: Push Spawn Stats")]
        public static void PushRespawnStats_OnCommand(CommandEventArgs e)
        {
            World.Save();

            e.Mobile.SendMessage($"UORespawn: Spawn Stats Pushed!");
        }


        [Usage("SpawnMetrics [players]")]
        [Description("Displays UORespawn performance metrics. Add 'players' to include per-player stats.")]
        private static void SpawnMetrics_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            bool includePlayerMetrics = e.Arguments.Length > 0 && e.Arguments[0].ToLower() == "players";

            try
            {
                string report = SpawnMetricsService.GetReport(includePlayerMetrics);

                // Send to console for readability
                from.SendMessage(0x35, "Metrics report sent to console. Check your server console window.");

                // Write to console
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(report);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                from.SendMessage(0x22, $"Error generating metrics report: {ex.Message}");
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Metrics command failed - {ex.Message}");
            }
        }

        [Usage("SpawnMetricsReset")]
        [Description("Resets all UORespawn performance metrics counters.")]
        private static void SpawnMetricsReset_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            try
            {
                SpawnMetricsService.Reset();

                from.SendMessage(0x35, "UORespawn metrics have been reset.");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[UORespawn]: Metrics reset by {from.Name}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                from.SendMessage(0x22, $"Error resetting metrics: {ex.Message}");
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Metrics reset failed - {ex.Message}");
            }
        }

        [Usage("SpawnStatus")]
        [Description("Displays a quick summary of UORespawn status.")]
        private static void SpawnStatus_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            try
            {
                string summary = SpawnMetricsService.GetSummary();

                from.SendMessage(0x59, "═══════════════════════════════════════════");
                from.SendMessage(0x35, "UORespawn Status:");
                from.SendMessage(0x48, summary);
                from.SendMessage(0x59, "═══════════════════════════════════════════");

                // Additional quick stats
                from.SendMessage(0x35, $"Last cleanup: {SpawnMetricsService.GetLastCleanupTime():F2}ms");
                from.SendMessage(0x35, $"Recycle rate: {SpawnMetricsService.GetRecycleRate():F1}%");
                from.SendMessage(0x35, $"Total cycles: {SpawnMetricsService.GetTotalCleanupCycles():N0}");

                from.SendMessage(0x59, "Use [SpawnMetrics for full report");
            }
            catch (Exception ex)
            {
                from.SendMessage(0x22, $"Error getting status: {ex.Message}");
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Status command failed - {ex.Message}");
            }
        }

        [Usage("SpawnReload")]
        [Description("Hot-reloads UORespawn settings from Binary files without server restart.")]
        private static void SpawnReload_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            try
            {
                from.SendMessage(0x35, "Reloading UORespawn settings...");

                // Reload settings from Binary
                UORespawnDataBase.ReLoadSpawns();

                from.SendMessage(0x35, "═══════════════════════════════════════════");
                from.SendMessage(0x59, "UORespawn settings reloaded successfully!");
                from.SendMessage(0x35, "═══════════════════════════════════════════");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[UORespawn]: Settings reloaded by {from.Name}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                from.SendMessage(0x22, $"Error reloading settings: {ex.Message}");
                from.SendMessage(0x22, "Check server console for details.");
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Settings reload failed - {ex.Message}");
            }
        }

        [Usage("SpawnPause")]
        [Description("Pauses the UORespawn system (stops spawning new mobs).")]
        private static void SpawnPause_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            try
            {
                if (UORespawnCore.IsPaused())
                {
                    from.SendMessage(0x35, "UORespawn is already paused.");
                    return;
                }

                UORespawnCore.Pause();

                from.SendMessage(0x35, "═══════════════════════════════════════════");
                from.SendMessage(0x35, "UORespawn system PAUSED");
                from.SendMessage(0x48, "No new mobs will spawn until resumed.");
                from.SendMessage(0x48, "Existing mobs remain in world.");
                from.SendMessage(0x59, "Use [SpawnResume to restart spawning.");
                from.SendMessage(0x35, "═══════════════════════════════════════════");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[UORespawn]: System PAUSED by {from.Name}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                from.SendMessage(0x22, $"Error pausing spawn system: {ex.Message}");
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Pause command failed - {ex.Message}");
            }
        }

        [Usage("SpawnResume")]
        [Description("Resumes the UORespawn system (restarts spawning).")]
        private static void SpawnResume_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            try
            {
                if (!UORespawnCore.IsPaused())
                {
                    from.SendMessage(0x35, "UORespawn is not paused.");
                    return;
                }

                UORespawnCore.Resume();

                from.SendMessage(0x35, "═══════════════════════════════════════════");
                from.SendMessage(0x59, "UORespawn system RESUMED");
                from.SendMessage(0x48, "Spawning has restarted normally.");
                from.SendMessage(0x35, "═══════════════════════════════════════════");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[UORespawn]: System RESUMED by {from.Name}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                from.SendMessage(0x22, $"Error resuming spawn system: {ex.Message}");
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Resume command failed - {ex.Message}");
            }
        }

        #region Recycle System Commands

        [Usage("SpawnRecycleStats")]
        [Description("Displays detailed statistics about the recycle pool.")]
        private static void SpawnRecycleStats_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            try
            {
                // Get recycle stats from manager
                var stats = SpawnRecycleService.GetRecycleStats();
                int totalRecycled = SpawnRecycleService.GetTotalRecycled();

                from.SendMessage(0x35, "═══════════════════════════════════════════");
                from.SendMessage(0x59, $"UORespawn Recycle Pool Statistics");
                from.SendMessage(0x35, "═══════════════════════════════════════════");

                if (totalRecycled == 0)
                {
                    from.SendMessage(0x48, "Recycle pool is empty.");
                }
                else
                {
                    from.SendMessage(0x48, $"Total Recycled Mobs: {totalRecycled}/{UORespawnSettings.MAX_RECYCLE_TOTAL}");
                    from.SendMessage(0x35, "───────────────────────────────────────────");

                    if (stats.Count > 0)
                    {
                        from.SendMessage(0x48, "Breakdown by mob type:");
                        from.SendMessage(0x35, "");

                        // Sort by count descending
                        var sortedStats = stats.OrderByDescending(kvp => kvp.Value).Take(15);

                        foreach (var kvp in sortedStats)
                        {
                            string mobType = kvp.Key;
                            int count = kvp.Value;
                            double percentage = (count / (double)UORespawnSettings.MAX_RECYCLE_TYPE) * 100.0;

                            // Color code based on capacity
                            int color = 0x48; // Default green
                            if (percentage >= 80)
                                color = 0x26; // Red (near full)
                            else if (percentage >= 50)
                                color = 0x35; // Yellow (half full)

                            from.SendMessage(color, $"  {mobType,-30} {count,2}/{UORespawnSettings.MAX_RECYCLE_TYPE} ({percentage,5:F1}%)");
                        }

                        if (stats.Count > 15)
                        {
                            from.SendMessage(0x35, $"  ... and {stats.Count - 15} more types");
                        }
                    }
                }

                from.SendMessage(0x35, "═══════════════════════════════════════════");
                from.SendMessage(0x59, $"Active Spawns: {UORespawnCore.GetActiveSpawnCount()}");
                from.SendMessage(0x59, $"Active Players: {UORespawnCore.GetActivePlayerCount()}");
                from.SendMessage(0x35, "═══════════════════════════════════════════");

                // Also write to console
                string consoleStats = SpawnRecycleService.GetStatsString();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(consoleStats);
                Console.ResetColor();

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"{from.Name} checked recycle stats - Pool: {totalRecycled}/{UORespawnSettings.MAX_RECYCLE_TOTAL} mobs");
            }
            catch (Exception ex)
            {
                from.SendMessage(0x22, $"Error getting recycle stats: {ex.Message}");
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: RecycleStats command failed - {ex.Message}");
            }
        }

        [Usage("ClearRecycle")]
        [Description("Clears the recycle pool (deletes all recycled mobs). Administrator only.")]
        private static void ClearRecycle_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            try
            {
                int recycleCount = SpawnRecycleService.GetTotalRecycled();

                if (recycleCount == 0)
                {
                    from.SendMessage(0x48, "Recycle pool is already empty.");
                    return;
                }

                // Clear the recycle pool
                SpawnRecycleService.ClearAll();

                from.SendMessage(0x35, "═══════════════════════════════════════════");
                from.SendMessage(0x59, "Recycle Pool Cleared!");
                from.SendMessage(0x48, $"Deleted {recycleCount} recycled mob(s) from storage.");
                from.SendMessage(0x35, "═══════════════════════════════════════════");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[UORespawn]: Recycle pool cleared by {from.Name} - {recycleCount} mobs deleted");
                Console.ResetColor();

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"{from.Name} cleared recycle pool - Deleted: {recycleCount} mobs");
            }
            catch (Exception ex)
            {
                from.SendMessage(0x22, $"Error clearing recycle pool: {ex.Message}");
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: ClearRecycle command failed - {ex.Message}");
            }
        }

        #endregion
    }
}
