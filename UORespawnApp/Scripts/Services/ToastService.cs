namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Service for displaying toast notifications to users.
/// Provides non-blocking UI feedback for user-facing information, warnings, and errors.
/// </summary>
public class ToastService
{
    private readonly List<ToastMessage> _messages = new();
    
    /// <summary>
    /// Event triggered when a new toast message is added
    /// </summary>
    public event Action? OnToastChanged;

    /// <summary>
    /// Get all current toast messages
    /// </summary>
    public List<ToastMessage> Messages => _messages;

    /// <summary>
    /// Show an informational toast (blue)
    /// </summary>
    public void ShowInfo(string message, int durationMs = 3000)
    {
        AddToast(message, ToastType.Info, durationMs);
    }

    /// <summary>
    /// Show a success toast (green)
    /// </summary>
    public void ShowSuccess(string message, int durationMs = 3000)
    {
        AddToast(message, ToastType.Success, durationMs);
    }

    /// <summary>
    /// Show a warning toast (yellow)
    /// </summary>
    public void ShowWarning(string message, int durationMs = 3000)
    {
        AddToast(message, ToastType.Warning, durationMs);
    }

    /// <summary>
    /// Show an error toast (red)
    /// </summary>
    public void ShowError(string message, int durationMs = 5000)
    {
        AddToast(message, ToastType.Error, durationMs);
    }

    private void AddToast(string message, ToastType type, int durationMs)
    {
        var toast = new ToastMessage
        {
            Id = Guid.NewGuid(),
            Message = message,
            Type = type,
            Timestamp = DateTime.Now
        };

        lock (_messages)
        {
            _messages.Add(toast);
        }

        OnToastChanged?.Invoke();

        // Auto-remove after duration
        if (durationMs > 0)
        {
            Task.Run(async () =>
            {
                await Task.Delay(durationMs);
                RemoveToast(toast.Id);
            });
        }
    }

    /// <summary>
    /// Remove a specific toast by ID
    /// </summary>
    public void RemoveToast(Guid id)
    {
        bool removed = false;

        lock (_messages)
        {
            var toast = _messages.FirstOrDefault(m => m.Id == id);
            if (toast != null)
            {
                _messages.Remove(toast);
                removed = true;
            }
        }

        if (removed)
        {
            OnToastChanged?.Invoke();
        }
    }

    /// <summary>
    /// Clear all toasts
    /// </summary>
    public void ClearAll()
    {
        lock (_messages)
        {
            _messages.Clear();
        }

        OnToastChanged?.Invoke();
    }
}

/// <summary>
/// Represents a single toast notification
/// </summary>
public class ToastMessage
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Toast notification types with corresponding Bootstrap alert colors
/// </summary>
public enum ToastType
{
    Info,       // Blue - informational messages
    Success,    // Green - successful operations
    Warning,    // Yellow - warnings and missing data
    Error       // Red - errors and failures
}
