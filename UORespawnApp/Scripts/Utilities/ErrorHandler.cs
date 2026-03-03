namespace UORespawnApp.Scripts.Utilities;

/// <summary>
/// Centralized error handling utility for consistent error management across the application.
/// Provides standardized methods for handling exceptions with logging and optional user notification.
/// </summary>
public static class ErrorHandler
{
    /// <summary>
    /// Handles an exception by logging it and optionally notifying the user.
    /// Use for recoverable errors where the operation can continue.
    /// </summary>
    /// <param name="ex">The exception that occurred</param>
    /// <param name="context">Description of what operation was being performed</param>
    /// <param name="notifyUser">Whether to show a toast notification to the user</param>
    /// <returns>A user-friendly error message</returns>
    public static string Handle(Exception ex, string context, bool notifyUser = false)
    {
        var message = FormatErrorMessage(ex, context);
        Logger.Error(message, ex);

        if (notifyUser)
        {
            // Toast notification would be shown here if ToastService was injected
            // For now, we just log - components can check the return message
        }

        return message;
    }

    /// <summary>
    /// Handles an exception silently (log only, no user notification).
    /// Use for background operations where user doesn't need to know about failures.
    /// </summary>
    /// <param name="ex">The exception that occurred</param>
    /// <param name="context">Description of what operation was being performed</param>
    public static void HandleSilent(Exception ex, string context)
    {
        Logger.Warning($"{context}: {ex.Message}");
    }

    /// <summary>
    /// Executes an action with standardized error handling.
    /// Returns true if successful, false if an exception occurred.
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <param name="context">Description of what operation is being performed</param>
    /// <param name="notifyUser">Whether to show a toast on failure</param>
    /// <returns>True if successful, false if an error occurred</returns>
    public static bool TryExecute(Action action, string context, bool notifyUser = false)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            Handle(ex, context, notifyUser);
            return false;
        }
    }

    /// <summary>
    /// Executes a function with standardized error handling.
    /// Returns the result if successful, or the default value if an exception occurred.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="func">The function to execute</param>
    /// <param name="context">Description of what operation is being performed</param>
    /// <param name="defaultValue">Value to return if an error occurs</param>
    /// <param name="notifyUser">Whether to show a toast on failure</param>
    /// <returns>The function result or default value on error</returns>
    public static T? TryExecute<T>(Func<T> func, string context, T? defaultValue = default, bool notifyUser = false)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            Handle(ex, context, notifyUser);
            return defaultValue;
        }
    }

    /// <summary>
    /// Executes an async action with standardized error handling.
    /// </summary>
    /// <param name="asyncAction">The async action to execute</param>
    /// <param name="context">Description of what operation is being performed</param>
    /// <param name="notifyUser">Whether to show a toast on failure</param>
    /// <returns>True if successful, false if an error occurred</returns>
    public static async Task<bool> TryExecuteAsync(Func<Task> asyncAction, string context, bool notifyUser = false)
    {
        try
        {
            await asyncAction();
            return true;
        }
        catch (Exception ex)
        {
            Handle(ex, context, notifyUser);
            return false;
        }
    }

    /// <summary>
    /// Executes an async function with standardized error handling.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="asyncFunc">The async function to execute</param>
    /// <param name="context">Description of what operation is being performed</param>
    /// <param name="defaultValue">Value to return if an error occurs</param>
    /// <param name="notifyUser">Whether to show a toast on failure</param>
    /// <returns>The function result or default value on error</returns>
    public static async Task<T?> TryExecuteAsync<T>(Func<Task<T>> asyncFunc, string context, T? defaultValue = default, bool notifyUser = false)
    {
        try
        {
            return await asyncFunc();
        }
        catch (Exception ex)
        {
            Handle(ex, context, notifyUser);
            return defaultValue;
        }
    }

    /// <summary>
    /// Formats an exception into a user-friendly message.
    /// </summary>
    private static string FormatErrorMessage(Exception ex, string context)
    {
        // Handle common exception types with friendlier messages
        var friendlyMessage = ex switch
        {
            IOException => "File or folder access error",
            UnauthorizedAccessException => "Permission denied",
            TimeoutException => "Operation timed out",
            OperationCanceledException => "Operation was cancelled",
            ArgumentException => "Invalid input",
            InvalidOperationException => "Invalid operation",
            _ => ex.GetType().Name
        };

        return $"{context}: {friendlyMessage} - {ex.Message}";
    }

    /// <summary>
    /// Gets a user-friendly message for common exceptions.
    /// Use when you need just the message without logging.
    /// </summary>
    /// <param name="ex">The exception</param>
    /// <returns>A user-friendly error message</returns>
    public static string GetFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            IOException ioEx => $"File access error: {ioEx.Message}",
            UnauthorizedAccessException => "Permission denied. Try running as administrator or check folder permissions.",
            TimeoutException => "The operation timed out. Please try again.",
            OperationCanceledException => "Operation was cancelled.",
            ArgumentNullException argEx => $"Missing required value: {argEx.ParamName}",
            ArgumentException argEx => $"Invalid value: {argEx.Message}",
            InvalidOperationException => $"Invalid operation: {ex.Message}",
            HttpRequestException => "Network error. Check your internet connection.",
            _ => ex.Message
        };
    }
}
