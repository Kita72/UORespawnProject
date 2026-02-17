namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Service for managing in-app debug logging and visualization.
/// Provides a live log viewport that can be toggled on/off from Settings.
/// </summary>
public class DebugService
{
    private readonly List<DebugLogEntry> _logEntries = [];
    private readonly object _lock = new();
    
    /// <summary>
    /// Maximum number of log entries to keep in memory (prevents unbounded growth)
    /// </summary>
    public const int MaxEntries = 1000;
    
    /// <summary>
    /// Whether debug mode is currently enabled
    /// </summary>
    public bool IsEnabled { get; private set; }
    
    /// <summary>
    /// Event raised when a new log entry is added
    /// </summary>
    public event Action<DebugLogEntry>? OnLogEntry;
    
    /// <summary>
    /// Event raised when the log is cleared
    /// </summary>
    public event Action? OnLogCleared;
    
    /// <summary>
    /// Event raised when debug mode is toggled
    /// </summary>
    public event Action<bool>? OnDebugModeChanged;
    
    /// <summary>
    /// Get all current log entries (thread-safe copy)
    /// </summary>
    public List<DebugLogEntry> GetEntries()
    {
        lock (_lock)
        {
            return [.. _logEntries];
        }
    }
    
    /// <summary>
    /// Get the count of log entries
    /// </summary>
    public int EntryCount
    {
        get
        {
            lock (_lock)
            {
                return _logEntries.Count;
            }
        }
    }

