using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Manages user accounts in the UORespawn app.
/// 
/// FILE-BASED SECURITY MODEL:
/// - We only store FOLDER PATHS in app preferences (where to look)
/// - Account details (name, dates) live in uor_account.json in user's folder
/// - If user deletes their folder, account disappears - no leftover data in app
/// - This is SRP security: the file IS the account
/// 
/// No passwords needed for app accounts - just friendly names.
/// FTP/AI credentials are separate files the user controls.
/// </summary>
public class AccountService
{
    private const string FolderPathsKey = "AccountFolderPaths";
    private const string ActiveFolderKey = "ActiveAccountFolder";
    private const string PathSeparator = "|||";

    private List<UserAccount> _accounts = [];
    private UserAccount? _activeAccount;

    /// <summary>
    /// Event raised when the active account changes.
    /// </summary>
    public event EventHandler<UserAccount?>? ActiveAccountChanged;

    /// <summary>
    /// Event raised when the accounts list changes (add/remove).
    /// </summary>
    public event EventHandler? AccountsChanged;

    /// <summary>
    /// The currently active account, or null if none.
    /// </summary>
    public UserAccount? ActiveAccount
    {
        get => _activeAccount;
        private set
        {
            var oldPath = _activeAccount?.CredentialFolderPath;
            var newPath = value?.CredentialFolderPath;

            if (oldPath != newPath)
            {
                _activeAccount = value;
                if (value != null)
                {
                    value.LastUsedAt = DateTime.Now;
                    SaveAccountTimestampInBackground(value);
                    Preferences.Set(ActiveFolderKey, value.CredentialFolderPath);
                }
                else
                {
                    Preferences.Remove(ActiveFolderKey);
                }
                ActiveAccountChanged?.Invoke(this, value);
            }
        }
    }

    private static void SaveAccountTimestampInBackground(UserAccount account)
    {
        _ = Task.Run(() =>
        {
            try { account.SaveToFolder(); }
            catch (Exception ex) { Logger.Error("Failed to save account timestamp", ex); }
        });
    }

    /// <summary>
    /// All registered accounts (validated on load).
    /// </summary>
    public IReadOnlyList<UserAccount> Accounts => _accounts.AsReadOnly();

    /// <summary>
    /// Whether any valid accounts exist.
    /// </summary>
    public bool HasAccounts => _accounts.Count > 0;

    /// <summary>
    /// Whether an account is currently active.
    /// </summary>
    public bool HasActiveAccount => _activeAccount != null;

    public AccountService()
    {
        LoadAccounts();
    }

