using FluentFTP;
using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Handles synchronization of spawn data between the editor and a remote ServUO server via FTP.
/// Provides a third option alongside "Link Local" (direct folder access) and "Manual" (user transfer).
/// 
/// Sync Direction:
/// - Upload (Editor → Server): Binary spawn files (.bin) and settings (.csv) to INPUT/
/// - Download (Server → Editor): Reference files (.txt) from OUTPUT/
/// </summary>
public class FtpSyncService
{
    private readonly FtpConnectionService _ftpConnection;
    private readonly FtpCredentialService _credentialService;

    /// <summary>
    /// Event raised when sync operation starts.
    /// </summary>
    public event EventHandler<SyncOperation>? SyncStarted;

    /// <summary>
    /// Event raised during sync progress.
    /// </summary>
    public event EventHandler<SyncProgressEventArgs>? SyncProgress;

    /// <summary>
    /// Event raised when sync operation completes.
    /// </summary>
    public event EventHandler<SyncResultEventArgs>? SyncCompleted;

    public FtpSyncService(FtpConnectionService ftpConnection, FtpCredentialService credentialService)
    {
        _ftpConnection = ftpConnection;
        _credentialService = credentialService;
    }

    /// <summary>
    /// Checks if FTP sync is available (credentials configured).
    /// </summary>
    public bool IsSyncAvailable => _credentialService.HasConfiguredCredentials;

    /// <summary>
    /// Gets the remote path from current credentials.
    /// </summary>
    private string? RemotePath => _credentialService.CurrentCredentials?.RemotePath;

    /// <summary>
    /// Uploads spawn data files to the remote server.
    /// Files uploaded: UOR_BoxSpawn.bin, UOR_TileSpawn.bin, UOR_RegionSpawn.bin, UOR_VendorSpawn.bin, UOR_SpawnSettings.csv
    /// </summary>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    public async Task<SyncResult> UploadSpawnDataAsync(IProgress<SyncProgressEventArgs>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!IsSyncAvailable || string.IsNullOrEmpty(RemotePath))
        {
            return new SyncResult(false, "FTP not configured");
        }

        SyncStarted?.Invoke(this, SyncOperation.Upload);
        var result = new SyncResult(true, "Upload started");
        var uploadedFiles = new List<string>();
        var failedFiles = new List<string>();

        try
        {
            // Connect
            ReportProgress(progress, "Connecting...", 0, 0);
            if (!await _ftpConnection.ConnectAsync())
            {
                return new SyncResult(false, $"Connection failed: {_ftpConnection.LastError}");
            }

            // Files to upload (from local UOR_DATA to server INPUT)
            var filesToUpload = new List<(string LocalPath, string RemotePath, string DisplayName)>
            {
                (GetLocalFilePath(PathConstants.BOX_FILENAME), GetRemoteInputPath(PathConstants.BOX_FILENAME), "Box Spawns"),
                (GetLocalFilePath(PathConstants.TILE_FILENAME), GetRemoteInputPath(PathConstants.TILE_FILENAME), "Tile Spawns"),
                (GetLocalFilePath(PathConstants.REGION_FILENAME), GetRemoteInputPath(PathConstants.REGION_FILENAME), "Region Spawns"),
                (GetLocalFilePath(PathConstants.VENDOR_FILENAME), GetRemoteInputPath(PathConstants.VENDOR_FILENAME), "Vendor Spawns"),
                (GetLocalFilePath(PathConstants.SETTINGS_FILENAME), GetRemoteInputPath(PathConstants.SETTINGS_FILENAME), "Settings")
            };

            int current = 0;
            int total = filesToUpload.Count;

            foreach (var (localPath, remotePath, displayName) in filesToUpload)
            {
                // Check for cancellation before each file
                if (cancellationToken.IsCancellationRequested)
                {
                    await _ftpConnection.DisconnectAsync();
                    return new SyncResult(false, "Upload cancelled by user")
                    {
                        FilesTransferred = uploadedFiles.Count,
                        FileNames = uploadedFiles
                    };
                }

                current++;
                ReportProgress(progress, $"Uploading {displayName}...", current, total);

                if (!File.Exists(localPath))
                {
                    Logger.Warning($"Skipping upload - file not found: {localPath}");
                    continue; // Skip files that don't exist
                }

                var fileProgress = new Progress<FtpProgress>(p =>
                {
                    var percent = (int)p.Progress;
                    ReportProgress(progress, $"Uploading {displayName}... {percent}%", current, total, percent);
                });

                var success = await _ftpConnection.UploadFileAsync(localPath, remotePath, fileProgress, cancellationToken);
                if (success)
                {
                    uploadedFiles.Add(displayName);
                }
                else
                {
                    failedFiles.Add(displayName);
                }
            }

            await _ftpConnection.DisconnectAsync();

            // Build result message
            if (failedFiles.Count == 0)
            {
                result = new SyncResult(true, $"Uploaded {uploadedFiles.Count} files successfully")
                {
                    FilesTransferred = uploadedFiles.Count,
                    FileNames = uploadedFiles
                };
            }
            else
            {
                result = new SyncResult(false, $"Upload completed with errors. Failed: {string.Join(", ", failedFiles)}")
                {
                    FilesTransferred = uploadedFiles.Count,
                    FilesFailed = failedFiles.Count,
                    FileNames = uploadedFiles
                };
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Info("FTP upload cancelled by user");
            result = new SyncResult(false, "Upload cancelled")
            {
                FilesTransferred = uploadedFiles.Count,
                FileNames = uploadedFiles
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"FTP upload failed: {ex.Message}");
            result = new SyncResult(false, $"Upload failed: {ex.Message}");
        }
        finally
        {
            SyncCompleted?.Invoke(this, new SyncResultEventArgs(SyncOperation.Upload, result));
        }

        return result;
    }

