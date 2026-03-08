using System.Text.Json;
using System.Text.Json.Serialization;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Entities;

/// <summary>
/// FTP connection credentials stored in the user's credential folder.
/// This file lives in a folder the user controls - not in the app's storage.
/// Serialized to JSON format (uor_credentials.json).
/// </summary>
public class FtpCredentials
{
    /// <summary>
    /// FTP server hostname or IP address.
    /// Examples: "ftp.myserver.com", "192.168.1.100"
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// FTP server port. Standard FTP is 21, FTPS often uses 990.
    /// </summary>
    public int Port { get; set; } = 21;

    /// <summary>
    /// FTP username for authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// FTP password for authentication.
    /// Note: Stored in plain text in the user's chosen folder.
    /// Security is the user's responsibility (encrypted drive, secure location, etc.)
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Remote path to the Custom scripts folder on the server.
    /// Example: "/home/user/ServUO/Scripts/Custom" or "/home/user/MUO/Projects/UOContent/Custom"
    /// </summary>
    public string RemoteCustomPath { get; set; } = string.Empty;

    /// <summary>
    /// Remote path to the Data/ folder on the server (parent of UORespawn/).
    /// The UORespawn/ subfolder is appended automatically by the sync service.
    /// Example: "/home/user/ServUO/Data" or "/home/user/MUO/Distribution/Data"
    /// </summary>
    public string RemotePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use TLS/SSL for secure connections (FTPS).
    /// Strongly recommended for security.
    /// </summary>
    public bool UseTls { get; set; } = true;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable passive mode (usually required for NAT/firewalls).
    /// </summary>
    public bool UsePassiveMode { get; set; } = true;

    /// <summary>
    /// Last successful connection timestamp.
    /// </summary>
    public DateTime? LastConnected { get; set; }

    /// <summary>
    /// Optional notes about this connection (e.g., "Production Server", "Test Environment").
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    // ==================== VALIDATION ====================

    /// <summary>
    /// Checks if the minimum required fields are filled.
    /// </summary>
    [JsonIgnore]
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(RemoteCustomPath) &&
        !string.IsNullOrWhiteSpace(RemotePath);

    /// <summary>
    /// Validates the credentials and returns any issues.
    /// </summary>
    [JsonIgnore]
    public List<string> ValidationErrors
    {
        get
        {
            List<string> errors = [];

            if (string.IsNullOrWhiteSpace(Host))
                errors.Add("Host is required");

            if (Port < 1 || Port > 65535)
                errors.Add("Port must be between 1 and 65535");

            if (string.IsNullOrWhiteSpace(Username))
                errors.Add("Username is required");

            if (string.IsNullOrWhiteSpace(RemoteCustomPath))
                errors.Add("Remote custom scripts path is required");

            if (!string.IsNullOrWhiteSpace(RemoteCustomPath) && !RemoteCustomPath.StartsWith('/'))
                errors.Add("Remote custom scripts path should start with /");

            if (string.IsNullOrWhiteSpace(RemotePath))
                errors.Add("Remote data exchange path is required");

            if (!string.IsNullOrWhiteSpace(RemotePath) && !RemotePath.StartsWith('/'))
                errors.Add("Remote data exchange path should start with /");

            return errors;
        }
    }

    // ==================== SERIALIZATION ====================

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Saves credentials to a JSON file.
    /// </summary>
    public bool SaveToFile(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save credentials: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads credentials from a JSON file.
    /// </summary>
    public static FtpCredentials? LoadFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FtpCredentials>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load credentials: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates a display-safe summary (hides password).
    /// </summary>
    public override string ToString()
    {
        var tlsIndicator = UseTls ? "FTPS" : "FTP";
        return $"{tlsIndicator}://{Username}@{Host}:{Port} | Custom: {RemoteCustomPath} | Data: {RemotePath}";
    }
}
