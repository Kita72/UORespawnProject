using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services;

/// <summary>
/// Service for managing server script update notifications and user decisions.
/// 
/// PHILOSOPHY: Never auto-delete or replace user's server scripts without confirmation.
/// The editor should inform the user and let them decide when to update.
/// 
/// Flow:
/// 1. On startup, CheckForServerUpdate() is called
/// 2. If version mismatch detected, OnServerUpdateAvailable event is raised
/// 3. UI shows ServerUpdateModal with options
/// 4. User can: Accept (update now), Decline (skip this session), or Skip Until Next Version
/// 
/// The editor continues to work normally regardless of server version mismatch.
/// Scripts on server may be older/newer - that's the user's choice.
/// </summary>
public class ServerUpdateService
{
    /// <summary>
    /// Information about a pending server update
    /// </summary>
    public class ServerUpdateInfo
    {
        public string CustomFolderPath { get; set; } = "";
        public string ServerDataFolderPath { get; set; } = "";
        public string InstalledVersion { get; set; } = "";
        public string EditorVersion { get; set; } = Utility.Version;
        public bool IsUpgrade => CompareVersions(EditorVersion, InstalledVersion) > 0;
        public bool IsDowngrade => CompareVersions(EditorVersion, InstalledVersion) < 0;
        public string? ScriptsPath { get; set; }
        public string? DataPath { get; set; }

        /// <summary>
        /// Compare two version strings. Returns positive if v1 > v2, negative if v1 < v2, 0 if equal.
        /// </summary>
        private static int CompareVersions(string v1, string v2)
        {
            try
            {
                var ver1 = new Version(v1);
                var ver2 = new Version(v2);
                return ver1.CompareTo(ver2);
            }
            catch
            {
                return string.Compare(v1, v2, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// Current pending update info (null if no update pending)
    /// </summary>
    public ServerUpdateInfo? PendingUpdate { get; private set; }

    /// <summary>
    /// Whether user has declined the update for this session
    /// </summary>
    public bool DeclinedForSession { get; private set; }

    /// <summary>
    /// Event raised when a server update is available and needs user confirmation.
    /// UI should show the ServerUpdateModal when this fires.
    /// </summary>
    public event EventHandler<ServerUpdateInfo>? OnServerUpdateAvailable;

    /// <summary>
    /// Event raised when user makes a decision about the update.
    /// </summary>
    public event EventHandler<bool>? OnUpdateDecisionMade;

    /// <summary>
    /// Check if server scripts need updating. Does NOT automatically update.
    /// Raises OnServerUpdateAvailable event if update is needed.
    /// Reads server paths from Settings.ScriptsCustomFolder and Settings.ServerDataFolder.
    /// </summary>
    /// <returns>True if check completed (regardless of update need), false if error</returns>
    public bool CheckForServerUpdate()
    {
        try
        {
            var customFolder = Settings.ScriptsCustomFolder;
            var dataFolder = Settings.ServerDataFolder;

            if (string.IsNullOrEmpty(customFolder))
            {
                Logger.Info("[ServerUpdate] No server linked - skipping update check");
                return true;
            }

            // Check installation status without auto-updating
            var status = ServerSetupUtility.CheckInstallation(customFolder, dataFolder);

            if (!status.IsInstalled)
            {
                // Server not installed - this is fine, will be handled when user links server
                Logger.Info("[ServerUpdate] Server scripts not installed - no update check needed");
                return true;
            }

            // Check if user has chosen to skip this version
            var skipVersion = Settings.SkipServerUpdateUntilVersion;
            if (!string.IsNullOrEmpty(skipVersion) && skipVersion == status.EditorVersion)
            {
                Logger.Info($"[ServerUpdate] User chose to skip update to v{status.EditorVersion}");
                return true;
            }

            if (status.NeedsUpdate)
            {
                // Version mismatch detected - raise event for UI to handle
                PendingUpdate = new ServerUpdateInfo
                {
                    CustomFolderPath = customFolder,
                    ServerDataFolderPath = dataFolder,
                    InstalledVersion = status.InstalledVersion ?? "unknown",
                    EditorVersion = status.EditorVersion,
                    ScriptsPath = status.ScriptsPath,
                    DataPath = status.DataPath
                };

                var direction = PendingUpdate.IsUpgrade ? "upgrade" : "downgrade";
                Logger.Info($"[ServerUpdate] {direction.ToUpperInvariant()} available: v{PendingUpdate.InstalledVersion} → v{PendingUpdate.EditorVersion}");

                // Raise event - UI will show confirmation modal
                OnServerUpdateAvailable?.Invoke(this, PendingUpdate);
            }
            else
            {
                Logger.Info($"[ServerUpdate] Server scripts up to date (v{status.InstalledVersion})");

                // Ensure data folders exist even when version matches
                ServerSetupUtility.EnsureDataFoldersExist();
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("[ServerUpdate] Error checking for server update", ex);
            return false;
        }
    }

    /// <summary>
    /// User accepted the update - perform the server update now.
    /// </summary>
    public (bool success, string message) AcceptUpdate()
    {
        if (PendingUpdate == null)
        {
            return (false, "No update pending");
        }

        try
        {
            Logger.Info($"[ServerUpdate] User accepted update - updating server scripts...");

            var serverTypeStr = Settings.ServerType;
            var serverType = serverTypeStr == "MUO"
                ? ServerSetupUtility.ServerType.MUO
                : ServerSetupUtility.ServerType.ServUO;

            // Perform the full server setup (this WILL replace scripts)
            var result = ServerSetupUtility.FullServerSetup(
                PendingUpdate.CustomFolderPath,
                PendingUpdate.ServerDataFolderPath,
                serverType,
                forceReinstall: true);

            if (result.success)
            {
                // Clear the skip preference since user just accepted an update
                Settings.SkipServerUpdateUntilVersion = "";
                Logger.Info($"[ServerUpdate] Server updated successfully to v{PendingUpdate.EditorVersion}");
            }
            else
            {
                Logger.Warning($"[ServerUpdate] Update failed: {result.message}");
            }

            // Clear pending update
            PendingUpdate = null;
            OnUpdateDecisionMade?.Invoke(this, true);

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error("[ServerUpdate] Error applying update", ex);
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// User declined the update for this session only.
    /// Update will be offered again on next app launch.
    /// </summary>
    public void DeclineForSession()
    {
        Logger.Info("[ServerUpdate] User declined update for this session");

        DeclinedForSession = true;
        PendingUpdate = null;
        OnUpdateDecisionMade?.Invoke(this, false);
    }

    /// <summary>
    /// User chose to skip this update until the editor version changes again.
    /// This prevents repeated prompts for the same version mismatch.
    /// </summary>
    public void SkipUntilNextVersion()
    {
        if (PendingUpdate != null)
        {
            Logger.Info($"[ServerUpdate] User chose to skip update until next editor version (current: v{PendingUpdate.EditorVersion})");
            Settings.SkipServerUpdateUntilVersion = PendingUpdate.EditorVersion;
        }

        DeclinedForSession = true;
        PendingUpdate = null;
        OnUpdateDecisionMade?.Invoke(this, false);
    }

    /// <summary>
    /// Clear any pending update (used when server is unlinked)
    /// </summary>
    public void ClearPendingUpdate()
    {
        PendingUpdate = null;
        DeclinedForSession = false;
    }
}
