using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Manages user accounts in the UORespawn app.
/// Accounts are lightweight profiles that link a name to a credential folder.
/// The actual credentials are stored in the user's chosen folder, not here.
/// </summary>
public class AccountService
{
    private const string AccountsKey = "UserAccounts";
    private const string ActiveAccountKey = "ActiveAccountName";
    private const string AccountSeparator = "|||";

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
            if (_activeAccount?.Name != value?.Name)
            {
                _activeAccount = value;
                if (value != null)
                {
                    value.LastUsedAt = DateTime.Now;
                    SaveAccounts();
                    Preferences.Set(ActiveAccountKey, value.Name);
                }
                else
                {
                    Preferences.Remove(ActiveAccountKey);
                }
                ActiveAccountChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// All registered accounts.
    /// </summary>
    public IReadOnlyList<UserAccount> Accounts => _accounts.AsReadOnly();

    /// <summary>
    /// Whether any accounts exist.
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
    /// </summary>
    /// <returns>The created account, or null if validation fails.</returns>
    public UserAccount? CreateAccount(string name, string credentialFolderPath)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            Logger.Warning("Cannot create account: name is empty");
            return null;
        }

        name = name.Trim();

        // Check for duplicate names (case-insensitive)
        if (_accounts.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            Logger.Warning($"Cannot create account: '{name}' already exists");
            return null;
        }

        // Validate folder path
        if (string.IsNullOrWhiteSpace(credentialFolderPath))
        {
            Logger.Warning("Cannot create account: credential folder path is empty");
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

        var account = new UserAccount
        {
            Name = name,
            CredentialFolderPath = credentialFolderPath,
            CreatedAt = DateTime.Now,
            LastUsedAt = DateTime.Now
        };

        _accounts.Add(account);
        SaveAccounts();
        AccountsChanged?.Invoke(this, EventArgs.Empty);

        Logger.Info($"Created account '{name}' with credential folder: {credentialFolderPath}");
        return account;
    }

    /// <summary>
    /// Removes an account from the app.
    /// Note: This does NOT delete the credential folder - user manages that.
    /// </summary>
    public bool RemoveAccount(string name)
    {
        var account = _accounts.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (account == null)
        {
            Logger.Warning($"Cannot remove account: '{name}' not found");
            return false;
        }

        // If removing the active account, clear it
        if (_activeAccount?.Name == account.Name)
        {
            ActiveAccount = null;
        }

        _accounts.Remove(account);
        SaveAccounts();
        AccountsChanged?.Invoke(this, EventArgs.Empty);

        Logger.Info($"Removed account '{name}' (credential folder left intact at: {account.CredentialFolderPath})");
        return true;
    }

    /// <summary>
    /// Sets the active account by name.
    /// </summary>
    public bool SetActiveAccount(string name)
    {
        var account = _accounts.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (account == null)
        {
            Logger.Warning($"Cannot set active account: '{name}' not found");
            return false;
        }

        ActiveAccount = account;
        Logger.Info($"Active account set to '{name}'");
        return true;
    }

    /// <summary>
    /// Clears the active account (logs out).
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
    /// Updates an account's credential folder path.
    /// </summary>
    public bool UpdateCredentialFolder(string name, string newFolderPath)
    {
        var account = GetAccount(name);
        if (account == null)
        {
            Logger.Warning($"Cannot update account: '{name}' not found");
            return false;
        }

        // Create folder if needed
        try
        {
            if (!Directory.Exists(newFolderPath))
            {
                Directory.CreateDirectory(newFolderPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Cannot create credential folder: {ex.Message}");
            return false;
        }

        account.CredentialFolderPath = newFolderPath;
        SaveAccounts();

        Logger.Info($"Updated credential folder for '{name}': {newFolderPath}");
        return true;
    }

    /// <summary>
    /// Renames an account.
    /// </summary>
    public bool RenameAccount(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            Logger.Warning("Cannot rename account: new name is empty");
            return false;
        }

        newName = newName.Trim();

        var account = GetAccount(oldName);
        if (account == null)
        {
            Logger.Warning($"Cannot rename account: '{oldName}' not found");
            return false;
        }

        // Check for duplicate
        if (_accounts.Any(a => a.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) && a != account))
        {
            Logger.Warning($"Cannot rename account: '{newName}' already exists");
            return false;
        }

        var wasActive = _activeAccount?.Name == account.Name;
        account.Name = newName;
        SaveAccounts();

        if (wasActive)
        {
            Preferences.Set(ActiveAccountKey, newName);
        }

        AccountsChanged?.Invoke(this, EventArgs.Empty);
        Logger.Info($"Renamed account '{oldName}' to '{newName}'");
        return true;
    }

    // ==================== PERSISTENCE ====================

    private void LoadAccounts()
    {
        try
        {
            var serialized = Preferences.Get(AccountsKey, "");
            if (string.IsNullOrEmpty(serialized))
            {
                _accounts = [];
                return;
            }

            var accountStrings = serialized.Split(AccountSeparator, StringSplitOptions.RemoveEmptyEntries);
            _accounts = accountStrings
                .Select(UserAccount.Deserialize)
                .Where(a => a != null)
                .Cast<UserAccount>()
                .OrderByDescending(a => a.LastUsedAt)
                .ToList();

            // Restore active account
            var activeAccountName = Preferences.Get(ActiveAccountKey, "");
            if (!string.IsNullOrEmpty(activeAccountName))
            {
                _activeAccount = _accounts.FirstOrDefault(a => a.Name.Equals(activeAccountName, StringComparison.OrdinalIgnoreCase));
            }

            Logger.Info($"Loaded {_accounts.Count} account(s), active: {_activeAccount?.Name ?? "none"}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load accounts: {ex.Message}");
            _accounts = [];
        }
    }

    private void SaveAccounts()
    {
        try
        {
            var serialized = string.Join(AccountSeparator, _accounts.Select(a => a.Serialize()));
            Preferences.Set(AccountsKey, serialized);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save accounts: {ex.Message}");
        }
    }
}
