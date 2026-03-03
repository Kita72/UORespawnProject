using FluentFTP;
using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Handles FTP connections and operations using FluentFTP.
/// Provides a clean interface for connecting, uploading, and downloading files.
/// </summary>
public class FtpConnectionService : IDisposable
{
    private readonly FtpCredentialService _credentialService;
    private AsyncFtpClient? _client;
    private bool _disposed;

    /// <summary>
    /// Event raised when connection status changes.
    /// </summary>
    public event EventHandler<FtpConnectionStatus>? ConnectionStatusChanged;

    /// <summary>
    /// Current connection status.
    /// </summary>
    public FtpConnectionStatus Status { get; private set; } = FtpConnectionStatus.Disconnected;

    /// <summary>
    /// Last error message, if any.
    /// </summary>
    public string? LastError { get; private set; }

    public FtpConnectionService(FtpCredentialService credentialService)
    {
        _credentialService = credentialService;
    }

    /// <summary>
    /// Tests the connection with the current credentials.
    /// Does not maintain the connection after testing.
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        var credentials = _credentialService.CurrentCredentials;
        if (credentials == null || !credentials.IsConfigured)
        {
            return (false, "No FTP credentials configured");
        }

        var errors = credentials.ValidationErrors;
        if (errors.Count > 0)
        {
            return (false, $"Invalid credentials: {string.Join(", ", errors)}");
        }

