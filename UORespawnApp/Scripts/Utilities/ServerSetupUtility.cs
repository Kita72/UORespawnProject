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
        /// Server-side scripts folder name within Data/SERVER/
        /// </summary>
        private const string SERVER_SCRIPTS_FOLDER = "UORespawnSystem";

        /// <summary>
        /// Parent folder containing server scripts (Data/SERVER)
        /// </summary>
        private const string SERVER_PARENT_FOLDER = "SERVER";

        /// <summary>
        /// Setup server-side scripts in ServUO/Scripts/Custom folder
        /// Creates Custom folder if needed and copies UORespawn server scripts
        /// </summary>
        /// <param name="scriptsFolderPath">Path to ServUO Scripts folder</param>
        /// <returns>Success status and message</returns>
        public static (bool success, string message) SetupServerScripts(string? scriptsFolderPath)
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
                var destinationPath = Path.Combine(customFolderPath, "UORespawn");
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                    Logger.Info($"Created UORespawn folder: {destinationPath}");
                }

                // Get the source UORespawnSystem folder from Data/SERVER/
                var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", SERVER_PARENT_FOLDER, SERVER_SCRIPTS_FOLDER);

                if (!Directory.Exists(sourcePath))
                {
                    Logger.Error($"Source server scripts folder not found: {sourcePath}");
                    return (false, "Server scripts source folder not found. Please ensure UORespawnSystem folder exists in the app directory.");
                }

                // Copy all files and subdirectories from UORespawnSystem to Scripts/Custom/UORespawn
                int filesCopied = CopyDirectoryRecursive(sourcePath, destinationPath);

                if (filesCopied == 0)
                {
                    return (false, "No server script files found to copy");
                }

                Logger.Info($"Server scripts setup complete: {filesCopied} files copied to {destinationPath}");
                return (true, $"Successfully installed {filesCopied} server script files to Scripts/Custom/UORespawn");
            }
            catch (Exception ex)
            {
                Logger.Error("Error setting up server scripts", ex);
                return (false, $"Error setting up scripts: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively copy all files and subdirectories from source to destination
        /// </summary>
        /// <param name="sourceDir">Source directory path</param>
        /// <param name="destDir">Destination directory path</param>
        /// <returns>Number of files copied</returns>
        private static int CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            int filesCopied = 0;

            // Create destination directory if it doesn't exist
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Copy all files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);

                try
                {
                    File.Copy(file, destFile, overwrite: true);
                    filesCopied++;
                    Logger.Info($"Copied: {fileName}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to copy {fileName}: {ex.Message}");
                }
            }

            // Recursively copy subdirectories
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);
                var destSubDir = Path.Combine(destDir, dirName);
                filesCopied += CopyDirectoryRecursive(subDir, destSubDir);
            }

            return filesCopied;
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

        /// <summary>
        /// Unlink server integration - clears settings only, preserves installed scripts
        /// </summary>
        /// <param name="dataFolderPath">Current Data folder path from Settings</param>
        /// <returns>Success status and message</returns>
        public static (bool success, string message) UnlinkServer(string? dataFolderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(dataFolderPath))
                {
                    return (true, "Server was not linked");
                }

                Logger.Info($"Server unlinked (scripts preserved): {dataFolderPath}");
                return (true, "Server unlinked (scripts preserved on server)");
            }
            catch (Exception ex)
            {
                Logger.Error("Error unlinking server", ex);
                return (false, $"Error unlinking: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete existing UORespawn/UORespawnSystem installation before fresh install
        /// </summary>
        /// <param name="scriptsFolderPath">Path to ServUO Scripts folder</param>
        /// <returns>True if cleanup was successful or no cleanup needed</returns>
        public static bool CleanupExistingInstallation(string? scriptsFolderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(scriptsFolderPath))
                    return true;

                var customFolderPath = Path.Combine(scriptsFolderPath, "Custom");
                if (!Directory.Exists(customFolderPath))
                    return true;

                // Check for and delete old UORespawn folder
                var uorRespawnPath = Path.Combine(customFolderPath, "UORespawn");
                if (Directory.Exists(uorRespawnPath))
                {
                    Directory.Delete(uorRespawnPath, true);
                    Logger.Info($"Cleaned up existing UORespawn folder: {uorRespawnPath}");
                }

                // Check for and delete old UORespawnSystem folder (legacy name)
                var uorRespawnSystemPath = Path.Combine(customFolderPath, "UORespawnSystem");
                if (Directory.Exists(uorRespawnSystemPath))
                {
                    Directory.Delete(uorRespawnSystemPath, true);
                    Logger.Info($"Cleaned up existing UORespawnSystem folder: {uorRespawnSystemPath}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error cleaning up existing installation", ex);
                return false;
            }
        }
    }
}