    /// <summary>
    /// Enable or disable debug mode
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (IsEnabled != enabled)
        {
            IsEnabled = enabled;

            if (enabled)
            {
                // Load existing log entries from today's log file first
                LoadExistingLogEntries();

                // Then add the "debug mode enabled" message
                AddEntry(DebugLogLevel.Info, "Debug mode enabled - showing session logs");
            }

            OnDebugModeChanged?.Invoke(enabled);
        }
    }

    /// <summary>
    /// Load existing log entries from today's log file into the debug panel
    /// </summary>
    private void LoadExistingLogEntries()
    {
        try
        {
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Logs");
            var logFile = Path.Combine(logDirectory, $"uor_spawn_{DateTime.Today:yyyy-MM-dd}.log");

            if (!File.Exists(logFile)) return;

            var lines = File.ReadAllLines(logFile);

            foreach (var line in lines)
            {
                var entry = ParseLogLine(line);
                if (entry != null)
                {
                    lock (_lock)
                    {
                        _logEntries.Add(entry);

                        // Trim old entries if exceeding max
                        while (_logEntries.Count > MaxEntries)
                        {
                            _logEntries.RemoveAt(0);
                        }
                    }
                }
            }

            // Notify UI of loaded entries
            OnLogCleared?.Invoke(); // Trigger refresh
        }
        catch
        {
            // Silent fail - don't crash if log file can't be read
        }
    }

    /// <summary>
    /// Parse a log line into a DebugLogEntry
    /// Format: [yyyy-MM-dd HH:mm:ss.fff] [LEVEL] message
    /// </summary>
    private static DebugLogEntry? ParseLogLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        try
        {
            // Expected format: [2024-01-15 21:32:26.225] [INFO ] message
            if (!line.StartsWith('[')) return null;

            var firstBracketEnd = line.IndexOf(']');
            if (firstBracketEnd < 0) return null;

            var timestampStr = line[1..firstBracketEnd];
            if (!DateTime.TryParse(timestampStr, out var timestamp)) return null;

            var levelStart = line.IndexOf('[', firstBracketEnd);
            if (levelStart < 0) return null;

            var levelEnd = line.IndexOf(']', levelStart);
            if (levelEnd < 0) return null;

            var levelStr = line[(levelStart + 1)..levelEnd].Trim();
            var message = line[(levelEnd + 1)..].TrimStart();

            var level = levelStr.ToUpperInvariant() switch
            {
                "WARN" => DebugLogLevel.Warning,
                "ERROR" => DebugLogLevel.Error,
                _ => DebugLogLevel.Info
            };

            return new DebugLogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = message,
                IsMark = false
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Add a log entry (called by Logger)
    /// </summary>
    public void AddEntry(DebugLogLevel level, string message, string? source = null)
    {
        if (!IsEnabled) return;
        
        var entry = new DebugLogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            Source = source
        };
        
        lock (_lock)
        {
            _logEntries.Add(entry);
            
            // Trim old entries if exceeding max
            while (_logEntries.Count > MaxEntries)
            {
                _logEntries.RemoveAt(0);
            }
        }
        
        OnLogEntry?.Invoke(entry);
    }
    
    /// <summary>
    /// Add a visual mark/separator in the log (for manual testing)
    /// </summary>
    public void AddMark(string? label = null)
    {
        var markText = string.IsNullOrWhiteSpace(label) 
            ? "══════════ MARK ══════════" 
            : $"══════════ {label.ToUpperInvariant()} ══════════";
            
        var entry = new DebugLogEntry
        {
            Timestamp = DateTime.Now,
            Level = DebugLogLevel.Mark,
            Message = markText,
            IsMark = true
        };
        
        lock (_lock)
        {
            _logEntries.Add(entry);
        }
        
        OnLogEntry?.Invoke(entry);
    }
    
    /// <summary>
    /// Add a test start marker
    /// </summary>
    public void StartTest(string testName)
    {
        AddMark($"TEST START: {testName}");
    }
    
    /// <summary>
    /// Add a test end marker
    /// </summary>
    public void EndTest(string testName)
    {
        AddMark($"TEST END: {testName}");
    }
    
    /// <summary>
    /// Clear all log entries
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _logEntries.Clear();
        }
        
        OnLogCleared?.Invoke();
        
        if (IsEnabled)
        {
            AddEntry(DebugLogLevel.Info, "Log cleared");
        }
    }
    
    /// <summary>
    /// Get formatted log text for copying
    /// </summary>
    public string GetLogText()
    {
        lock (_lock)
        {
            return string.Join(Environment.NewLine, _logEntries.Select(e => e.ToString()));
        }
    }
    
    /// <summary>
    /// Get formatted log text for a specific time range
    /// </summary>
    public string GetLogTextSince(DateTime since)
    {
        lock (_lock)
        {
            var filtered = _logEntries.Where(e => e.Timestamp >= since);
            return string.Join(Environment.NewLine, filtered.Select(e => e.ToString()));
        }
    }
}

/// <summary>
/// Log level for debug entries
/// </summary>
public enum DebugLogLevel
{
    Info,
    Warning,
    Error,
    Mark
}

/// <summary>
/// A single debug log entry
/// </summary>
public class DebugLogEntry
{
    public DateTime Timestamp { get; set; }
    public DebugLogLevel Level { get; set; }
    public string Message { get; set; } = "";
    public string? Source { get; set; }
    public bool IsMark { get; set; }
    
    /// <summary>
    /// Get CSS class for styling based on log level
    /// </summary>
    public string CssClass => Level switch
    {
        DebugLogLevel.Warning => "log-warning",
        DebugLogLevel.Error => "log-error",
        DebugLogLevel.Mark => "log-mark",
        _ => "log-info"
    };
    
    /// <summary>
    /// Get icon for log level
    /// </summary>
    public string Icon => Level switch
    {
        DebugLogLevel.Warning => "bi-exclamation-triangle",
        DebugLogLevel.Error => "bi-x-circle",
        DebugLogLevel.Mark => "bi-bookmark-star",
        _ => "bi-info-circle"
    };
    
    public override string ToString()
    {
        var timestamp = Timestamp.ToString("HH:mm:ss.fff");
        var level = Level.ToString().ToUpper().PadRight(7);
        var source = string.IsNullOrEmpty(Source) ? "" : $"[{Source}] ";
        return $"{timestamp} | {level} | {source}{Message}";
    }
}
