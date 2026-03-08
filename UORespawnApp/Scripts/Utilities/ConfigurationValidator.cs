using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities;

/// <summary>
/// Validates and ensures required application configuration (folders/files) exists at startup.
/// Creates missing directories and provides diagnostic information about the app's data structure.
/// </summary>
public static class ConfigurationValidator
{
    /// <summary>
    /// Result of configuration validation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> CreatedFolders { get; set; } = [];
        public List<string> MissingFiles { get; set; } = [];
        public List<string> Warnings { get; set; } = [];
        public List<string> Errors { get; set; } = [];

        public bool HasWarnings => Warnings.Count > 0;
        public bool HasErrors => Errors.Count > 0;
    }

    /// <summary>
    /// Required folders that must exist for the app to function.
    /// These are created automatically if missing.
    /// </summary>
    private static readonly string[] RequiredFolders =
    [
        PathConstants.LocalDataPath,      // Data/UORespawn/ - local spawn data
        PathConstants.MapsPath,           // Data/MAPS/ - map images
        PathConstants.TilesPath,          // Data/TILES/ - tile images
        PathConstants.PacksPath,          // Data/PACKS/ - spawn packs root
        PathConstants.PacksApprovedPath,  // Data/PACKS/Approved/
        PathConstants.PacksCreatedPath,   // Data/PACKS/Created/
        PathConstants.PacksImportedPath,  // Data/PACKS/Imported/
    ];

    /// <summary>
    /// Validates the application configuration at startup.
    /// Creates missing folders and reports any issues.
    /// </summary>
    /// <returns>Validation result with details about created folders and any issues</returns>
    public static ValidationResult ValidateStartup()
    {
        var result = new ValidationResult();

        Logger.Info("=== Configuration Validation Started ===");

        // Validate and create required folders
        ValidateFolders(result);

        // Validate server link if configured
        ValidateServerLink(result);

        // Log summary
        LogValidationSummary(result);

        return result;
    }

    /// <summary>
    /// Validates and creates required folders.
    /// </summary>
    private static void ValidateFolders(ValidationResult result)
    {
        foreach (var folderPath in RequiredFolders)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    result.CreatedFolders.Add(folderPath);
                    Logger.Info($"[Config] Created folder: {folderPath}");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Cannot create folder '{folderPath}': {ex.Message}");
                Logger.Error($"[Config] Failed to create folder: {folderPath}", ex);
            }
        }
    }

    /// <summary>
    /// Validates the server link configuration if one is set.
    /// </summary>
    private static void ValidateServerLink(ValidationResult result)
    {
        var customFolder = Settings.ScriptsCustomFolder;
        var dataFolder = Settings.ServerDataFolder;

        if (string.IsNullOrEmpty(customFolder))
        {
            // No server link configured - that's fine
            Logger.Info("[Config] No server link configured (standalone mode)");
            return;
        }

        // Check if linked Custom folder still exists
        if (!Directory.Exists(customFolder))
        {
            result.Warnings.Add($"Linked Custom folder no longer exists: {customFolder}");
            Logger.Warning($"[Config] Linked Custom folder missing: {customFolder}");
            return;
        }

        // Check for UORespawnServer in custom folder
        var serverScriptsPath = Path.Combine(customFolder, "UORespawnServer");
        if (!Directory.Exists(serverScriptsPath))
        {
            result.Warnings.Add("UORespawnServer scripts not found in Custom folder. Run server setup.");
            Logger.Warning("[Config] Server scripts not found");
        }

        // Check data folder
        if (!string.IsNullOrEmpty(dataFolder) && Directory.Exists(dataFolder))
        {
            var serverDataPath = Path.Combine(dataFolder, "UORespawn");
            if (!Directory.Exists(serverDataPath))
            {
                result.Warnings.Add("Server UORespawn data folder not found. Server may need initialization.");
                Logger.Warning("[Config] Server Data/UORespawn folder not found");
            }
        }

        Logger.Info($"[Config] Server link validated: Custom={customFolder}");
    }

    /// <summary>
    /// Logs a summary of the validation results.
    /// </summary>
    private static void LogValidationSummary(ValidationResult result)
    {
        if (result.CreatedFolders.Count > 0)
        {
            Logger.Info($"[Config] Created {result.CreatedFolders.Count} missing folder(s)");
        }

        if (result.HasWarnings)
        {
            Logger.Warning($"[Config] Validation completed with {result.Warnings.Count} warning(s)");
        }

        if (result.HasErrors)
        {
            Logger.Error($"[Config] Validation completed with {result.Errors.Count} error(s)");
        }

        if (result.IsValid && !result.HasWarnings)
        {
            Logger.Info("[Config] Configuration validation passed");
        }

        Logger.Info("=== Configuration Validation Complete ===");
    }

    /// <summary>
    /// Gets a diagnostic summary of the current configuration.
    /// Useful for troubleshooting and support.
    /// </summary>
    /// <returns>Multi-line string with configuration details</returns>
    public static string GetDiagnosticSummary()
    {
        List<string> lines =
        [
            $"UORespawn v{Utility.Version} - Configuration Summary",
            $"────────────────────────────────────────",
            $"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}",
            $"Local Data Path: {PathConstants.LocalDataPath}",
            $"Maps Path: {PathConstants.MapsPath}",
            $"Packs Path: {PathConstants.PacksPath}",
            $"Scripts Custom Folder: {(string.IsNullOrEmpty(Settings.ScriptsCustomFolder) ? "(not linked)" : Settings.ScriptsCustomFolder)}",
            $"Server Data Folder: {(string.IsNullOrEmpty(Settings.ServerDataFolder) ? "(not linked)" : Settings.ServerDataFolder)}",
            $"Current Pack: {Settings.CurrentPackName ?? "(none)"}",
            $"Debug Mode: {Settings.IsDebugMode}",
            $"────────────────────────────────────────",
        ];

        // Check folder existence
        lines.Add("Folder Status:");
        lines.Add($"  Local Data: {(Directory.Exists(PathConstants.LocalDataPath) ? "✓" : "✗")}");
        lines.Add($"  Maps: {(Directory.Exists(PathConstants.MapsPath) ? "✓" : "✗")}");
        lines.Add($"  Packs: {(Directory.Exists(PathConstants.PacksPath) ? "✓" : "✗")}");

        if (!string.IsNullOrEmpty(Settings.ScriptsCustomFolder))
        {
            lines.Add($"  Custom Folder: {(Directory.Exists(Settings.ScriptsCustomFolder) ? "✓" : "✗ MISSING")}");
        }

        if (!string.IsNullOrEmpty(Settings.ServerDataFolder))
        {
            lines.Add($"  Data Folder:   {(Directory.Exists(Settings.ServerDataFolder) ? "✓" : "✗ MISSING")}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
