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
        private static readonly SemaphoreSlim _loadLock = new(1, 1);

        /// <summary>
        /// Clear bestiary list to force reload from file.
        /// Called by DataWatcher when server updates the bestiary file.
        /// </summary>
        internal static void ClearBestiaryList()
        {
            _loadLock.Wait();
            try
            {
                BestiaryNameList?.Clear();
                _isLoaded = false;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        /// <summary>
        /// Load the bestiary from Resources/Raw/UOR_BestiaryList.txt
        /// </summary>
        internal static async Task LoadBestiaryList(CancellationToken cancellationToken = default)
        {
            if (_isLoaded && BestiaryNameList != null && BestiaryNameList.Count > 0)
            {
                return;
            }

            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                if (_isLoaded && BestiaryNameList != null && BestiaryNameList.Count > 0)
                {
                    return;
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

                    var lines = await FileUtility.ReadAllLinesAsync(filePath, cancellationToken: cancellationToken);

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
                    _isLoaded = true;
                }
            }
            finally
            {
                _loadLock.Release();
            }
        }
    }
}