    /// <summary>
    /// Downloads reference data files from the remote server.
    /// Files downloaded: UOR_BestiaryList.txt, UOR_RegionList.txt, UOR_TileList.txt, etc.
    /// </summary>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    public async Task<SyncResult> DownloadReferenceDataAsync(IProgress<SyncProgressEventArgs>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!IsSyncAvailable || string.IsNullOrEmpty(RemotePath))
        {
            return new SyncResult(false, "FTP not configured");
        }

        SyncStarted?.Invoke(this, SyncOperation.Download);
        var result = new SyncResult(true, "Download started");
        var downloadedFiles = new List<string>();
        var failedFiles = new List<string>();

        try
        {
            // Connect
            ReportProgress(progress, "Connecting...", 0, 0);
            if (!await _ftpConnection.ConnectAsync())
            {
                return new SyncResult(false, $"Connection failed: {_ftpConnection.LastError}");
            }

            // Files to download (from server OUTPUT to local UOR_DATA)
            var filesToDownload = new List<(string LocalPath, string RemotePath, string DisplayName)>
            {
                (GetLocalFilePath(PathConstants.BESTIARY_FILENAME), GetRemoteOutputPath(PathConstants.BESTIARY_FILENAME), "Bestiary"),
                (GetLocalFilePath(PathConstants.REGION_LIST_FILENAME), GetRemoteOutputPath(PathConstants.REGION_LIST_FILENAME), "Regions"),
                (GetLocalFilePath(PathConstants.TILE_LIST_FILENAME), GetRemoteOutputPath(PathConstants.TILE_LIST_FILENAME), "Tiles"),
                (GetLocalFilePath(PathConstants.MAP_LIST_FILENAME), GetRemoteOutputPath(PathConstants.MAP_LIST_FILENAME), "Maps"),
                (GetLocalFilePath(PathConstants.VENDOR_LIST_FILENAME), GetRemoteOutputPath(PathConstants.VENDOR_LIST_FILENAME), "Vendors"),
                (GetLocalFilePath(PathConstants.SIGN_DATA_FILENAME), GetRemoteOutputPath(PathConstants.SIGN_DATA_FILENAME), "Signs"),
                (GetLocalFilePath(PathConstants.HIVE_DATA_FILENAME), GetRemoteOutputPath(PathConstants.HIVE_DATA_FILENAME), "Hives"),
                (GetLocalFilePath(PathConstants.SPAWNER_LIST_FILENAME), GetRemoteOutputPath(PathConstants.SPAWNER_LIST_FILENAME), "Spawners")
            };

            int current = 0;
            int total = filesToDownload.Count;

            foreach (var (localPath, remotePath, displayName) in filesToDownload)
            {
                // Check for cancellation before each file
                if (cancellationToken.IsCancellationRequested)
                {
                    await _ftpConnection.DisconnectAsync();
                    return new SyncResult(false, "Download cancelled by user")
                    {
                        FilesTransferred = downloadedFiles.Count,
                        FileNames = downloadedFiles
                    };
                }

                current++;
                ReportProgress(progress, $"Downloading {displayName}...", current, total);

                // Check if remote file exists first
                if (!await _ftpConnection.FileExistsAsync(remotePath, cancellationToken))
                {
                    Logger.Info($"Skipping download - remote file not found: {remotePath}");
                    continue;
                }

                var fileProgress = new Progress<FtpProgress>(p =>
                {
                    var percent = (int)p.Progress;
                    ReportProgress(progress, $"Downloading {displayName}... {percent}%", current, total, percent);
                });

                var success = await _ftpConnection.DownloadFileAsync(localPath, remotePath, fileProgress, cancellationToken);
                if (success)
                {
                    downloadedFiles.Add(displayName);
                }
                else
                {
                    failedFiles.Add(displayName);
                }
            }

            await _ftpConnection.DisconnectAsync();

            // Build result message
            if (downloadedFiles.Count == 0 && failedFiles.Count == 0)
            {
                result = new SyncResult(true, "No reference files found on server. Has the server run at least once?");
            }
            else if (failedFiles.Count == 0)
            {
                result = new SyncResult(true, $"Downloaded {downloadedFiles.Count} files successfully")
                {
                    FilesTransferred = downloadedFiles.Count,
                    FileNames = downloadedFiles
                };
            }
            else
            {
                result = new SyncResult(false, $"Download completed with errors. Failed: {string.Join(", ", failedFiles)}")
                {
                    FilesTransferred = downloadedFiles.Count,
                    FilesFailed = failedFiles.Count,
                    FileNames = downloadedFiles
                };
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Info("FTP download cancelled by user");
            result = new SyncResult(false, "Download cancelled")
            {
                FilesTransferred = downloadedFiles.Count,
                FileNames = downloadedFiles
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"FTP download failed: {ex.Message}");
            result = new SyncResult(false, $"Download failed: {ex.Message}");
        }
        finally
        {
            SyncCompleted?.Invoke(this, new SyncResultEventArgs(SyncOperation.Download, result));
        }

        return result;
    }

