using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Custom.UORespawnSystem.SpawnUtility;

namespace Server.Custom.UORespawnSystem.Services
{
    internal static class SpawnMetricsService
    {
        // Spawn Counters
        private static int _totalSpawns = 0;           // Total spawns processed (new + recycled)
        private static int _totalRecycled = 0;         // Mobs teleported from recycle pool
        private static int _totalNewSpawns = 0;        // Fresh mobs created
        private static int _totalDeleted = 0;          // Mobs removed from world
        private static int _totalAddedToRecycle = 0;   // Mobs added to recycle pool
        private static int _totalAddedToCleanup = 0;   // Mobs marked for deletion

        // Cleanup Cycle Tracking
        private static readonly List<double> _cleanupTimes = new List<double>(100);  // Last 100 cleanup times
        private static double _lastCleanupTime = 0;
        private static int _totalCleanupCycles = 0;

        // Queue Tracking
        private static int _totalQueued = 0;
        private static int _peakQueueSize = 0;

        // Session Tracking
        private static DateTime _startTime = DateTime.UtcNow;
        private static DateTime _lastResetTime = DateTime.UtcNow;

        // Player Tracking (optional detailed metrics)
        private static readonly Dictionary<string, PlayerMetrics> _playerMetrics = new Dictionary<string, PlayerMetrics>();

        private class PlayerMetrics
        {
            public int SpawnsReceived { get; set; }
            public int RecycledReceived { get; set; }
            public int NewReceived { get; set; }
            public DateTime FirstSpawn { get; set; }
            public DateTime LastSpawn { get; set; }

            public PlayerMetrics()
            {
                FirstSpawn = DateTime.UtcNow;
                LastSpawn = DateTime.UtcNow;
            }
        }

        #region Recording Methods

        /// <summary>
        /// Record a spawn (either new or recycled)
        /// </summary>
        internal static void RecordSpawn(PlayerMobile pm, bool wasRecycled)
        {
            _totalSpawns++;

            if (wasRecycled)
            {
                _totalRecycled++;
            }
            else
            {
                _totalNewSpawns++;
            }

            // Track per-player metrics
            if (pm != null)
            {
                string playerName = pm.Name;

                if (!_playerMetrics.ContainsKey(playerName))
                {
                    _playerMetrics[playerName] = new PlayerMetrics();
                }

                var metrics = _playerMetrics[playerName];
                metrics.SpawnsReceived++;
                metrics.LastSpawn = DateTime.UtcNow;

                if (wasRecycled)
                {
                    metrics.RecycledReceived++;
                }
                else
                {
                    metrics.NewReceived++;
                }
            }
        }

        /// <summary>
        /// Record mobs being deleted from world
        /// </summary>
        internal static void RecordDeletion(int count)
        {
            _totalDeleted += count;
        }

        /// <summary>
        /// Record mobs being added to recycle pool
        /// </summary>
        internal static void RecordRecyclePoolAddition(int count)
        {
            _totalAddedToRecycle += count;
        }

        /// <summary>
        /// Record a mob being retrieved from recycle pool
        /// </summary>
        internal static void RecordRecyclePoolRetrieval()
        {
            // This is already counted in RecordSpawn(wasRecycled=true)
            // This method exists for compatibility but doesn't need separate tracking
        }

        /// <summary>
        /// Record mobs being marked for cleanup
        /// </summary>
        internal static void RecordCleanupAddition(int count)
        {
            _totalAddedToCleanup += count;
        }

        /// <summary>
        /// Record a cleanup cycle's execution time
        /// </summary>
        internal static void RecordCleanupTime(double milliseconds)
        {
            _totalCleanupCycles++;
            _lastCleanupTime = milliseconds;

            // Keep only last 100 samples for averaging
            if (_cleanupTimes.Count >= 100)
            {
                _cleanupTimes.RemoveAt(0);
            }

            _cleanupTimes.Add(milliseconds);
        }

        /// <summary>
        /// Record a complete cleanup (shutdown, crash, or manual clear)
        /// </summary>
        internal static void RecordCleanup(int activeDeleted, int recycledDeleted, string reason)
        {
            _totalDeleted += activeDeleted;
            _totalDeleted += recycledDeleted;

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green,
                $"METRICS: Recorded cleanup - Active:{activeDeleted}, Recycled:{recycledDeleted}, Reason:{reason}");
        }

        /// <summary>
        /// Record spawn being queued
        /// </summary>
        internal static void RecordQueue(int currentQueueSize)
        {
            _totalQueued++;

            if (currentQueueSize > _peakQueueSize)
            {
                _peakQueueSize = currentQueueSize;
            }
        }

        /// <summary>
        /// Record player disconnecting (cleanup player metrics)
        /// </summary>
        internal static void RecordPlayerLogout(PlayerMobile pm)
        {
            if (pm != null && _playerMetrics.ContainsKey(pm.Name))
            {
                // Keep metrics for reporting, just update last seen
                _playerMetrics[pm.Name].LastSpawn = DateTime.UtcNow;
            }
        }

        #endregion