        try
        {
            SetStatus(FtpConnectionStatus.Connecting);

            using var testClient = CreateClient(credentials);
            await testClient.Connect();

            // Test that the remote path exists
            if (!await testClient.DirectoryExists(credentials.RemotePath))
            {
                await testClient.Disconnect();
                SetStatus(FtpConnectionStatus.Disconnected);
                return (false, $"Remote path does not exist: {credentials.RemotePath}");
            }

            await testClient.Disconnect();
            SetStatus(FtpConnectionStatus.Disconnected);

            // Record successful connection
            _credentialService.RecordSuccessfulConnection();

            return (true, $"Successfully connected to {credentials.Host}");
        }
        catch (Exception ex)
        {
            SetStatus(FtpConnectionStatus.Error);
            LastError = ex.Message;
            Logger.Error($"FTP connection test failed: {ex.Message}");
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Connects to the FTP server and maintains the connection.
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        var credentials = _credentialService.CurrentCredentials;
        if (credentials == null || !credentials.IsConfigured)
        {
            LastError = "No FTP credentials configured";
            return false;
        }

        try
        {
            // Disconnect existing connection if any
            await DisconnectAsync();

            SetStatus(FtpConnectionStatus.Connecting);
            _client = CreateClient(credentials);
            await _client.Connect();

            SetStatus(FtpConnectionStatus.Connected);
            _credentialService.RecordSuccessfulConnection();

            Logger.Info($"Connected to FTP server: {credentials.Host}");
            return true;
        }
        catch (Exception ex)
        {
            SetStatus(FtpConnectionStatus.Error);
            LastError = ex.Message;
            Logger.Error($"FTP connection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disconnects from the FTP server.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_client != null)
        {
            try
            {
                if (_client.IsConnected)
                {
                    await _client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error during FTP disconnect: {ex.Message}");
            }
            finally
            {
                _client.Dispose();
                _client = null;
                SetStatus(FtpConnectionStatus.Disconnected);
            }
        }
    }

    /// <summary>
    /// Uploads a file to the server.
    /// </summary>
    /// <param name="localPath">Local file path to upload</param>
    /// <param name="remotePath">Remote destination path</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    public async Task<bool> UploadFileAsync(string localPath, string remotePath, IProgress<FtpProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_client == null || !_client.IsConnected)
        {
            if (!await ConnectAsync())
                return false;
        }

        try
        {
            var status = await _client!.UploadFile(
                localPath,
                remotePath,
                FtpRemoteExists.Overwrite,
                createRemoteDir: true,
                progress: progress,
                token: cancellationToken);

            var success = status == FtpStatus.Success;
            if (success)
            {
                Logger.Info($"Uploaded: {Path.GetFileName(localPath)} -> {remotePath}");
            }
            else
            {
                Logger.Warning($"Upload failed for: {localPath}");
            }

            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Logger.Error($"Upload error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Downloads a file from the server.
    /// </summary>
    /// <param name="localPath">Local destination path</param>
    /// <param name="remotePath">Remote file path to download</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    public async Task<bool> DownloadFileAsync(string localPath, string remotePath, IProgress<FtpProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_client == null || !_client.IsConnected)
        {
            if (!await ConnectAsync())
                return false;
        }

        try
        {
            // Ensure local directory exists
            var localDir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(localDir) && !Directory.Exists(localDir))
            {
                Directory.CreateDirectory(localDir);
            }

            var status = await _client!.DownloadFile(
                localPath,
                remotePath,
                FtpLocalExists.Overwrite,
                progress: progress,
                token: cancellationToken);

            var success = status == FtpStatus.Success;
            if (success)
            {
                Logger.Info($"Downloaded: {remotePath} -> {Path.GetFileName(localPath)}");
            }
            else
            {
                Logger.Warning($"Download failed for: {remotePath}");
            }

            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Logger.Error($"Download error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a remote file exists.
    /// </summary>
    /// <param name="remotePath">Remote file path to check</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    public async Task<bool> FileExistsAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        if (_client == null || !_client.IsConnected)
        {
            if (!await ConnectAsync())
                return false;
        }

        try
        {
            return await _client!.FileExists(remotePath, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Warning($"Error checking file existence: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a remote directory exists.
    /// </summary>
    public async Task<bool> DirectoryExistsAsync(string remotePath)
    {
        if (_client == null || !_client.IsConnected)
        {
            if (!await ConnectAsync())
                return false;
        }

        try
        {
            return await _client!.DirectoryExists(remotePath);
        }
        catch (Exception ex)
        {
            Logger.Warning($"Error checking directory existence: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Lists files in a remote directory.
    /// </summary>
    public async Task<List<FtpListItem>> ListDirectoryAsync(string remotePath)
    {
        if (_client == null || !_client.IsConnected)
        {
            if (!await ConnectAsync())
                return [];
        }

        try
        {
            var items = await _client!.GetListing(remotePath);
            return [.. items];
        }
        catch (Exception ex)
        {
            Logger.Warning($"Error listing directory: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Lists only directories in a remote path.
    /// </summary>
    public async Task<List<string>> ListDirectoriesAsync(string remotePath)
    {
        var items = await ListDirectoryAsync(remotePath);
        return items
            .Where(i => i.Type == FtpObjectType.Directory)
            .Select(i => i.FullName)
            .ToList();
    }

    /// <summary>
    /// Creates the FTP client with configured settings.
    /// </summary>
    private AsyncFtpClient CreateClient(FtpCredentials credentials)
    {
        var client = new AsyncFtpClient(
            credentials.Host,
            credentials.Username,
            credentials.Password,
            credentials.Port);

        // Configure connection
        client.Config.ConnectTimeout = credentials.TimeoutSeconds * 1000;
        client.Config.DataConnectionConnectTimeout = credentials.TimeoutSeconds * 1000;
        client.Config.DataConnectionReadTimeout = credentials.TimeoutSeconds * 1000;

        // Configure passive mode
        client.Config.DataConnectionType = credentials.UsePassiveMode
            ? FtpDataConnectionType.AutoPassive
            : FtpDataConnectionType.AutoActive;

        // Configure encryption
        if (credentials.UseTls)
        {
            client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
            // Accept any certificate for now (user should use trusted hosts)
            client.ValidateCertificate += (control, e) => { e.Accept = true; };
        }
        else
        {
            client.Config.EncryptionMode = FtpEncryptionMode.None;
        }

        return client;
    }

    private void SetStatus(FtpConnectionStatus status)
    {
        if (Status != status)
        {
            Status = status;
            ConnectionStatusChanged?.Invoke(this, status);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _client?.Dispose();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// FTP connection status states.
/// </summary>
public enum FtpConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Transferring,
    Error
}
