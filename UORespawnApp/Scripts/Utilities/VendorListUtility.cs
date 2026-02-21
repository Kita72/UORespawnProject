using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Utility for loading and managing the vendor list (valid vendor class names).
    /// The vendor list is a READ-ONLY list of approved vendor types from the server.
    /// 
    /// Data Flow:
    /// - Default: Loads from Resources/Raw/UOR_VendorList.txt (bundled with app)
    /// - Server Linked: DataWatcher copies server's vendor list to Resources/Raw, then reloads
    /// - Users cannot edit the vendor list - it's a verified list from the server
    /// </summary>
    internal static class VendorListUtility
    {
        internal static List<string>? VendorNameList { get; private set; }
        private static bool _isLoaded = false;

        /// <summary>
        /// Clear vendor list to force reload from file.
        /// Called by DataWatcher when server updates the vendor list file.
        /// </summary>
        internal static void ClearVendorList()
        {
            VendorNameList?.Clear();
            _isLoaded = false;
        }

        /// <summary>
        /// Load the vendor list from Resources/Raw/UOR_VendorList.txt
        /// </summary>
        internal static async Task LoadVendorList()
        {
            if (_isLoaded && VendorNameList != null && VendorNameList.Count > 0)
            {
                return; // Already loaded
            }

            VendorNameList = [];

            try
            {
                var filePath = PathConstants.GetVendorListFilePath();

                if (!File.Exists(filePath))
                {
                    Logger.Warning($"Vendor list file not found at: {filePath}");
                    _isLoaded = true;
                    return;
                }

                var lines = await File.ReadAllLinesAsync(filePath);

                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
                    {
                        VendorNameList.Add(line.Trim());
                    }
                }

                VendorNameList.Sort();
                _isLoaded = true;

                Logger.Info($"Loaded {VendorNameList.Count} vendors from vendor list file");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading vendor list", ex);
                _isLoaded = true; // Mark as loaded to prevent repeated attempts
            }
        }
    }
}
