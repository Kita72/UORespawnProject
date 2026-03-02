namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Utility for ServUO server integration setup and validation.
    /// Handles folder validation, script installation, version checking, and server configuration.
    /// 
    /// Server Structure on ServUO:
    /// - Scripts/Custom/UORespawnServer/ (server scripts - namespace must match folder name)
    /// - Data/UORespawn/ (runtime data with INPUT/, OUTPUT/, COMMANDS/, STATS/, SYS/ subfolders)
    /// 
    /// Version Management:
    /// - Editor version in Utility.Version
    /// - Server version in UOR_Settings.cs VERSION constant
    /// - Auto-update when versions mismatch
    /// 
    /// Legacy Cleanup:
    /// - Removes any folder starting with "UORespawn" or "Respawn" in Scripts/Custom/
    /// - Removes Data/Respawn/ folder if exists (old naming)
    /// </summary>
    public static class ServerSetupUtility
    {
        /// <summary>
        /// Server scripts folder name - MUST match namespace (Server.Custom.UORespawnServer)
        /// </summary>
        private const string SERVER_SCRIPTS_FOLDER = "UORespawnServer";

        /// <summary>
        /// Server data folder name under ServUO/Data/
        /// </summary>
        private const string SERVER_DATA_FOLDER = "UORespawn";

        /// <summary>
        /// Parent folder containing server scripts source (Data/SERVER/)
        /// </summary>
        private const string SERVER_SOURCE_PARENT = "SERVER";

        /// <summary>
        /// Prefixes used by legacy/old installations that need cleanup
        /// </summary>
        private static readonly string[] LEGACY_FOLDER_PREFIXES = ["UORespawn", "Respawn"];

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
        /// Result of server installation check
        /// </summary>
        public class InstallationStatus
        {
            public bool IsInstalled { get; set; }
            public string? InstalledVersion { get; set; }
            public string EditorVersion { get; set; } = Utility.Version;
            public bool NeedsUpdate => IsInstalled && InstalledVersion != EditorVersion;
            public string? ScriptsPath { get; set; }
            public string? DataPath { get; set; }
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

        #region Installation Status & Version Checking

        /// <summary>
        /// Check installation status and version of server scripts
        /// </summary>
        /// <param name="servuoPath">Path to ServUO root folder</param>
        /// <returns>Installation status including version info</returns>
        public static InstallationStatus CheckInstallation(string? servuoPath)
        {
            var status = new InstallationStatus();

            try
            {
                if (string.IsNullOrEmpty(servuoPath))
                    return status;

                var scriptsPath = Path.Combine(servuoPath, "Scripts", "Custom", SERVER_SCRIPTS_FOLDER);
                var dataPath = Path.Combine(servuoPath, "Data", SERVER_DATA_FOLDER);

                status.ScriptsPath = scriptsPath;
                status.DataPath = dataPath;

                if (!Directory.Exists(scriptsPath))
                    return status;

                // Check for UOR_Settings.cs to extract version
                var settingsFile = Path.Combine(scriptsPath, "UOR_Settings.cs");
                if (!File.Exists(settingsFile))
                    return status;

                status.IsInstalled = true;
                status.InstalledVersion = ExtractVersionFromSettingsFile(settingsFile);

                Logger.Info($"Server installation found: v{status.InstalledVersion} (editor: v{status.EditorVersion})");

                return status;
            }
            catch (Exception ex)
            {
                Logger.Error("Error checking server installation", ex);
                return status;
            }
        }

        /// <summary>
        /// Extract version string from UOR_Settings.cs file
        /// Looks for: internal const string VERSION = "x.x.x.x";
        /// </summary>
        private static string? ExtractVersionFromSettingsFile(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);

                // Look for VERSION constant pattern
                var patterns = new[]
                {
                    @"internal\s+const\s+string\s+VERSION\s*=\s*""([^""]+)""",
                    @"const\s+string\s+VERSION\s*=\s*""([^""]+)""",
                    @"VERSION\s*=\s*""([^""]+)"""
                };

                foreach (var pattern in patterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(content, pattern);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value;
                    }
                }

                Logger.Warning("Could not extract version from UOR_Settings.cs");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error reading version from settings file: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Legacy Cleanup

        /// <summary>
        /// Clean up ALL legacy UORespawn installations from ServUO.
        /// Removes any folder starting with "UORespawn" or "Respawn" in:
        /// - Scripts/Custom/
        /// - Data/
        /// This ensures clean state before fresh install.
        /// </summary>
        /// <param name="servuoPath">Path to ServUO root folder</param>
        /// <returns>Number of folders removed</returns>
        public static int CleanupAllLegacyInstallations(string? servuoPath)
        {
            int foldersRemoved = 0;

            try
            {
                if (string.IsNullOrEmpty(servuoPath))
                    return 0;

                // Clean Scripts/Custom/ folder
                var customPath = Path.Combine(servuoPath, "Scripts", "Custom");
                if (Directory.Exists(customPath))
                {
                    foldersRemoved += CleanupLegacyFoldersInDirectory(customPath);
                }

                // Clean Data/ folder (remove Data/Respawn/, Data/UORespawn/ if exists with old structure)
                var dataPath = Path.Combine(servuoPath, "Data");
                if (Directory.Exists(dataPath))
                {
                    // Remove old "Respawn" folder (pre-2.0 naming)
                    var oldRespawnPath = Path.Combine(dataPath, "Respawn");
                    if (Directory.Exists(oldRespawnPath))
                    {
                        Directory.Delete(oldRespawnPath, true);
                        foldersRemoved++;
                        Logger.Info($"Removed legacy Data/Respawn folder");
                    }
                }

                if (foldersRemoved > 0)
                {
                    Logger.Info($"Legacy cleanup complete: {foldersRemoved} folder(s) removed");
                }

                return foldersRemoved;
            }
            catch (Exception ex)
            {
                Logger.Error("Error during legacy cleanup", ex);
                return foldersRemoved;
            }
        }

        /// <summary>
        /// Remove folders starting with legacy prefixes from a directory
        /// </summary>
        private static int CleanupLegacyFoldersInDirectory(string directoryPath)
        {
            int removed = 0;

            try
            {
                foreach (var folder in Directory.GetDirectories(directoryPath))
                {
                    var folderName = Path.GetFileName(folder);

                    // Check if folder name starts with any legacy prefix
                    foreach (var prefix in LEGACY_FOLDER_PREFIXES)
                    {
                        if (folderName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                Directory.Delete(folder, true);
                                removed++;
                                Logger.Info($"Removed legacy folder: {folder}");
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning($"Failed to remove legacy folder {folderName}: {ex.Message}");
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error scanning directory for legacy folders: {ex.Message}");
            }

            return removed;
        }

        #endregion

        #region Server Installation

        /// <summary>
        /// Full server setup - cleans legacy, installs scripts, creates data folders.
        /// Call this for fresh install or when linking a new server.
        /// </summary>
        /// <param name="servuoPath">Path to ServUO root folder</param>
        /// <param name="forceReinstall">If true, reinstall even if already installed</param>
        /// <returns>Success status and message</returns>
        public static (bool success, string message) FullServerSetup(string servuoPath, bool forceReinstall = false)
        {
            try
            {
                Logger.Info($"Starting full server setup for: {servuoPath}");

                // Step 1: Validate ServUO folder
                var validation = ValidateServUOFolder(servuoPath);
                if (!validation.IsValid)
                {
                    return (false, validation.Message);
                }

                // Step 2: Check existing installation
                var installStatus = CheckInstallation(servuoPath);

                // Step 3: Decide if we need to install/update
                if (installStatus.IsInstalled && !installStatus.NeedsUpdate && !forceReinstall)
                {
                    Logger.Info("Server already installed with matching version - no action needed");

                    // Still ensure data folders exist
                    SetupServerDataFolders(servuoPath);

                    return (true, $"Server already installed (v{installStatus.InstalledVersion})");
                }

                // Step 4: Clean up ALL legacy installations
                int cleaned = CleanupAllLegacyInstallations(servuoPath);
                if (cleaned > 0)
                {
                    Logger.Info($"Cleaned {cleaned} legacy folder(s)");
                }

                // Step 5: Install server scripts
                var scriptsResult = InstallServerScripts(servuoPath);
                if (!scriptsResult.success)
                {
                    return scriptsResult;
                }

                // Step 6: Setup data folders
                var dataResult = SetupServerDataFolders(servuoPath);
                if (!dataResult.success)
                {
                    return dataResult;
                }

                string action = installStatus.IsInstalled ? "updated" : "installed";
                Logger.Info($"Server {action} successfully to v{Utility.Version}");

                return (true, $"Server {action} successfully (v{Utility.Version})");
            }
            catch (Exception ex)
            {
                Logger.Error("Error during full server setup", ex);
                return (false, $"Setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Install server scripts to Scripts/Custom/UORespawnServer/
        /// </summary>
        private static (bool success, string message) InstallServerScripts(string servuoPath)
        {
            try
            {
                var scriptsPath = Path.Combine(servuoPath, "Scripts");

                // Ensure Custom folder exists
                var customPath = Path.Combine(scriptsPath, "Custom");
                if (!Directory.Exists(customPath))
                {
                    Directory.CreateDirectory(customPath);
                    Logger.Info($"Created Custom folder: {customPath}");
                }

                // Destination: Scripts/Custom/UORespawnServer/
                var destinationPath = Path.Combine(customPath, SERVER_SCRIPTS_FOLDER);

                // Remove existing if present (we already cleaned legacy, but this ensures exact folder is fresh)
                if (Directory.Exists(destinationPath))
                {
                    Directory.Delete(destinationPath, true);
                }

                Directory.CreateDirectory(destinationPath);

                // Source: App/Data/SERVER/UORespawnServer/
                var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", SERVER_SOURCE_PARENT, SERVER_SCRIPTS_FOLDER);

                if (!Directory.Exists(sourcePath))
                {
                    Logger.Error($"Server scripts source not found: {sourcePath}");
                    return (false, "Server scripts source folder not found in editor installation.");
                }

                // Copy all files recursively
                int filesCopied = CopyDirectoryRecursive(sourcePath, destinationPath);

                if (filesCopied == 0)
                {
                    return (false, "No server script files found to copy");
                }

                Logger.Info($"Installed {filesCopied} server script files to {destinationPath}");
                return (true, $"Installed {filesCopied} script files");
            }
            catch (Exception ex)
            {
                Logger.Error("Error installing server scripts", ex);
                return (false, $"Error installing scripts: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup server data folder structure: Data/UORespawn/ with subfolders
        /// </summary>
        private static (bool success, string message) SetupServerDataFolders(string servuoPath)
        {
            try
            {
                var dataPath = Path.Combine(servuoPath, "Data", SERVER_DATA_FOLDER);

                // Create main UORespawn folder
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                    Logger.Info($"Created server data folder: {dataPath}");
                }

                // Create subfolders
                string[] subfolders = ["INPUT", "OUTPUT", "COMMANDS", "STATS", "SYS"];

                foreach (var subfolder in subfolders)
                {
                    var subPath = Path.Combine(dataPath, subfolder);
                    if (!Directory.Exists(subPath))
                    {
                        Directory.CreateDirectory(subPath);
                        Logger.Info($"Created subfolder: {subfolder}");
                    }
                }

                return (true, "Data folders created");
            }
            catch (Exception ex)
            {
                Logger.Error("Error setting up server data folders", ex);
                return (false, $"Error creating data folders: {ex.Message}");
            }
        }

        #endregion

        #region Startup Version Check

        /// <summary>
        /// Check server version on startup and auto-update if needed.
        /// Called by BackgroundDataLoader when server is linked.
        /// </summary>
        /// <param name="servuoPath">Path to ServUO root folder</param>
        /// <returns>True if server is ready to use (installed/updated), false if error</returns>
        public static bool CheckAndUpdateServerOnStartup(string? servuoPath)
        {
            try
            {
                if (string.IsNullOrEmpty(servuoPath))
                    return false;

                var validation = ValidateServUOFolder(servuoPath);
                if (!validation.IsValid)
                {
                    Logger.Warning($"Linked server folder is not valid: {validation.Message}");
                    return false;
                }

                var installStatus = CheckInstallation(servuoPath);

                if (!installStatus.IsInstalled)
                {
                    Logger.Info("Server not installed - performing full setup");
                    var result = FullServerSetup(servuoPath);
                    return result.success;
                }

                if (installStatus.NeedsUpdate)
                {
                    Logger.Info($"Server version mismatch (installed: {installStatus.InstalledVersion}, editor: {installStatus.EditorVersion}) - updating...");
                    var result = FullServerSetup(servuoPath, forceReinstall: true);
                    return result.success;
                }

                Logger.Info($"Server version matches editor (v{installStatus.InstalledVersion})");

                // Ensure data folders exist even if version matches
                SetupServerDataFolders(servuoPath);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error during startup server check", ex);
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Recursively copy all files and subdirectories from source to destination.
        /// Skips DOCS folder (documentation not needed on server).
        /// </summary>
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
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to copy {fileName}: {ex.Message}");
                }
            }

            // Recursively copy subdirectories (skip DOCS folder)
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);

                // Skip DOCS folder - not needed on server
                if (dirName.Equals("DOCS", StringComparison.OrdinalIgnoreCase))
                    continue;

                var destSubDir = Path.Combine(destDir, dirName);
                filesCopied += CopyDirectoryRecursive(subDir, destSubDir);
            }

            return filesCopied;
        }

        /// <summary>
        /// Check if server integration is properly configured
        /// </summary>
        public static bool IsServerConfigured(string? servuoPath)
        {
            if (string.IsNullOrEmpty(servuoPath))
                return false;

            try
            {
                var scriptsPath = Path.Combine(servuoPath, "Scripts", "Custom", SERVER_SCRIPTS_FOLDER);
                return Directory.Exists(scriptsPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Unlink server integration - clears settings only, preserves installed scripts
        /// </summary>
        public static (bool success, string message) UnlinkServer(string? servuoPath)
        {
            try
            {
                if (string.IsNullOrEmpty(servuoPath))
                {
                    return (true, "Server was not linked");
                }

                Logger.Info($"Server unlinked (scripts preserved): {servuoPath}");
                return (true, "Server unlinked (scripts preserved on server)");
            }
            catch (Exception ex)
            {
                Logger.Error("Error unlinking server", ex);
                return (false, $"Error unlinking: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the path where server scripts are installed
        /// </summary>
        public static string GetServerScriptsPath(string servuoPath)
        {
            return Path.Combine(servuoPath, "Scripts", "Custom", SERVER_SCRIPTS_FOLDER);
        }

        /// <summary>
        /// Get the path where server data is stored
        /// </summary>
        public static string GetServerDataPath(string servuoPath)
        {
            return Path.Combine(servuoPath, "Data", SERVER_DATA_FOLDER);
        }

        #endregion

        #region Legacy Compatibility (Deprecated Methods)

        /// <summary>
        /// [DEPRECATED] Use FullServerSetup instead.
        /// Setup server-side scripts in ServUO/Scripts/Custom folder.
        /// </summary>
        [Obsolete("Use FullServerSetup instead")]
        public static (bool success, string message) SetupServerScripts(string? scriptsFolderPath)
        {
            if (string.IsNullOrEmpty(scriptsFolderPath))
                return (false, "Scripts folder not found");

            // Extract ServUO path from scripts path
            var servuoPath = Path.GetDirectoryName(scriptsFolderPath);
            if (string.IsNullOrEmpty(servuoPath))
                return (false, "Invalid scripts path");

            return FullServerSetup(servuoPath);
        }

        /// <summary>
        /// [DEPRECATED] Use CleanupAllLegacyInstallations instead.
        /// Delete existing UORespawn installation before fresh install.
        /// </summary>
        [Obsolete("Use CleanupAllLegacyInstallations instead")]
        public static bool CleanupExistingInstallation(string? scriptsFolderPath)
        {
            if (string.IsNullOrEmpty(scriptsFolderPath))
                return true;

            var servuoPath = Path.GetDirectoryName(scriptsFolderPath);
            if (string.IsNullOrEmpty(servuoPath))
                return false;

            return CleanupAllLegacyInstallations(servuoPath) >= 0;
        }

        #endregion
    }
}
