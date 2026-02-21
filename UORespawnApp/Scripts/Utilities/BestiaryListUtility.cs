using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Utility for loading and managing the bestiary list (creature names).
    /// The bestiary is a READ-ONLY list of valid creature class names from the server.
    /// 
    /// Data Flow:
    /// - Default: Loads from Resources/Raw/UOR_BestiaryList.txt (bundled with app)
    /// - Server Linked: DataWatcher copies server's bestiary to Resources/Raw, then reloads
    /// - Users cannot edit the bestiary - it's a verified list from the server
    /// </summary>
    internal static class BestiaryListUtility
    {
        internal static List<string>? BestiaryNameList { get; private set; }
        private static bool _isLoaded = false;

        /// <summary>
        /// Clear spawn list to force reload from file.
        /// Called by DataWatcher when server updates the bestiary file.
        /// </summary>
        internal static void ClearSpawnList()
        {
            BestiaryNameList?.Clear();
            _isLoaded = false;
        }

        /// <summary>
        /// Load the bestiary from Resources/Raw/UOR_BestiaryList.txt
        /// </summary>
        internal static async Task LoadSpawnList()
        {
            if (_isLoaded && BestiaryNameList != null && BestiaryNameList.Count > 0)
            {
                return; // Already loaded
            }

            BestiaryNameList = [];

            try
            {
                var filePath = PathConstants.GetBestiaryFilePath();

                if (!File.Exists(filePath))
                {
                    Logger.Warning($"Bestiary file not found at: {filePath}");
                    _isLoaded = true;
                    return;
                }

                var lines = await File.ReadAllLinesAsync(filePath);

                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
                    {
                        BestiaryNameList.Add(line.Trim());
                    }
                }

                BestiaryNameList.Sort();
                _isLoaded = true;

                Logger.Info($"Loaded {BestiaryNameList.Count} creatures from bestiary file");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading bestiary list", ex);
                _isLoaded = true; // Mark as loaded to prevent repeated attempts
            }
        }
    }
}
