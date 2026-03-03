using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Manages FTP credentials stored in user-controlled folders.
/// This service reads/writes credential files to the folder specified by the user's account.
/// The app never stores credentials directly - only folder paths are stored.
/// </summary>
public class FtpCredentialService
{
    private readonly AccountService _accountService;
    private FtpCredentials? _currentCredentials;

    /// <summary>
    /// Event raised when credentials are loaded or changed.
    /// </summary>
    public event EventHandler<FtpCredentials?>? CredentialsChanged;

    /// <summary>
    /// The currently loaded credentials, or null if none.
    /// </summary>
    public FtpCredentials? CurrentCredentials => _currentCredentials;

    /// <summary>
    /// Whether credentials are currently loaded and configured.
    /// </summary>
    public bool HasConfiguredCredentials => _currentCredentials?.IsConfigured ?? false;

    public FtpCredentialService(AccountService accountService)
    {
        _accountService = accountService;

        // Listen for account changes to load/unload credentials
        _accountService.ActiveAccountChanged += OnActiveAccountChanged;

        // Load credentials if an account is already active
        if (_accountService.ActiveAccount != null)
        {
            LoadCredentials();
        }
    }

    private void OnActiveAccountChanged(object? sender, UserAccount? account)
    {
        if (account != null)
        {
            LoadCredentials();
        }
        else
        {
            _currentCredentials = null;
            CredentialsChanged?.Invoke(this, null);
        }
    }

    /// <summary>
    /// Loads credentials from the active account's credential folder.
    /// </summary>
    public FtpCredentials? LoadCredentials()
    {
        var account = _accountService.ActiveAccount;
        if (account == null)
        {
            Logger.Warning("Cannot load credentials: no active account");
            _currentCredentials = null;
            CredentialsChanged?.Invoke(this, null);
            return null;
        }

        if (!account.HasCredentialFolder)
        {
            Logger.Warning($"Credential folder does not exist: {account.CredentialFolderPath}");
            _currentCredentials = null;
            CredentialsChanged?.Invoke(this, null);
            return null;
        }

        _currentCredentials = FtpCredentials.LoadFromFile(account.CredentialFilePath);

        if (_currentCredentials != null)
        {
            Logger.Info($"Loaded FTP credentials for account '{account.Name}'");
        }
        else
        {
            Logger.Info($"No credentials file found for account '{account.Name}' (will create on save)");
        }

        CredentialsChanged?.Invoke(this, _currentCredentials);
        return _currentCredentials;
    }

    /// <summary>
    /// Saves credentials to the active account's credential folder.
    /// </summary>
    public bool SaveCredentials(FtpCredentials credentials)
    {
        var account = _accountService.ActiveAccount;
        if (account == null)
        {
            Logger.Error("Cannot save credentials: no active account");
            return false;
        }

        // Ensure the folder exists
        if (!account.HasCredentialFolder)
        {
            try
            {
                Directory.CreateDirectory(account.CredentialFolderPath);
            }
            catch (Exception ex)
            {
                Logger.Error($"Cannot create credential folder: {ex.Message}");
                return false;
            }
        }

        var success = credentials.SaveToFile(account.CredentialFilePath);

        if (success)
        {
            _currentCredentials = credentials;
            CredentialsChanged?.Invoke(this, _currentCredentials);
            Logger.Info($"Saved FTP credentials for account '{account.Name}'");
        }

        return success;
    }

    /// <summary>
    /// Creates a new credentials object (not yet saved).
    /// Call SaveCredentials() to persist.
    /// </summary>
    public FtpCredentials CreateNewCredentials()
    {
        return new FtpCredentials
        {
            Port = 21,
            UseTls = true,
            UsePassiveMode = true,
            TimeoutSeconds = 30
        };
    }

    /// <summary>
    /// Deletes credentials from the active account's folder.
    /// </summary>
    public bool DeleteCredentials()
    {
        var account = _accountService.ActiveAccount;
        if (account == null)
        {
            Logger.Warning("Cannot delete credentials: no active account");
            return false;
        }

        try
        {
            if (File.Exists(account.CredentialFilePath))
            {
                File.Delete(account.CredentialFilePath);
                Logger.Info($"Deleted credentials file for account '{account.Name}'");
            }

            _currentCredentials = null;
            CredentialsChanged?.Invoke(this, null);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to delete credentials: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates the last connected timestamp and saves.
    /// </summary>
    public void RecordSuccessfulConnection()
    {
        if (_currentCredentials != null)
        {
            _currentCredentials.LastConnected = DateTime.Now;
            SaveCredentials(_currentCredentials);
        }
    }

    /// <summary>
    /// Gets the credential file path for the active account.
    /// </summary>
    public string? GetCredentialFilePath()
    {
        return _accountService.ActiveAccount?.CredentialFilePath;
    }

    /// <summary>
    /// Checks if the credential file exists for the active account.
    /// </summary>
    public bool CredentialFileExists()
    {
        var path = GetCredentialFilePath();
        return !string.IsNullOrEmpty(path) && File.Exists(path);
    }
}
