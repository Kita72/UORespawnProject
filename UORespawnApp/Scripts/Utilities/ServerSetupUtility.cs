using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Utility for ServUO server integration setup and validation
    /// Handles folder validation, script installation, and server configuration
    /// </summary>
    public static class ServerSetupUtility
    {
        /// <summary>
        /// Result of ServUO folder validation
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? DataFolderPath { get; set; }
            public string? ScriptsFolderPath { get; set; }
            public bool HasServUOExe { get; set; }
            public bool HasDataFolder { get; set; }
            public bool HasScriptsFolder { get; set; }
        }

        /// <summary>
        /// Validate that a folder is a valid ServUO installation
        /// Checks for ServUO.exe, Data folder, and Scripts folder
        /// </summary>
        /// <param name="folderPath">Path to potential ServUO folder</param>
        /// <returns>Validation result with details</returns>
        public static ValidationResult ValidateServUOFolder(string folderPath)
        {
            var result = new ValidationResult();

            try
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    result.Message = "No folder selected";
                    return result;
                }

                if (!Directory.Exists(folderPath))
                {
                    result.Message = "Selected folder does not exist";
                    return result;
                }

                // Check for ServUO.exe
                var exePath = Path.Combine(folderPath, "ServUO.exe");
                result.HasServUOExe = File.Exists(exePath);

                // Check for Data folder
                var dataPath = Path.Combine(folderPath, "Data");
                result.HasDataFolder = Directory.Exists(dataPath);
                result.DataFolderPath = dataPath;

                // Check for Scripts folder
                var scriptsPath = Path.Combine(folderPath, "Scripts");
                result.HasScriptsFolder = Directory.Exists(scriptsPath);
                result.ScriptsFolderPath = scriptsPath;

                // Build validation message
                if (!result.HasServUOExe)
                {
                    result.Message = "ServUO.exe not found in selected folder. Please select the main ServUO folder.";
                    return result;
                }

                if (!result.HasDataFolder)
                {
                    result.Message = "Data folder not found. Please select the main ServUO folder.";
                    return result;
                }

                if (!result.HasScriptsFolder)
                {
                    result.Message = "Scripts folder not found. Please select the main ServUO folder.";
                    return result;
                }

                // All checks passed
                result.IsValid = true;
                result.Message = "Valid ServUO installation detected!";
                Logger.Info($"Validated ServUO folder: {folderPath}");

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Error validating ServUO folder", ex);
                result.Message = $"Error validating folder: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Setup server-side scripts in ServUO/Scripts/Custom folder
        /// Creates Custom folder if needed and copies UORespawn server scripts
        /// </summary>
        /// <param name="scriptsFolderPath">Path to ServUO Scripts folder</param>
        /// <returns>Success status and message</returns>
        public static (bool success, string message) SetupServerScripts(string scriptsFolderPath)
        {
            try
            {
                if (!Directory.Exists(scriptsFolderPath))
                {
                    return (false, "Scripts folder not found");
                }

                // Check/create Custom folder
                var customFolderPath = Path.Combine(scriptsFolderPath, "Custom");
                if (!Directory.Exists(customFolderPath))
                {
                    Directory.CreateDirectory(customFolderPath);
                    Logger.Info($"Created Custom folder: {customFolderPath}");
                }

                // Check/create UORespawn subfolder in Custom
                var uorRespawnFolderPath = Path.Combine(customFolderPath, "UORespawn");
                if (!Directory.Exists(uorRespawnFolderPath))
                {
                    Directory.CreateDirectory(uorRespawnFolderPath);
                    Logger.Info($"Created UORespawn folder: {uorRespawnFolderPath}");
                }

                // TODO: Copy server-side scripts from Resources to UORespawn folder
                // For now, just create the folder structure
                // Future: Copy .cs files from Resources/Raw/ServerScripts to this location

                Logger.Info($"Server scripts setup complete at: {uorRespawnFolderPath}");
                return (true, $"Server scripts folder ready at: Scripts/Custom/UORespawn");
            }
            catch (Exception ex)
            {
                Logger.Error("Error setting up server scripts", ex);
                return (false, $"Error setting up scripts: {ex.Message}");
            }
        }

        /// <summary>
        /// Get user-friendly status message for current server integration state
        /// </summary>
        /// <param name="dataFolderPath">Current Data folder path from Settings</param>
        /// <returns>Status message with setup instructions</returns>
        public static string GetServerIntegrationStatus(string? dataFolderPath)
        {
            if (string.IsNullOrEmpty(dataFolderPath))
            {
                return "Not configured";
            }

            // Check if the Data folder still exists and is accessible
            try
            {
                if (Directory.Exists(dataFolderPath))
                {
                    // Get parent folder (should be ServUO main folder)
                    var servUOFolder = Directory.GetParent(dataFolderPath)?.FullName;
                    if (!string.IsNullOrEmpty(servUOFolder))
                    {
                        return $"Connected: {servUOFolder}";
                    }
                    return $"Connected: {dataFolderPath}";
                }
                else
                {
                    return "Configured but folder not found - please update";
                }
            }
            catch
            {
                return "Error checking status - please reconfigure";
            }
        }

        /// <summary>
        /// Check if server integration is properly configured
        /// </summary>
        public static bool IsServerConfigured(string? dataFolderPath)
        {
            if (string.IsNullOrEmpty(dataFolderPath))
                return false;

            try
            {
                return Directory.Exists(dataFolderPath);
            }
            catch
            {
                return false;
            }
        }
    }
}
