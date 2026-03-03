namespace UORespawnApp.Scripts.Entities;

/// <summary>
/// Represents a user account in the UORespawn app.
/// An account is simply a profile name linked to a folder where credentials are stored.
/// The app only stores the account name and folder path - actual credentials live in the user's folder.
/// This design gives users full control over their sensitive data.
/// </summary>
public class UserAccount
{
    /// <summary>
    /// Display name for this account (e.g., "Wilson", "TestServer", "Wife").
    /// Used to identify the account in the UI.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the folder where this account's credentials are stored.
    /// The user chooses this folder - it could be on an encrypted drive, USB, cloud folder, etc.
    /// The app reads/writes credential files to this location.
    /// </summary>
    public string CredentialFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// When this account was created (for sorting/display purposes).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// When this account was last used (for sorting/display purposes).
    /// Updated when the account is selected as active.
    /// </summary>
    public DateTime LastUsedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Checks if this account has a valid credential folder configured.
    /// </summary>
    public bool HasCredentialFolder => !string.IsNullOrEmpty(CredentialFolderPath) && Directory.Exists(CredentialFolderPath);

    /// <summary>
    /// The expected path to the credentials file within this account's folder.
    /// </summary>
    public string CredentialFilePath => string.IsNullOrEmpty(CredentialFolderPath)
        ? string.Empty
        : Path.Combine(CredentialFolderPath, "uor_credentials.json");

    /// <summary>
    /// Checks if credentials exist for this account.
    /// </summary>
    public bool HasCredentials => !string.IsNullOrEmpty(CredentialFilePath) && File.Exists(CredentialFilePath);

    /// <summary>
    /// Creates a serializable representation for storage in Preferences.
    /// Format: Name|CredentialFolderPath|CreatedAt|LastUsedAt
    /// </summary>
    public string Serialize()
    {
        return $"{Name}|{CredentialFolderPath}|{CreatedAt:O}|{LastUsedAt:O}";
    }

    /// <summary>
    /// Creates a UserAccount from a serialized string.
    /// </summary>
    public static UserAccount? Deserialize(string serialized)
    {
        if (string.IsNullOrEmpty(serialized))
            return null;

        var parts = serialized.Split('|');
        if (parts.Length < 2)
            return null;

        var account = new UserAccount
        {
            Name = parts[0],
            CredentialFolderPath = parts[1]
        };

        if (parts.Length >= 3 && DateTime.TryParse(parts[2], out var created))
            account.CreatedAt = created;

        if (parts.Length >= 4 && DateTime.TryParse(parts[3], out var lastUsed))
            account.LastUsedAt = lastUsed;

        return account;
    }

    public override string ToString() => Name;
}
