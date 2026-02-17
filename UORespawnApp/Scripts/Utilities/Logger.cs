using UORespawnApp.Scripts.Services;

namespace UORespawnApp.Scripts.Utilities;

/// <summary>
/// Simple file-based logger that writes to daily log files in the app's Data directory.
/// Thread-safe and performance-optimized with lazy initialization.
/// Also sends entries to DebugService for in-app visualization when debug mode is enabled.
/// </summary>
public static class Logger
{
    private static readonly string LogDirectory;
    private static readonly Lock _lock = new();
    private static string? _currentLogFile;
    private static DateTime _currentLogDate;
    private static bool _initialized = false;

    /// <summary>
    /// Reference to DebugService for in-app log visualization.
    /// Set during app startup from MauiProgram.
    /// </summary>
    public static DebugService? DebugService { get; set; }

    static Logger()
    {
        // Store logs in the Data/Logs folder alongside spawn files and maps
        LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Logs");
        _currentLogDate = DateTime.Today;
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return; // Double-check inside lock

            try
            {
                Directory.CreateDirectory(LogDirectory);
                _currentLogFile = GetLogFilePath(DateTime.Today);

                // Defer cleanup to background task to avoid blocking startup
                Task.Run(() => CleanupOldLogs());

                _initialized = true;
                Write("INFO", "=== Application Started ===");
            }
            catch
            {
                // If initialization fails, remain uninitialized but don't crash
                _initialized = true; // Mark as initialized to avoid repeated failures
            }
        }
    }

    /// <summary>
    /// Log an informational message (normal operations, status updates)
    /// </summary>
    public static void Info(string message)
    {
        EnsureInitialized();
        Write("INFO", message);
        DebugService?.AddEntry(DebugLogLevel.Info, message);
    }

    /// <summary>
    /// Log a warning message (recoverable issues, missing data, non-critical problems)
    /// </summary>
    public static void Warning(string message)
    {
        EnsureInitialized();
        Write("WARN", message);
        DebugService?.AddEntry(DebugLogLevel.Warning, message);
    }

    /// <summary>
    /// Log an error message (exceptions, critical failures, data corruption)
    /// </summary>
    public static void Error(string message)
    {
        EnsureInitialized();
        Write("ERROR", message);
        DebugService?.AddEntry(DebugLogLevel.Error, message);
    }

    /// <summary>
    /// Log an error with exception details
    /// </summary>
    public static void Error(string message, Exception ex)
    {
        EnsureInitialized();
        var fullMessage = $"{message} | Exception: {ex.GetType().Name} - {ex.Message}";
        Write("ERROR", $"{fullMessage}\n{ex.StackTrace}");
        DebugService?.AddEntry(DebugLogLevel.Error, fullMessage);
    }

    private static void Write(string level, string message)
    {
        try
        {
            // Check if we need to roll over to a new log file (new day)
            if (DateTime.Today != _currentLogDate)
            {
                lock (_lock)
                {
                    if (DateTime.Today != _currentLogDate) // Double-check inside lock
                    {
                        _currentLogDate = DateTime.Today;
                        _currentLogFile = GetLogFilePath(DateTime.Today);
                        Info("=== New Day - Log Rolled Over ===");
                    }
                }
            }

            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level,-5}] {message}";
            
            lock (_lock)
            {
                File.AppendAllText(_currentLogFile!, logEntry + Environment.NewLine);
            }
        }
        catch
        {
            // Silent fail - never crash the app because of logging
            // We could optionally write to Debug output here
        }
    }

    private static string GetLogFilePath(DateTime date)
    {
        return Path.Combine(LogDirectory, $"uor_spawn_{date:yyyy-MM-dd}.log");
    }

    private static void CleanupOldLogs()
    {
        try
        {
            var cutoffDate = DateTime.Today.AddDays(-7);
            var logFiles = Directory.GetFiles(LogDirectory, "uor_spawn_*.log");
            
            foreach (var file in logFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Silent fail on cleanup
        }
    }
}
