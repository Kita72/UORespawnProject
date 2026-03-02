using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Server.Custom.UORespawnServer.Managers
{
    /// <summary>
    /// Session logger - buffers all messages and flushes to file on shutdown/crash.
    /// Only keeps the last session log for easy review in Editor.
    /// Supports log levels, categories, and call tracking.
    /// </summary>
    internal static class LogManager
    {
        private static readonly List<string> _SessionLog = new List<string>();
        private static DateTime _SessionStart;

        // Counters for summary
        private static int _InfoCount;
        private static int _DebugCount;
        private static int _ErrorCount;

        internal static void Initialize()
        {
            _SessionStart = DateTime.Now;
            _SessionLog.Clear();
            _InfoCount = 0;
            _DebugCount = 0;
            _ErrorCount = 0;
        }

        /// <summary>
        /// Legacy method for backward compatibility.
        /// </summary>
        internal static void LogMessage(ConsoleColor level, string message)
        {
            switch (level)
            {
                case ConsoleColor.Yellow:
                case ConsoleColor.DarkYellow:
                    _DebugCount++;
                    break;
                case ConsoleColor.Red:
                case ConsoleColor.DarkRed:
                    _ErrorCount++;
                    break;
                default:
                    _InfoCount++;
                    break;
            }

            _SessionLog.Add($"{GetLevelTag(level)}:{message}");
        }

        private static string GetLevelTag(ConsoleColor level)
        {
            switch (level)
            {
                case ConsoleColor.Yellow:
                case ConsoleColor.DarkYellow:
                    return "DEBG";
                case ConsoleColor.Red:
                case ConsoleColor.DarkRed:
                    return "ERROR";
                default: return "INFO";
            }
        }

        internal static void FlushToFile(string reason)
        {
            if (_SessionLog == null || _SessionLog.Count == 0) return;

            try
            {
                var sb = new StringBuilder();

                // Header
                sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
                sb.AppendLine("║                    UORespawn Session Log                      ");
                sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
                sb.AppendLine();

                // System Info
                sb.AppendLine("┌─────────────────────────────────────────────────────────────┐");
                sb.AppendLine("│ SESSION INFO                                                 ");
                sb.AppendLine("├─────────────────────────────────────────────────────────────┤");
                sb.AppendLine($"│ Version      : {UOR_Settings.VERSION,-45} │");
                sb.AppendLine($"│ Session Start: {_SessionStart,-45:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"│ Session End  : {DateTime.Now,-45:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"│ Duration     : {(DateTime.Now - _SessionStart),-45:hh\\:mm\\:ss}");
                sb.AppendLine($"│ End Reason   : {reason,-45}");
                sb.AppendLine("└─────────────────────────────────────────────────────────────┘");
                sb.AppendLine();

                // Log Summary
                sb.AppendLine("┌─────────────────────────────────────────────────────────────┐");
                sb.AppendLine("│ LOG SUMMARY                                                  ");
                sb.AppendLine("├─────────────────────────────────────────────────────────────┤");
                sb.AppendLine($"│ INFO: {_InfoCount,-8} DEBUG: {_DebugCount,-8} ERROR: {_ErrorCount,-8}");
                sb.AppendLine($"│ Total Entries: {_SessionLog.Count,-10}");
                sb.AppendLine("└─────────────────────────────────────────────────────────────┘");
                sb.AppendLine();

                // Settings Snapshot
                sb.AppendLine("┌─────────────────────────────────────────────────────────────┐");
                sb.AppendLine("│ SETTINGS SNAPSHOT                                            ");
                sb.AppendLine("├─────────────────────────────────────────────────────────────┤");
                sb.AppendLine($"│ Max Spawn: {UOR_Settings.MAX_SPAWN,-5} Max Range: {UOR_Settings.MAX_RANGE,-5} Debug: {UOR_Settings.ENABLE_DEBUG,-10}");
                sb.AppendLine($"│ Vendors : {(UOR_Settings.ENABLE_VENDOR_SPAWN ? "ON" : "OFF"),-5} Effects : {(UOR_Settings.ENABLE_SPAWN_EFFECTS ? "ON" : "OFF"),-5} Towns: {(UOR_Settings.ENABLE_TOWN_SPAWN ? "ON" : "OFF"),-10}");
                sb.AppendLine("└─────────────────────────────────────────────────────────────┘");
                sb.AppendLine();

                // Log Entries
                sb.AppendLine("┌─────────────────────────────────────────────────────────────┐");
                sb.AppendLine("│ SESSION LOG                                                  ");
                sb.AppendLine("└─────────────────────────────────────────────────────────────┘");
                sb.AppendLine();

                foreach (var entry in _SessionLog)
                {
                    sb.AppendLine(entry);
                }

                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"END OF LOG - {_SessionLog.Count} entries                      ");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");

                File.WriteAllText(UOR_DIR.LOG_DEBUG_FILE, sb.ToString());

                _SessionLog.Clear();
            }
            catch (Exception ex)
            {
                // Last resort logging - we're likely in shutdown/crash state
                Console.WriteLine($"[UORespawn] LogManager.FlushToFile failed: {ex.Message}");
            }
        }
    }
}