    /// <summary>
    /// Performs a full sync: downloads reference data, then uploads spawn data.
    /// </summary>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    public async Task<SyncResult> FullSyncAsync(IProgress<SyncProgressEventArgs>? progress = null, CancellationToken cancellationToken = default)
    {
        // First download reference data (bestiary, regions, etc.)
        var downloadResult = await DownloadReferenceDataAsync(progress, cancellationToken);

        // Check if cancelled or failed before continuing
        if (cancellationToken.IsCancellationRequested || !downloadResult.Success)
        {
            return downloadResult;
        }

        // Then upload spawn data
        var uploadResult = await UploadSpawnDataAsync(progress, cancellationToken);

        // Combine results
        var totalTransferred = downloadResult.FilesTransferred + uploadResult.FilesTransferred;
        var totalFailed = downloadResult.FilesFailed + uploadResult.FilesFailed;

        if (totalFailed == 0)
        {
            return new SyncResult(true, $"Full sync complete: {totalTransferred} files transferred")
            {
                FilesTransferred = totalTransferred
            };
        }
        else
        {
            var message = $"Sync completed with errors: {totalTransferred} succeeded, {totalFailed} failed";
            return new SyncResult(false, message)
            {
                FilesTransferred = totalTransferred,
                FilesFailed = totalFailed
            };
        }
    }

    // ==================== HELPER METHODS ====================

    private string GetLocalFilePath(string filename)
    {
        return Path.Combine(PathConstants.LocalDataPath, filename);
    }

    private string GetRemoteInputPath(string filename)
    {
        // RemotePath points to UORespawn folder, INPUT is subfolder
        return $"{RemotePath}/{PathConstants.UOR_INPUT_SUBFOLDER}/{filename}";
    }

    private string GetRemoteOutputPath(string filename)
    {
        // RemotePath points to UORespawn folder, OUTPUT is subfolder
        return $"{RemotePath}/{PathConstants.UOR_OUTPUT_SUBFOLDER}/{filename}";
    }

    private void ReportProgress(IProgress<SyncProgressEventArgs>? progress, string message, int current, int total, int filePercent = 0)
    {
        var args = new SyncProgressEventArgs(message, current, total, filePercent);
        progress?.Report(args);
        SyncProgress?.Invoke(this, args);
    }
}

/// <summary>
/// Types of sync operations.
/// </summary>
public enum SyncOperation
{
    Upload,
    Download,
    FullSync
}

/// <summary>
/// Result of a sync operation.
/// </summary>
public class SyncResult
{
    public bool Success { get; }
    public string Message { get; }
    public int FilesTransferred { get; init; }
    public int FilesFailed { get; init; }
    public List<string> FileNames { get; init; } = [];

    public SyncResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}

/// <summary>
/// Progress information for sync operations.
/// </summary>
public class SyncProgressEventArgs : EventArgs
{
    public string Message { get; }
    public int CurrentFile { get; }
    public int TotalFiles { get; }
    public int FileProgress { get; }
    public int OverallProgress => TotalFiles > 0 ? (CurrentFile * 100) / TotalFiles : 0;

    public SyncProgressEventArgs(string message, int current, int total, int fileProgress = 0)
    {
        Message = message;
        CurrentFile = current;
        TotalFiles = total;
        FileProgress = fileProgress;
    }
}

/// <summary>
/// Event args for sync completion.
/// </summary>
public class SyncResultEventArgs : EventArgs
{
    public SyncOperation Operation { get; }
    public SyncResult Result { get; }

    public SyncResultEventArgs(SyncOperation operation, SyncResult result)
    {
        Operation = operation;
        Result = result;
    }
}
