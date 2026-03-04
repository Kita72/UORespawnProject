namespace UORespawnApp.Scripts.Utilities;

/// <summary>
/// Utility class for robust file operations with retry logic.
/// Handles transient file access failures (locks, network issues, antivirus scans).
/// Uses exponential backoff to avoid hammering the file system.
/// </summary>
public static class FileUtility
{
    /// <summary>
    /// Default number of retry attempts for file operations.
    /// </summary>
    public const int DefaultRetryCount = 3;

    /// <summary>
    /// Default initial delay between retries in milliseconds.
    /// Each retry doubles this delay (exponential backoff).
    /// </summary>
    public const int DefaultRetryDelayMs = 100;

    /// <summary>
    /// Reads all text from a file with retry logic.
    /// </summary>
    /// <param name="path">File path to read</param>
    /// <param name="retryCount">Number of retry attempts (default: 3)</param>
    /// <param name="initialDelayMs">Initial delay between retries in ms (default: 100)</param>
    /// <returns>File contents as string</returns>
    /// <exception cref="IOException">Thrown if all retries fail</exception>
    public static string ReadAllText(string path, int retryCount = DefaultRetryCount, int initialDelayMs = DefaultRetryDelayMs)
    {
        return ExecuteWithRetry(
            () => File.ReadAllText(path),
            path,
            "read",
            retryCount,
            initialDelayMs
        );
    }

    /// <summary>
    /// Reads all text from a file with retry logic (async version).
    /// </summary>
    public static async Task<string> ReadAllTextAsync(string path, int retryCount = DefaultRetryCount, int initialDelayMs = DefaultRetryDelayMs, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(
            async () => await File.ReadAllTextAsync(path, cancellationToken),
            path,
            "read",
            retryCount,
            initialDelayMs,
            cancellationToken
        );
    }

    /// <summary>
    /// Reads all lines from a file with retry logic.
    /// </summary>
    public static string[] ReadAllLines(string path, int retryCount = DefaultRetryCount, int initialDelayMs = DefaultRetryDelayMs)
    {
        return ExecuteWithRetry(
            () => File.ReadAllLines(path),
            path,
            "read lines from",
            retryCount,
            initialDelayMs
        );
    }

    /// <summary>
    /// Reads all lines from a file with retry logic (async version).
    /// </summary>
    public static async Task<string[]> ReadAllLinesAsync(string path, int retryCount = DefaultRetryCount, int initialDelayMs = DefaultRetryDelayMs, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(
            async () => await File.ReadAllLinesAsync(path, cancellationToken),
            path,
            "read lines from",
            retryCount,
            initialDelayMs,
            cancellationToken
        );
    }

    /// <summary>
    /// Writes text to a file with retry logic.
    /// </summary>
    public static void WriteAllText(string path, string contents, int retryCount = DefaultRetryCount, int initialDelayMs = DefaultRetryDelayMs)
    {
        ExecuteWithRetry(
            () => { File.WriteAllText(path, contents); return true; },
            path,
            "write to",
            retryCount,
            initialDelayMs
        );
    }

    /// <summary>
    /// Writes text to a file with retry logic (async version).
    /// </summary>
    public static async Task WriteAllTextAsync(string path, string contents, int retryCount = DefaultRetryCount, int initialDelayMs = DefaultRetryDelayMs, CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(
            async () => { await File.WriteAllTextAsync(path, contents, cancellationToken); return true; },
            path,
            "write to",
            retryCount,
            initialDelayMs,
            cancellationToken
        );
    }

    /// <summary>
    /// Copies a file with retry logic.
    /// </summary>
    public static void Copy(string sourceFileName, string destFileName, bool overwrite = false, int retryCount = DefaultRetryCount, int initialDelayMs = DefaultRetryDelayMs)
    {
        ExecuteWithRetry(
            () => { File.Copy(sourceFileName, destFileName, overwrite); return true; },
            sourceFileName,
            "copy",
            retryCount,
            initialDelayMs
        );
    }

    /// <summary>
    /// Opens a file for reading with retry logic and shared read access.
    /// The caller is responsible for disposing the returned stream.
    /// </summary>
    public static FileStream OpenRead(string path, int retryCount = DefaultRetryCount, int initialDelayMs = DefaultRetryDelayMs)
    {
        return ExecuteWithRetry(
            () => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
            path,
            "open for reading",
            retryCount,
            initialDelayMs
        );
    }

    /// <summary>
    /// Opens a file for writing with retry logic.
    /// Creates the file if it doesn't exist, overwrites if it does.
    /// The caller is responsible for disposing the returned stream.
    /// </summary>
    public static FileStream OpenWrite(string path, int retryCount = DefaultRetryCount, int initialDelayMs = DefaultRetryDelayMs)
    {
        return ExecuteWithRetry(
            () => new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None),
            path,
            "open for writing",
            retryCount,
            initialDelayMs
        );
    }

    /// <summary>
    /// Deletes a file with retry logic.
    /// Does not throw if file doesn't exist.
    /// </summary>
    public static void Delete(string path, int retryCount = DefaultRetryCount, int initialDelayMs = DefaultRetryDelayMs)
    {
        if (!File.Exists(path))
            return;

        ExecuteWithRetry(
            () => { File.Delete(path); return true; },
            path,
            "delete",
            retryCount,
            initialDelayMs
        );
    }

    /// <summary>
    /// Checks if a file exists (no retry needed, but included for API consistency).
    /// </summary>
    public static bool Exists(string path) => File.Exists(path);

    #region Core Retry Logic

    /// <summary>
    /// Executes a file operation with exponential backoff retry logic.
    /// </summary>
    private static T ExecuteWithRetry<T>(Func<T> operation, string path, string operationName, int retryCount, int initialDelayMs)
    {
        Exception? lastException = null;
        int delayMs = initialDelayMs;

        for (int attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                return operation();
            }
            catch (IOException ex)
            {
                lastException = ex;
                
                if (attempt < retryCount)
                {
                    Logger.Warning($"File {operationName} failed (attempt {attempt}/{retryCount}): {path} - {ex.Message}. Retrying in {delayMs}ms...");
                    Thread.Sleep(delayMs);
                    delayMs *= 2; // Exponential backoff
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                lastException = ex;
                
                if (attempt < retryCount)
                {
                    Logger.Warning($"File {operationName} access denied (attempt {attempt}/{retryCount}): {path} - {ex.Message}. Retrying in {delayMs}ms...");
                    Thread.Sleep(delayMs);
                    delayMs *= 2;
                }
            }
        }

        // All retries exhausted
        Logger.Error($"File {operationName} failed after {retryCount} attempts: {path}", lastException!);
        throw lastException!;
    }

    /// <summary>
    /// Executes a file operation with exponential backoff retry logic (async version).
    /// </summary>
    private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string path, string operationName, int retryCount, int initialDelayMs, CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        int delayMs = initialDelayMs;

        for (int attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (IOException ex)
            {
                lastException = ex;
                
                if (attempt < retryCount)
                {
                    Logger.Warning($"File {operationName} failed (attempt {attempt}/{retryCount}): {path} - {ex.Message}. Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs *= 2; // Exponential backoff
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                lastException = ex;
                
                if (attempt < retryCount)
                {
                    Logger.Warning($"File {operationName} access denied (attempt {attempt}/{retryCount}): {path} - {ex.Message}. Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs *= 2;
                }
            }
        }

        // All retries exhausted
        Logger.Error($"File {operationName} failed after {retryCount} attempts: {path}", lastException!);
        throw lastException!;
    }

    #endregion
}
