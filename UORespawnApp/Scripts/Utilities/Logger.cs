namespace UORespawnApp.Scripts.Utilities;

/// <summary>
/// Simple file-based logger that writes to daily log files in the app's Data directory.
/// Thread-safe and performance-optimized.
/// </summary>
public static class Logger
{
    private static readonly string LogDirectory;
    private static readonly object _lock = new object();
    private static string? _currentLogFile;
    private static DateTime _currentLogDate;

    static Logger()
    {
        // Store logs in the app's local data folder
        LogDirectory = Path.Combine(FileSystem.AppDataDirectory, "Logs");
        Directory.CreateDirectory(LogDirectory);
        
        _currentLogDate = DateTime.Today;
        _currentLogFile = GetLogFilePath(DateTime.Today);
        
        // Clean up old logs on startup (keep last 7 days)
        CleanupOldLogs();
        
        Info("=== Application Started ===");
    }

    /// <summary>
    /// Log an informational message (normal operations, status updates)
    /// </summary>
    public static void Info(string message)
    {
        Write("INFO", message);
    }

    /// <summary>
    /// Log a warning message (recoverable issues, missing data, non-critical problems)
    /// </summary>
    public static void Warning(string message)
    {
        Write("WARN", message);
    }

    /// <summary>
    /// Log an error message (exceptions, critical failures, data corruption)
    /// </summary>
    public static void Error(string message)
    {
        Write("ERROR", message);
    }

    /// <summary>
    /// Log an error with exception details
    /// </summary>
    public static void Error(string message, Exception ex)
    {
        Write("ERROR", $"{message} | Exception: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
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

    /// <summary>
    /// Get the path to the current log file (for displaying to users)
    /// </summary>
    public static string GetCurrentLogPath()
    {
        return _currentLogFile ?? GetLogFilePath(DateTime.Today);
    }

    /// <summary>
    /// Get the logs directory path (for displaying to users or opening in file explorer)
    /// </summary>
    public static string GetLogsDirectory()
    {
        return LogDirectory;
    }
}
