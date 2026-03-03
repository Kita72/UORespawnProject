using System.Text.Json;
using System.Text.Json.Serialization;

namespace UORespawnApp.Scripts.Entities;

/// <summary>
/// Represents a user account in the UORespawn app.
/// 
/// SECURITY BY DESIGN:
/// - Account data lives ONLY in the user's chosen folder (uor_account.json)
/// - The app stores only folder paths - if user deletes folder, account is gone
/// - No passwords needed for the app itself - just a friendly name
/// - FTP/AI credentials are separate files the user controls
/// 
/// This gives users full control over their data with zero app-side storage of sensitive info.
/// </summary>
public class UserAccount
{
    private const string AccountFileName = "uor_account.json";

    /// <summary>
    /// Display name for this account (e.g., "Wilson", "TestServer", "Production").
    /// This is just a friendly label - no password required for the app.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When this account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// When this account was last used.
    /// </summary>
    public DateTime LastUsedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Full path to the folder where this account's data is stored.
    /// This is set when loading, not serialized to JSON.
    /// </summary>
    [JsonIgnore]
    public string CredentialFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Checks if this account has a valid folder.
    /// </summary>
    [JsonIgnore]
    public bool HasCredentialFolder => !string.IsNullOrEmpty(CredentialFolderPath) && Directory.Exists(CredentialFolderPath);

    /// <summary>
    /// Path to the account file in this folder.
    /// </summary>
    [JsonIgnore]
    public string AccountFilePath => string.IsNullOrEmpty(CredentialFolderPath)
        ? string.Empty
        : Path.Combine(CredentialFolderPath, AccountFileName);

    /// <summary>
    /// Path to the FTP credentials file in this folder.
    /// </summary>
    [JsonIgnore]
    public string CredentialFilePath => string.IsNullOrEmpty(CredentialFolderPath)
        ? string.Empty
        : Path.Combine(CredentialFolderPath, "uor_credentials.json");

    /// <summary>
    /// Checks if FTP credentials exist for this account.
    /// </summary>
    [JsonIgnore]
    public bool HasCredentials => !string.IsNullOrEmpty(CredentialFilePath) && File.Exists(CredentialFilePath);

    /// <summary>
    /// Saves this account to its folder.
    /// </summary>
    public bool SaveToFolder()
    {
        if (string.IsNullOrEmpty(CredentialFolderPath))
            return false;

        try
        {
            if (!Directory.Exists(CredentialFolderPath))
                Directory.CreateDirectory(CredentialFolderPath);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AccountFilePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Loads an account from a folder. Returns null if no valid account file exists.
    /// </summary>
    public static UserAccount? LoadFromFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return null;

        var accountFile = Path.Combine(folderPath, AccountFileName);
        if (!File.Exists(accountFile))
            return null;

        try
        {
            var json = File.ReadAllText(accountFile);
            var account = JsonSerializer.Deserialize<UserAccount>(json);
            if (account != null)
            {
                account.CredentialFolderPath = folderPath;
            }
            return account;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a folder contains a valid account file.
    /// </summary>
    public static bool IsValidAccountFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return false;

        return File.Exists(Path.Combine(folderPath, AccountFileName));
    }

    public override string ToString() => Name;
}
