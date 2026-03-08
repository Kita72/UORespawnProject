namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Utility for server integration setup and validation.
    /// Handles folder validation, script installation, version checking, and server configuration.
    ///
    /// New Two-Path Approach (v2.0.2+):
    ///   Users now pick TWO explicit folders when linking a server:
    ///   1. Scripts Custom Folder — the Custom/ directory where UORespawnServer/ scripts are installed
    ///      ServUO example : Scripts/Custom/
    ///      MUO example    : Projects/UOContent/Custom/
    ///   2. Server Data Folder — the Data/ directory where UORespawn/ data exchange lives
    ///      ServUO example : Data/
    ///      MUO example    : Distribution/Data/
    ///
    ///   The app no longer needs to know the server root or type at runtime —
    ///   users point directly at the correct folders.
    ///
    /// Script Source (Data/SERVER/{ServerType}/UORespawnServer/):
    ///   Data/SERVER/SERVUO/UORespawnServer/  →  deployed to chosen custom folder
    ///   Data/SERVER/MUO/UORespawnServer/     →  deployed to chosen custom folder
    ///
    /// Version Management:
    /// - Editor version in Utility.Version
    /// - Server version in UOR_Settings.cs VERSION constant
    /// - Auto-update when versions mismatch (user confirmation required)
    ///
    /// Legacy Cleanup:
    /// - Removes any folder starting with "UORespawn" or "Respawn" in the custom folder
    /// - Removes Data/Respawn/ folder if exists (old naming)
    /// </summary>
    public static class ServerSetupUtility
    {
        // ==================== SERVER TYPE ====================

        /// <summary>
        /// Supported server platforms UORespawn can integrate with.
        /// </summary>
        public enum ServerType
        {
            ServUO,
            MUO
        }

        /// <summary>
        /// Maps a ServerType to the subfolder name under Data/SERVER/ in the editor.
        /// </summary>
        private static string GetSourceSubfolder(ServerType serverType) => serverType switch
        {
            ServerType.ServUO => "SERVUO",
            ServerType.MUO    => "MUO",
            _                 => "SERVUO"
        };

        // ==================== CONSTANTS ====================

        /// <summary>
        /// Server scripts folder name — MUST match namespace (Server.Custom.UORespawnServer)
        /// </summary>
        private const string SERVER_SCRIPTS_FOLDER = "UORespawnServer";

        /// <summary>
        /// Server data folder name placed under the server's data directory
        /// </summary>
        private const string SERVER_DATA_FOLDER = "UORespawn";

        /// <summary>
        /// Parent folder containing server script sources (Data/SERVER/)
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
        /// Validate that a folder can be used as the server's Custom scripts folder.
        /// Just checks it exists — user has already navigated to the correct location.
        /// </summary>
        public static ValidationResult ValidateCustomFolder(string customFolderPath)
        {
            var result = new ValidationResult();

            try
            {
                if (string.IsNullOrWhiteSpace(customFolderPath))
                {
                    result.Message = "No Custom folder selected";
                    return result;
                }

                if (!Directory.Exists(customFolderPath))
                {
                    result.Message = "Selected Custom folder does not exist";
                    return result;
                }

                result.IsValid = true;
                result.Message = "Custom folder is valid";
                result.ScriptsFolderPath = Path.Combine(customFolderPath, SERVER_SCRIPTS_FOLDER);
                result.HasScriptsFolder = Directory.Exists(result.ScriptsFolderPath);

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Error validating Custom folder", ex);
                result.Message = $"Error validating folder: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Validate that a folder can be used as the server's Data folder.
        /// Just checks it exists — user has already navigated to the correct location.
        /// </summary>
        public static ValidationResult ValidateDataFolder(string serverDataFolderPath)
        {
            var result = new ValidationResult();

            try
            {
                if (string.IsNullOrWhiteSpace(serverDataFolderPath))
                {
                    result.Message = "No Data folder selected";
                    return result;
                }

                if (!Directory.Exists(serverDataFolderPath))
                {
                    result.Message = "Selected Data folder does not exist";
                    return result;
                }

                result.IsValid = true;
                result.Message = "Data folder is valid";
                result.DataFolderPath = Path.Combine(serverDataFolderPath, SERVER_DATA_FOLDER);
                result.HasDataFolder = Directory.Exists(result.DataFolderPath);

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Error validating Data folder", ex);
                result.Message = $"Error validating folder: {ex.Message}";
                return result;
            }
        }

        #region Installation Status & Version Checking

        /// <summary>
        /// Check installation status and version of server scripts.
        /// </summary>
        /// <param name="customFolderPath">Path to the Custom/ folder where UORespawnServer/ is installed</param>
        /// <param name="serverDataFolderPath">Path to the Data/ folder where UORespawn/ lives (optional)</param>
        /// <returns>Installation status including version info</returns>
        public static InstallationStatus CheckInstallation(string? customFolderPath, string? serverDataFolderPath = null)
        {
            var status = new InstallationStatus();

            try
            {
                if (string.IsNullOrEmpty(customFolderPath))
                    return status;

                var scriptsPath = Path.Combine(customFolderPath, SERVER_SCRIPTS_FOLDER);
                var dataPath = !string.IsNullOrEmpty(serverDataFolderPath)
                    ? Path.Combine(serverDataFolderPath, SERVER_DATA_FOLDER)
                    : null;

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
        /// Clean up ALL legacy UORespawn installations from the selected folders.
        /// Removes any folder starting with "UORespawn" or "Respawn" in the custom folder.
        /// Removes the old Respawn/ folder from the data folder if present.
        /// </summary>
        /// <param name="customFolderPath">The Custom/ folder where UORespawnServer/ is installed</param>
        /// <param name="serverDataFolderPath">The Data/ folder where UORespawn/ lives</param>
        /// <returns>Number of folders removed</returns>
        public static int CleanupLegacyInstallations(string? customFolderPath, string? serverDataFolderPath)
        {
            int foldersRemoved = 0;

            try
            {
                // Clean custom folder (remove folders with legacy prefix names)
                if (!string.IsNullOrEmpty(customFolderPath) && Directory.Exists(customFolderPath))
                {
                    foldersRemoved += CleanupLegacyFoldersInDirectory(customFolderPath);
                }

                // Clean data folder (remove legacy Respawn/ if present from pre-2.0 naming)
                if (!string.IsNullOrEmpty(serverDataFolderPath) && Directory.Exists(serverDataFolderPath))
                {
                    var oldRespawnPath = Path.Combine(serverDataFolderPath, "Respawn");
                    if (Directory.Exists(oldRespawnPath))
                    {
                        Directory.Delete(oldRespawnPath, true);
                        foldersRemoved++;
                        Logger.Info("Removed legacy Respawn folder from data folder");
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
        /// <param name="customFolderPath">The Custom/ folder where UORespawnServer/ scripts will be installed</param>
        /// <param name="serverDataFolderPath">The Data/ folder where UORespawn/ data exchange will be created</param>
        /// <param name="serverType">Target server type (ServUO or MUO) - selects bundled script source</param>
        /// <param name="forceReinstall">If true, reinstall even if already installed</param>
        /// <returns>Success status and message</returns>
        public static (bool success, string message) FullServerSetup(string customFolderPath, string serverDataFolderPath, ServerType serverType = ServerType.ServUO, bool forceReinstall = false)
        {
            try
            {
                Logger.Info($"Starting full {serverType} server setup...");
                Logger.Info($"  Custom folder : {customFolderPath}");
                Logger.Info($"  Data folder   : {serverDataFolderPath}");

                // Step 1: Validate folders
                var customValidation = ValidateCustomFolder(customFolderPath);
                if (!customValidation.IsValid)
                    return (false, customValidation.Message);

                var dataValidation = ValidateDataFolder(serverDataFolderPath);
                if (!dataValidation.IsValid)
                    return (false, dataValidation.Message);

                // Step 2: Check existing installation
                var installStatus = CheckInstallation(customFolderPath, serverDataFolderPath);

                // Step 3: Decide if we need to install/update
                if (installStatus.IsInstalled && !installStatus.NeedsUpdate && !forceReinstall)
                {
                    Logger.Info("Server already installed with matching version - no action needed");

                    // Still ensure data folders exist
                    SetupServerDataFolders(serverDataFolderPath);

                    return (true, $"Server already installed (v{installStatus.InstalledVersion})");
                }

                // Step 4: Clean up legacy installations
                int cleaned = CleanupLegacyInstallations(customFolderPath, serverDataFolderPath);
                if (cleaned > 0)
                    Logger.Info($"Cleaned {cleaned} legacy folder(s)");

                // Step 5: Install server scripts
                var scriptsResult = InstallServerScripts(customFolderPath, serverType);
                if (!scriptsResult.success)
                    return scriptsResult;

                // Step 6: Setup data folders
                var dataResult = SetupServerDataFolders(serverDataFolderPath);
                if (!dataResult.success)
                    return dataResult;

                string action = installStatus.IsInstalled ? "updated" : "installed";
                Logger.Info($"{serverType} server {action} successfully to v{Utility.Version}");

                return (true, $"{serverType} server {action} successfully (v{Utility.Version})");
            }
            catch (Exception ex)
            {
                Logger.Error("Error during full server setup", ex);
                return (false, $"Setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Install server scripts to the chosen Custom folder.
        /// Destination: {customFolderPath}/UORespawnServer/
        /// Source: App/Data/SERVER/{SERVUO|MUO}/UORespawnServer/
        /// </summary>
        private static (bool success, string message) InstallServerScripts(string customFolderPath, ServerType serverType)
        {
            try
            {
                // Destination: {customFolderPath}/UORespawnServer/
                if (!Directory.Exists(customFolderPath))
                {
                    Directory.CreateDirectory(customFolderPath);
                    Logger.Info($"Created custom folder: {customFolderPath}");
                }

                var destinationPath = Path.Combine(customFolderPath, SERVER_SCRIPTS_FOLDER);

                // Remove existing if present (ensures clean install)
                if (Directory.Exists(destinationPath))
                {
                    Directory.Delete(destinationPath, true);
                }

                Directory.CreateDirectory(destinationPath);

                // Source: App/Data/SERVER/{SERVUO|MUO}/UORespawnServer/
                var sourcePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Data", SERVER_SOURCE_PARENT, GetSourceSubfolder(serverType), SERVER_SCRIPTS_FOLDER);

                if (!Directory.Exists(sourcePath))
                {
                    Logger.Error($"Server scripts source not found: {sourcePath}");
                    return (false, $"{serverType} server scripts source folder not found in editor installation.");
                }

                int filesCopied = CopyDirectoryRecursive(sourcePath, destinationPath);

                if (filesCopied == 0)
                {
                    return (false, "No server script files found to copy");
                }

                Logger.Info($"Installed {filesCopied} {serverType} server script files to {destinationPath}");
                return (true, $"Installed {filesCopied} script files");
            }
            catch (Exception ex)
            {
                Logger.Error("Error installing server scripts", ex);
                return (false, $"Error installing scripts: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup server data folder structure (INPUT, OUTPUT, COMMANDS, STATS, SYS).
        /// Destination: {serverDataFolderPath}/UORespawn/
        /// </summary>
        private static (bool success, string message) SetupServerDataFolders(string serverDataFolderPath)
        {
            try
            {
                var dataPath = Path.Combine(serverDataFolderPath, SERVER_DATA_FOLDER);

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
        /// [DEPRECATED] Old auto-update method - now just ensures data folders exist.
        /// Version checking is now handled by ServerUpdateService which prompts the user.
        /// 
        /// This method is kept for backwards compatibility but NO LONGER auto-updates.
        /// Use ServerUpdateService.CheckForServerUpdate() for version checking with user confirmation.
        /// </summary>
        /// <returns>True if server folders are ready, false if error</returns>
        [Obsolete("Use ServerUpdateService.CheckForServerUpdate() instead for version checking with user confirmation")]
        public static bool CheckAndUpdateServerOnStartup()
        {
            try
            {
                var customFolder = Settings.ScriptsCustomFolder;
                if (string.IsNullOrEmpty(customFolder))
                    return false;

                var dataFolder = Settings.ServerDataFolder;
                var installStatus = CheckInstallation(customFolder, dataFolder);

                if (!installStatus.IsInstalled)
                {
                    Logger.Info("Server scripts not installed - will be installed when user links server");
                    return true;
                }

                if (installStatus.NeedsUpdate)
                {
                    Logger.Info($"Server version mismatch detected (installed: {installStatus.InstalledVersion}, editor: {installStatus.EditorVersion})");
                    Logger.Info("Update will be offered via ServerUpdateService - user confirmation required");

                    if (!string.IsNullOrEmpty(dataFolder))
                        SetupServerDataFolders(dataFolder);

                    return true;
                }

                Logger.Info($"Server version matches editor (v{installStatus.InstalledVersion})");

                if (!string.IsNullOrEmpty(dataFolder))
                    SetupServerDataFolders(dataFolder);

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
        /// Check if server integration is properly configured (both folders set and scripts installed)
        /// </summary>
        public static bool IsServerConfigured()
        {
            var customFolder = Settings.ScriptsCustomFolder;
            var dataFolder = Settings.ServerDataFolder;

            if (string.IsNullOrEmpty(customFolder) || string.IsNullOrEmpty(dataFolder))
                return false;

            try
            {
                var scriptsPath = Path.Combine(customFolder, SERVER_SCRIPTS_FOLDER);
                return Directory.Exists(scriptsPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if server paths are stored in settings but no longer resolve on disk.
        /// Indicates a previously linked server that may have moved or been deleted.
        /// </summary>
        public static bool IsLinkBroken()
        {
            var customFolder = Settings.ScriptsCustomFolder;
            var dataFolder = Settings.ServerDataFolder;

            // Only broken when paths are stored but fail the configured check
            if (string.IsNullOrEmpty(customFolder) || string.IsNullOrEmpty(dataFolder))
                return false;

            return !IsServerConfigured();
        }

        /// <summary>
        /// Links the editor to a server where UORespawnServer scripts are already installed.
        /// Use when scripts were deployed manually or by a previous Install action.
        /// Does NOT copy scripts — validates they exist, then creates data exchange folders.
        /// </summary>
        public static (bool success, string message) LinkOnlySetup(
            string customFolderPath, string serverDataFolderPath)
        {
            try
            {
                var customValidation = ValidateCustomFolder(customFolderPath);
                if (!customValidation.IsValid)
                    return (false, customValidation.Message);

                var dataValidation = ValidateDataFolder(serverDataFolderPath);
                if (!dataValidation.IsValid)
                    return (false, dataValidation.Message);

                // Scripts must already be present — Link does not install
                var scriptsPath = Path.Combine(customFolderPath, SERVER_SCRIPTS_FOLDER);
                if (!Directory.Exists(scriptsPath))
                    return (false, "UORespawnServer scripts not found in that folder. Use Install to deploy them first.");

                // Create data exchange subfolders if they don't exist
                var dataPath = Path.Combine(serverDataFolderPath, SERVER_DATA_FOLDER);
                if (!Directory.Exists(dataPath))
                    Directory.CreateDirectory(dataPath);

                string[] subfolders = ["INPUT", "OUTPUT", "COMMANDS", "STATS", "SYS"];
                foreach (var subfolder in subfolders)
                {
                    var subPath = Path.Combine(dataPath, subfolder);
                    if (!Directory.Exists(subPath))
                        Directory.CreateDirectory(subPath);
                }

                Logger.Info($"Server linked (link-only): Custom={customFolderPath} | Data={serverDataFolderPath}");
                return (true, "Server linked successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error("Error during link-only setup", ex);
                return (false, $"Link failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to repair a broken link by walking up parent directories of the stored
        /// Custom scripts path to find where UORespawnServer/ actually lives.
        /// Common cause: user previously selected UORespawnServer/ itself instead of Custom/.
        /// Updates Settings.ScriptsCustomFolder if repair succeeds.
        /// Returns true if the link was successfully repaired.
        /// </summary>
        public static bool TryRepairLink()
        {
            var customFolder = Settings.ScriptsCustomFolder;
            if (string.IsNullOrEmpty(customFolder)) return false;

            try
            {
                var dir = new DirectoryInfo(customFolder);

                // Walk up to 2 parent levels to find a directory containing UORespawnServer/
                for (int i = 0; i < 2; i++)
                {
                    if (dir.Parent == null || !dir.Parent.Exists) break;
                    dir = dir.Parent;

                    var testScripts = Path.Combine(dir.FullName, SERVER_SCRIPTS_FOLDER);
                    if (Directory.Exists(testScripts))
                    {
                        Settings.ScriptsCustomFolder = dir.FullName;
                        Logger.Info($"TryRepairLink: repaired custom folder → {dir.FullName}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TryRepairLink error", ex);
            }

            return false;
        }

        /// <summary>
        /// Unlink server integration - clears settings only, preserves installed scripts
        /// </summary>
        public static (bool success, string message) UnlinkServer()
        {
            try
            {
                Logger.Info($"Server unlinked (scripts preserved): Custom={Settings.ScriptsCustomFolder}");

                Settings.ScriptsCustomFolder = string.Empty;
                Settings.ServerDataFolder = string.Empty;

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
        public static string GetServerScriptsPath()
        {
            return Path.Combine(Settings.ScriptsCustomFolder, SERVER_SCRIPTS_FOLDER);
        }

        /// <summary>
        /// Get the path where server data is stored
        /// </summary>
        public static string GetServerDataPath()
        {
            return Path.Combine(Settings.ServerDataFolder, SERVER_DATA_FOLDER);
        }

        /// <summary>
        /// Ensures server data folders exist without reinstalling scripts.
        /// Safe to call even when server scripts are custom/modified.
        /// Used when version matches but we want to ensure folder structure.
        /// </summary>
        public static void EnsureDataFoldersExist()
        {
            var dataFolder = Settings.ServerDataFolder;
            if (string.IsNullOrEmpty(dataFolder))
                return;

            try
            {
                SetupServerDataFolders(dataFolder);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error ensuring data folders exist: {ex.Message}");
            }
        }

        #endregion

        #region Legacy Compatibility (Deprecated Methods)

        /// <summary>
        /// [DEPRECATED] Use FullServerSetup instead.
        /// Setup server-side scripts using explicit folder paths.
        /// </summary>
        [Obsolete("Use FullServerSetup instead")]
        public static (bool success, string message) SetupServerScripts(string? scriptsFolderPath)
        {
            if (string.IsNullOrEmpty(scriptsFolderPath))
                return (false, "Scripts folder not found");

            return FullServerSetup(scriptsFolderPath, Settings.ServerDataFolder);
        }

        /// <summary>
        /// [DEPRECATED] Use CleanupLegacyInstallations instead.
        /// Delete existing UORespawn installation before fresh install.
        /// </summary>
        [Obsolete("Use CleanupLegacyInstallations instead")]
        public static bool CleanupExistingInstallation(string? scriptsFolderPath)
        {
            return CleanupLegacyInstallations(scriptsFolderPath, Settings.ServerDataFolder) >= 0;
        }

        #endregion
    }
}