        #region Calculation Methods

        /// <summary>
        /// Calculate recycle rate as percentage
        /// </summary>
        internal static double GetRecycleRate()
        {
            if (_totalSpawns == 0)
                return 0.0;

            return (_totalRecycled / (double)_totalSpawns) * 100.0;
        }

        /// <summary>
        /// Get average cleanup cycle time in milliseconds
        /// </summary>
        internal static double GetAverageCleanupTime()
        {
            if (_cleanupTimes.Count == 0)
                return 0.0;

            return _cleanupTimes.Average();
        }

        /// <summary>
        /// Get peak cleanup time in milliseconds
        /// </summary>
        internal static double GetPeakCleanupTime()
        {
            if (_cleanupTimes.Count == 0)
                return 0.0;

            return _cleanupTimes.Max();
        }

        /// <summary>
        /// Get minimum cleanup time in milliseconds
        /// </summary>
        internal static double GetMinCleanupTime()
        {
            if (_cleanupTimes.Count == 0)
                return 0.0;

            return _cleanupTimes.Min();
        }

        /// <summary>
        /// Get session uptime
        /// </summary>
        internal static TimeSpan GetUptime()
        {
            return DateTime.UtcNow - _startTime;
        }

        /// <summary>
        /// Get time since last reset
        /// </summary>
        internal static TimeSpan GetTimeSinceReset()
        {
            return DateTime.UtcNow - _lastResetTime;
        }

        /// <summary>
        /// Get spawns per minute rate
        /// </summary>
        internal static double GetSpawnsPerMinute()
        {
            TimeSpan elapsed = GetTimeSinceReset();

            if (elapsed.TotalMinutes < 0.1)
                return 0.0;

            return _totalSpawns / elapsed.TotalMinutes;
        }

        /// <summary>
        /// Get cleanup cycles per minute
        /// </summary>
        internal static double GetCleanupCyclesPerMinute()
        {
            TimeSpan elapsed = GetTimeSinceReset();

            if (elapsed.TotalMinutes < 0.1)
                return 0.0;

            return _totalCleanupCycles / elapsed.TotalMinutes;
        }

        #endregion

        #region Reporting Methods

        /// <summary>
        /// Generate a comprehensive metrics report
        /// </summary>
        internal static string GetReport(bool includePlayerMetrics = false)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║           UORespawn Performance Metrics Report               ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();

            // Session Info
            TimeSpan uptime = GetUptime();
            TimeSpan sinceReset = GetTimeSinceReset();