    /// <summary>
    /// Creates a new account with the given name and credential folder.
    /// The account data is stored in the user's folder, not in app preferences.
    /// </summary>
    public UserAccount? CreateAccount(string name, string credentialFolderPath)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Logger.Warning("Cannot create account: name is empty");
            return null;
        }

        name = name.Trim();

        if (string.IsNullOrWhiteSpace(credentialFolderPath))
        {
            Logger.Warning("Cannot create account: credential folder path is empty");
            return null;
        }

        // Check if folder already has an account
        if (UserAccount.IsValidAccountFolder(credentialFolderPath))
        {
            Logger.Warning($"Cannot create account: folder already contains an account");
            return null;
        }

        // Check for duplicate folder paths
        if (_accounts.Any(a => a.CredentialFolderPath.Equals(credentialFolderPath, StringComparison.OrdinalIgnoreCase)))
        {
            Logger.Warning($"Cannot create account: folder already registered");
            return null;
        }

        // Create the folder if it doesn't exist
        try
        {
            if (!Directory.Exists(credentialFolderPath))
            {
                Directory.CreateDirectory(credentialFolderPath);
                Logger.Info($"Created credential folder: {credentialFolderPath}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Cannot create credential folder: {ex.Message}");
            return null;
        }

        // Create account and save to user's folder
        var account = new UserAccount
        {
            Name = name,
            CredentialFolderPath = credentialFolderPath,
            CreatedAt = DateTime.Now,
            LastUsedAt = DateTime.Now
        };

        if (!account.SaveToFolder())
        {
            Logger.Error("Failed to save account file to folder");
            return null;
        }

        _accounts.Add(account);
        SaveFolderPaths();
        AccountsChanged?.Invoke(this, EventArgs.Empty);

        Logger.Info($"Created account '{name}' in folder: {credentialFolderPath}");
        return account;
    }

    /// <summary>
    /// Removes an account from the app's tracked list.
    /// The user's folder and files are NOT deleted - user controls that.
    /// </summary>
    public bool RemoveAccount(string folderPath)
    {
        var account = _accounts.FirstOrDefault(a => 
            a.CredentialFolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase));

        if (account == null)
        {
            Logger.Warning($"Cannot remove account: folder not found in list");
            return false;
        }

        // If removing the active account, clear it
        if (_activeAccount?.CredentialFolderPath == account.CredentialFolderPath)
        {
            ActiveAccount = null;
        }

        _accounts.Remove(account);
        SaveFolderPaths();
        AccountsChanged?.Invoke(this, EventArgs.Empty);

        Logger.Info($"Removed account '{account.Name}' (folder left intact: {account.CredentialFolderPath})");
        return true;
    }

    /// <summary>
    /// Sets the active account by folder path.
    /// </summary>
    public bool SetActiveAccount(string folderPath)
    {
        var account = _accounts.FirstOrDefault(a => 
            a.CredentialFolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase));

        if (account == null)
        {
            Logger.Warning($"Cannot set active account: folder not found");
            return false;
        }

        ActiveAccount = account;
        Logger.Info($"Active account set to '{account.Name}'");
        return true;
    }

    /// <summary>
    /// Sets the active account by name (convenience method).
    /// </summary>
    public bool SetActiveAccountByName(string name)
    {
        var account = _accounts.FirstOrDefault(a => 
            a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (account == null)
        {
            Logger.Warning($"Cannot set active account: '{name}' not found");
            return false;
        }

        ActiveAccount = account;
        return true;
    }

    /// <summary>
    /// Clears the active account.
    /// </summary>
    public void ClearActiveAccount()
    {
        ActiveAccount = null;
        Logger.Info("Active account cleared");
    }

    /// <summary>
    /// Gets an account by name.
    /// </summary>
    public UserAccount? GetAccount(string name)
    {
        return _accounts.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets an account by folder path.
    /// </summary>
    public UserAccount? GetAccountByFolder(string folderPath)
    {
        return _accounts.FirstOrDefault(a => 
            a.CredentialFolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Refreshes the account list by re-validating all folders.
    /// Removes accounts whose folders/files no longer exist.
    /// </summary>
    public void RefreshAccounts()
    {
        LoadAccounts();
        AccountsChanged?.Invoke(this, EventArgs.Empty);
    }

    // ==================== FILE-BASED PERSISTENCE ====================

    private void LoadAccounts()
    {
        try
        {
            _accounts = [];

            // Get stored folder paths
            var pathsString = Preferences.Get(FolderPathsKey, "");
            if (string.IsNullOrEmpty(pathsString))
            {
                Logger.Info("No account folders registered");
                return;
            }

            var folderPaths = pathsString.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var validPaths = new List<string>();

            foreach (var folderPath in folderPaths)
            {
                // Try to load account from folder
                var account = UserAccount.LoadFromFolder(folderPath);
                if (account != null)
                {
                    _accounts.Add(account);
                    validPaths.Add(folderPath);
                    Logger.Info($"Loaded account '{account.Name}' from: {folderPath}");
                }
                else
                {
                    // Folder or file was deleted by user - skip it
                    Logger.Info($"Account folder no longer valid, removing: {folderPath}");
                }
            }

            // Update stored paths to only valid ones
            if (validPaths.Count != folderPaths.Length)
            {
                Preferences.Set(FolderPathsKey, string.Join(PathSeparator, validPaths));
            }

            // Sort by last used
            _accounts = [.. _accounts.OrderByDescending(a => a.LastUsedAt)];

            // Restore active account
            var activeFolderPath = Preferences.Get(ActiveFolderKey, "");
            if (!string.IsNullOrEmpty(activeFolderPath))
            {
                _activeAccount = _accounts.FirstOrDefault(a => 
                    a.CredentialFolderPath.Equals(activeFolderPath, StringComparison.OrdinalIgnoreCase));

                if (_activeAccount == null)
                {
                    // Active account folder was deleted
                    Preferences.Remove(ActiveFolderKey);
                }
            }

            Logger.Info($"Loaded {_accounts.Count} account(s), active: {_activeAccount?.Name ?? "none"}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load accounts: {ex.Message}");
            _accounts = [];
        }
    }

    private void SaveFolderPaths()
    {
        try
        {
            var paths = _accounts.Select(a => a.CredentialFolderPath);
            Preferences.Set(FolderPathsKey, string.Join(PathSeparator, paths));
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save account folder paths: {ex.Message}");
        }
    }
}
