using System;
using System.IO;
using System.Collections.Generic;

namespace Server.Custom.UORespawnSystem.Services
{
    /// <summary>
    /// Simple color-based debug logging system
    /// - Blue/Green/Cyan messages go to console only (system messages)
    /// - Red/Yellow messages buffer to memory when debug enabled, flush to file on toggle/shutdown
    /// </summary>
    internal static class SpawnDebugService
    {
        #region Debug Entry Class

        private class DebugEntry
        {
            public DateTime Timestamp { get; set; }
            public ConsoleColor Color { get; set; }
            public string Message { get; set; }

            public DebugEntry(ConsoleColor color, string message)
            {
                Timestamp = DateTime.Now;
                Color = color;
                Message = message;
            }

            public string ToLogString()
            {
                string colorTag = GetColorTag(Color);
                return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{colorTag}] {Message}";
            }

            private static string GetColorTag(ConsoleColor color)
            {
                switch (color)
                {
                    case ConsoleColor.Red:
                    case ConsoleColor.DarkRed:
                        return "ERROR";
                    case ConsoleColor.Yellow:
                    case ConsoleColor.DarkYellow:
                        return "DEBUG";
                    case ConsoleColor.Green:
                    case ConsoleColor.DarkGreen:
                        return "SUCCESS";
                    case ConsoleColor.Blue:
                    case ConsoleColor.DarkBlue:
                        return "INFO";
                    case ConsoleColor.Cyan:
                    case ConsoleColor.DarkCyan:
                        return "SYSTEM";
                    default:
                        return "LOG";
                }
            }
        }

        #endregion

        #region Fields

        private static readonly string LOG_FILE = Path.Combine(UORespawnSettings.UOR_DATA, "UOR_DebugLog.txt");
        private static readonly object _logLock = new object();

        // In-memory buffer for debug messages (only populated when debug enabled)
        private static readonly List<DebugEntry> _debugBuffer = new List<DebugEntry>();
        private static bool _hasBufferedMessages = false;

        #endregion

        #region Public Methods

        /// <summary>
        /// Buffer a debug message (only if debug enabled)
        /// Does NOT write to file immediately - buffers in memory
        /// </summary>
        internal static void BufferDebugMessage(ConsoleColor color, string message)
        {
            if (!UORespawnSettings.ENABLE_DEBUG)
                return;

            lock (_logLock)
            {
                _debugBuffer.Add(new DebugEntry(color, message));
                _hasBufferedMessages = true;
            }
        }

        /// <summary>
        /// Flush all buffered debug messages to file
        /// Called when: debug toggled off, server shutdown, server crash
        /// </summary>
        internal static void FlushToFile(string reason = null)
        {
            if (!_hasBufferedMessages || _debugBuffer.Count == 0)
                return;

            try
            {
                lock (_logLock)
                {
                    using (StreamWriter writer = new StreamWriter(LOG_FILE, append: true))
                    {
                        // Write header
                        writer.WriteLine("════════════════════════════════════════════════════════");
                        writer.WriteLine($"Debug Session Flush: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        if (!string.IsNullOrEmpty(reason))
                            writer.WriteLine($"Reason: {reason}");
                        writer.WriteLine($"Messages: {_debugBuffer.Count}");
                        writer.WriteLine("════════════════════════════════════════════════════════");
                        writer.WriteLine();

                        // Write all buffered messages
                        foreach (DebugEntry entry in _debugBuffer)
                        {
                            writer.WriteLine(entry.ToLogString());
                        }

                        // Write footer
                        writer.WriteLine();
                        writer.WriteLine("════════════════════════════════════════════════════════");
                        writer.WriteLine();
                    }

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[UORespawn]: Debug log flushed - {_debugBuffer.Count} messages written to file");
                    Console.ResetColor();

                    // Clear buffer after successful flush
                    _debugBuffer.Clear();
                    _hasBufferedMessages = false;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[UORespawn Logger Error]: Failed to flush debug log - {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Clear the debug buffer without writing to file
        /// </summary>
        internal static void ClearBuffer()
        {
            lock (_logLock)
            {
                _debugBuffer.Clear();
                _hasBufferedMessages = false;
            }
        }

        /// <summary>
        /// Get current buffer count (for status display)
        /// </summary>
        internal static int GetBufferCount()
        {
            lock (_logLock)
            {
                return _debugBuffer.Count;
            }
        }

        /// <summary>
        /// Write session start marker (always goes to file)
        /// </summary>
        internal static void WriteSessionStart()
        {
            try
            {
                lock (_logLock)
                {
                    using (StreamWriter writer = new StreamWriter(LOG_FILE, append: true))
                    {
                        writer.WriteLine();
                        writer.WriteLine("╔════════════════════════════════════════════════════════╗");
                        writer.WriteLine("║          UORespawn Session Started                     ║");
                        writer.WriteLine($"║  {DateTime.Now:yyyy-MM-dd HH:mm:ss}                              ║");
                        writer.WriteLine("╚════════════════════════════════════════════════════════╝");
                        writer.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[UORespawn Logger Error]: {ex.Message}");
                Console.ResetColor();
            }
        }

        #endregion
    }
}