            sb.AppendLine("┌─ SESSION INFORMATION ─────────────────────────────────────┐");
            sb.AppendLine($"│ System Started:    {_startTime:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"│ Total Uptime:      {FormatTimeSpan(uptime)}");
            sb.AppendLine($"│ Last Reset:        {_lastResetTime:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"│ Time Since Reset:  {FormatTimeSpan(sinceReset)}");
            sb.AppendLine("└───────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            // Spawn Statistics
            double recycleRate = GetRecycleRate();

            sb.AppendLine("┌─ SPAWN STATISTICS ────────────────────────────────────────┐");
            sb.AppendLine($"│ Total Spawns:      {_totalSpawns,10:N0}  ({GetSpawnsPerMinute():F1}/min)");
            sb.AppendLine($"│   ├─ Recycled:     {_totalRecycled,10:N0}  ({recycleRate:F1}%)");
            sb.AppendLine($"│   └─ New Created:  {_totalNewSpawns,10:N0}  ({100 - recycleRate:F1}%)");
            sb.AppendLine($"│");
            sb.AppendLine($"│ Total Queued:      {_totalQueued,10:N0}");
            sb.AppendLine($"│ Peak Queue Size:   {_peakQueueSize,10:N0}");
            sb.AppendLine("└───────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            // Cleanup Statistics
            double avgCleanupTime = GetAverageCleanupTime();
            double peakCleanupTime = GetPeakCleanupTime();
            double minCleanupTime = GetMinCleanupTime();

            sb.AppendLine("┌─ CLEANUP STATISTICS ──────────────────────────────────────┐");
            sb.AppendLine($"│ Total Cycles:      {_totalCleanupCycles,10:N0}  ({GetCleanupCyclesPerMinute():F1}/min)");
            sb.AppendLine($"│ Last Cycle Time:   {_lastCleanupTime,10:F2} ms");
            sb.AppendLine($"│ Average Time:      {avgCleanupTime,10:F2} ms");
            sb.AppendLine($"│ Peak Time:         {peakCleanupTime,10:F2} ms");
            sb.AppendLine($"│ Min Time:          {minCleanupTime,10:F2} ms");
            sb.AppendLine($"│");
            sb.AppendLine($"│ Added to Recycle:  {_totalAddedToRecycle,10:N0}");
            sb.AppendLine($"│ Marked for Delete: {_totalAddedToCleanup,10:N0}");
            sb.AppendLine($"│ Total Deleted:     {_totalDeleted,10:N0}");
            sb.AppendLine("└───────────────────────────────────────────────────────────┘");
            sb.AppendLine();

            // Performance Assessment
            sb.AppendLine("┌─ PERFORMANCE ASSESSMENT ──────────────────────────────────┐");
            sb.AppendLine($"│ Recycle Rate:      {GetRecycleRatingEmoji(recycleRate)} {recycleRate:F1}% {GetRecycleRating(recycleRate)}");
            sb.AppendLine($"│ Cleanup Speed:     {GetCleanupSpeedEmoji(avgCleanupTime)} {avgCleanupTime:F2}ms {GetCleanupSpeedRating(avgCleanupTime)}");
            sb.AppendLine($"│ Spawn Throughput:  {GetSpawnThroughputEmoji()} {GetSpawnsPerMinute():F1}/min");
            sb.AppendLine("└───────────────────────────────────────────────────────────┘");

            // Player Metrics (optional)
            if (includePlayerMetrics && _playerMetrics.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("┌─ TOP 10 PLAYERS BY SPAWNS ────────────────────────────────┐");

                var topPlayers = _playerMetrics
                    .OrderByDescending(kvp => kvp.Value.SpawnsReceived)
                    .Take(10)
                    .ToList();

                foreach (var kvp in topPlayers)
                {
                    string name = kvp.Key.Length > 15 ? kvp.Key.Substring(0, 12) + "..." : kvp.Key;
                    var metrics = kvp.Value;
                    double playerRecycleRate = metrics.SpawnsReceived > 0 ? (metrics.RecycledReceived / (double)metrics.SpawnsReceived) * 100.0 : 0.0;

                    sb.AppendLine($"│ {name,-15} {metrics.SpawnsReceived,6:N0} spawns  ({playerRecycleRate,5:F1}% recycled)");
                }

                sb.AppendLine("└───────────────────────────────────────────────────────────┘");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get a compact one-line summary
        /// </summary>
        internal static string GetSummary()
        {
            double recycleRate = GetRecycleRate();
            double avgCleanupTime = GetAverageCleanupTime();

            return $"Spawns: {_totalSpawns:N0} ({recycleRate:F1}% recycled) | Cleanup: {avgCleanupTime:F2}ms avg | Uptime: {FormatTimeSpan(GetUptime())}";
        }

        #endregion

        #region Helper Methods

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
            else if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
            else if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
            else
                return $"{ts.Seconds}s";
        }

        private static string GetRecycleRating(double rate)
        {
            if (rate >= 40) return "(Excellent)";
            if (rate >= 30) return "(Good)";
            if (rate >= 20) return "(Fair)";
            return "(Poor)";
        }

        private static string GetRecycleRatingEmoji(double rate)
        {
            if (rate >= 40) return "✓✓";
            if (rate >= 30) return "✓";
            if (rate >= 20) return "~";
            return "✗";
        }

        private static string GetCleanupSpeedRating(double ms)
        {
            if (ms <= 5) return "(Excellent)";
            if (ms <= 10) return "(Good)";
            if (ms <= 20) return "(Fair)";
            return "(Slow)";
        }

        private static string GetCleanupSpeedEmoji(double ms)
        {
            if (ms <= 5) return "✓✓";
            if (ms <= 10) return "✓";
            if (ms <= 20) return "~";
            return "✗";
        }

        private static string GetSpawnThroughputEmoji()
        {
            double spm = GetSpawnsPerMinute();
            if (spm >= 10) return "✓✓";
            if (spm >= 5) return "✓";
            if (spm >= 1) return "~";
            return "✗";
        }

        #endregion

        #region Reset Methods

        /// <summary>
        /// Reset all metrics (keeps session start time)
        /// </summary>
        internal static void Reset()
        {
            _totalSpawns = 0;
            _totalRecycled = 0;
            _totalNewSpawns = 0;
            _totalDeleted = 0;
            _totalAddedToRecycle = 0;
            _totalAddedToCleanup = 0;
            _totalQueued = 0;
            _peakQueueSize = 0;

            _cleanupTimes.Clear();
            _lastCleanupTime = 0;
            _totalCleanupCycles = 0;

            _playerMetrics.Clear();

            _lastResetTime = DateTime.UtcNow;

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "METRICS RESET: All counters cleared");
        }

        /// <summary>
        /// Full reset including session start time
        /// </summary>
        internal static void FullReset()
        {
            Reset();
            _startTime = DateTime.UtcNow;
            _lastResetTime = DateTime.UtcNow;

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "METRICS FULL RESET: Session restarted");
        }

        #endregion

        #region Raw Access Methods (for commands/gumps)

        internal static int GetTotalSpawns() => _totalSpawns;
        internal static int GetTotalRecycled() => _totalRecycled;
        internal static int GetTotalNewSpawns() => _totalNewSpawns;
        internal static int GetTotalDeleted() => _totalDeleted;
        internal static int GetTotalQueued() => _totalQueued;
        internal static int GetPeakQueueSize() => _peakQueueSize;
        internal static int GetTotalCleanupCycles() => _totalCleanupCycles;
        internal static double GetLastCleanupTime() => _lastCleanupTime;

        #endregion
    }
}
