using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Utility class for map-related operations
    /// 
    /// MAP STORAGE ARCHITECTURE:
    /// - Location: Data/maps/ folder (centralized via PathConstants.MapsPath)
    /// - Format: BMP images named Map0.bmp, Map1.bmp, etc.
    /// - Standard Maps (0-5): Felucca, Trammel, Ilshenar, Malas, Tokuno, Ter Mur
    /// - Custom Maps (6+): Supported - add Map6.bmp, Map7.bmp, etc. to Data/maps/
    /// 
    /// MAP USAGE FLOW:
    /// 1. User adds/replaces map via Settings page → Saves to Data/maps/ (.bmp files)
    /// 2. Map display components (Box/Region Spawn) call Utility.GetMapImagePath()
    /// 3. Utility loads from Data/maps/ and converts to base64 data URL for Blazor WebView
    /// 4. Base64 approach allows Blazor to display images from file system without wwwroot
    /// 
    /// WHY Data/maps/ NOT wwwroot/maps/:
    /// - Data folder is user-accessible and easy to browse/edit directly
    /// - Keeps all user data (spawn files, maps, bestiary) in one Data folder
    /// - Avoids mixing user content with application web assets (wwwroot)
    /// - Server admin can copy entire Data folder between editor and server
    /// </summary>
    public static class MapUtility
    {
        /// <summary>
        /// Gets the friendly name for a map ID.
        /// Uses MapListUtility for file-based map names (supports custom maps from server).
        /// </summary>
        /// <param name="mapId">The map ID (0-5 for standard maps, 6+ for custom)</param>
        /// <returns>Friendly name (e.g., "Felucca") or generic "Map X" for unknown maps</returns>
        public static string GetMapName(int mapId)
        {
            // Use MapListUtility which loads from file (supports custom maps)
            var name = MapListUtility.GetMapName(mapId);

            // If MapListUtility returns "Unknown", provide a generic name
            if (name == "Unknown")
            {
                return $"Map {mapId}";
            }

            return name;
        }

        /// <summary>
        /// Gets list of available map IDs by scanning for map images in Data/maps/ folder
        /// </summary>
        /// <returns>List of map IDs that have corresponding image files</returns>
        public static List<int> GetAvailableMaps()
        {
            var mapIds = new List<int>();

            try
            {
                string mapsFolder = PathConstants.MapsPath;

                if (Directory.Exists(mapsFolder))
                {
                    var mapFiles = Directory.GetFiles(mapsFolder, "Map*.bmp");
                    mapIds = [.. mapFiles
                        .Select(f => Path.GetFileNameWithoutExtension(f))
                        .Where(n => n.StartsWith("Map") && int.TryParse(n.AsSpan(3), out _))
                        .Select(n => int.Parse(n[3..]))
                        .OrderBy(id => id)];
                }

                // Fallback to standard maps (0-5) if folder doesn't exist or is empty
                if (mapIds.Count == 0)
                {
                    mapIds = [.. Enumerable.Range(0, 6)];
                    Logger.Warning("No map images found, using standard map list (0-5)");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting available maps", ex);
                // Fallback to standard maps
                mapIds = [.. Enumerable.Range(0, 6)];
            }

            return mapIds;
        }

        /// <summary>
        /// Converts map name string to map ID.
        /// Uses MapListUtility for dynamic lookups (supports custom maps from server).
        /// </summary>
        /// <param name="name">Map name (e.g., "Felucca", "Map0", "3")</param>
        /// <returns>Map ID or 0 if invalid</returns>
        public static int ParseMapName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return 0;

            // Try parsing as number first
            if (int.TryParse(name, out int id))
                return id;

            // Try "MapX" format
            if (name.StartsWith("Map", StringComparison.OrdinalIgnoreCase) && 
                int.TryParse(name.AsSpan(3), out int mapIdFromFormat))
            {
                return mapIdFromFormat;
            }

            // Search in MapListUtility for matching name
            var mapList = MapListUtility.MapList;
            if (mapList != null)
            {
                foreach (var entry in mapList)
                {
                    if (entry.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.Key;
                    }
                    // Also try without spaces (e.g., "TerMur" matches "Ter Mur")
                    if (entry.Value.Replace(" ", "").Equals(name.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.Key;
                    }
                }
            }

            return 0; // Default to Felucca
        }

        /// <summary>
        /// Checks if a map ID is valid (has a corresponding image file in Data/maps/)
        /// </summary>
        /// <param name="mapId">The map ID to check</param>
        /// <returns>True if the map image exists</returns>
        public static bool IsValidMapId(int mapId)
        {
            if (mapId < 0)
                return false;

            string imagePath = Path.Combine(PathConstants.MapsPath, $"Map{mapId}.bmp");
            return File.Exists(imagePath);
        }

        /// <summary>
        /// Gets the full path to a map image from Data/maps/ folder
        /// </summary>
        /// <param name="mapId">The map ID</param>
        /// <returns">Full path to the map image in Data/maps/</returns>
        public static string GetMapImagePath(int mapId)
        {
            return Path.Combine(PathConstants.MapsPath, $"Map{mapId}.bmp");
        }
    }
}
