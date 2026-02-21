using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Entities;

namespace UORespawnApp.Scripts.Utilities
{
    /// <summary>
    /// Utility to clean up region spawn data by validating against UOR_RegionList.txt
    /// Removes any spawn regions that don't exist in the official region list
    /// </summary>
    public static class RegionListUtility
    {
        /// <summary>
        /// Region name corrections: OldName -> CorrectName
        /// Applied BEFORE validation to fix typos/variations
        /// </summary>
        private static readonly Dictionary<string, string> NameCorrections = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Scholar's Inn", "The Scholar's Inn" },
            { "Lizardman's Huts", "Lizard Man's Huts" },
            { "Gravewater Lake", "Gravewater Lake [Underwater]" },
            { "Khaldun Camp Region", "Khaldun" },
            { "Pormir Reg", "Pormir Harm" },
        };

        /// <summary>
        /// Clean up all loaded region spawn data by validating against UOR_RegionList.txt
        /// Returns the number of corrections made and regions removed
        /// </summary>
        public static (int corrected, int removed) CleanupRegionSpawns()
        {
            int totalCorrected = 0;
            int totalRemoved = 0;

            try
            {
                // Safety check - if no region spawns loaded, nothing to clean
                if (Utility.RegionSpawns == null || Utility.RegionSpawns.Count == 0)
                {
                    Logger.Info("[Cleanup] No region spawns loaded - skipping cleanup");
                    return (0, 0);
                }

                // Load valid region names DIRECTLY from UOR_RegionList.txt
                var validRegionsByMap = LoadValidRegionsFromFile();

                if (validRegionsByMap.Count == 0)
                {
                    Logger.Warning("[Cleanup] Failed to load UOR_RegionList.txt - cannot validate regions!");
                    return (0, 0);
                }

                Logger.Info($"[Cleanup] Loaded {validRegionsByMap.Sum(kvp => kvp.Value.Count)} valid region names from UOR_RegionList.txt");

                foreach (var mapEntry in Utility.RegionSpawns.ToList())
                {
                    int mapId = mapEntry.Key;
                    var regions = mapEntry.Value;

                    if (regions == null || regions.Count == 0)
                        continue;

                    // Get valid names for this map
                    if (!validRegionsByMap.TryGetValue(mapId, out var validNames))
                    {
                        Logger.Warning($"[Cleanup] No valid regions defined for Map {mapId} in UOR_RegionList.txt");
                        validNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }

                    var toRemove = new List<RegionSpawnEntity>();

                    foreach (var region in regions)
                    {
                        if (region == null || string.IsNullOrEmpty(region.Name))
                            continue;

                        string originalName = region.Name;

                        // Step 1: Apply name corrections first
                        if (NameCorrections.TryGetValue(region.Name, out var correctedName))
                        {
                            Logger.Info($"[Cleanup] Correcting region name: '{region.Name}' -> '{correctedName}' on Map {mapId}");
                            region.Name = correctedName;
                            totalCorrected++;
                        }

                        // Step 2: Validate against the authoritative region list
                        if (!validNames.Contains(region.Name))
                        {
                            Logger.Warning($"[Cleanup] INVALID region '{originalName}' (corrected: '{region.Name}') not in UOR_RegionList.txt for Map {mapId} - REMOVING");
                            toRemove.Add(region);
                        }
                    }

                    // Remove invalid regions
                    foreach (var region in toRemove)
                    {
                        regions.Remove(region);
                        totalRemoved++;
                    }
                }

                Logger.Info($"[Cleanup] Region spawn cleanup complete: {totalCorrected} corrected, {totalRemoved} removed");
            }
            catch (Exception ex)
            {
                Logger.Error("[Cleanup] Error during region spawn cleanup", ex);
            }

            return (totalCorrected, totalRemoved);
        }

        /// <summary>
        /// Load valid region names DIRECTLY from UOR_RegionList.txt file
        /// Returns Dictionary: MapId -> HashSet of valid region names (case-insensitive)
        /// </summary>
        private static Dictionary<int, HashSet<string>> LoadValidRegionsFromFile()
        {
            var result = new Dictionary<int, HashSet<string>>();

            try
            {
                // Try multiple paths to find the region list
                string? filePath = FindRegionListFile();

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Logger.Warning("[Cleanup] UOR_RegionList.txt not found in any expected location");
                    return result;
                }

                Logger.Info($"[Cleanup] Loading region list from: {filePath}");

                var lines = File.ReadAllLines(filePath);
                int linesParsed = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                        continue;

                    // Parse format: MapID:RegionName:(X,Y,W,H)
                    var parsed = ParseRegionLine(line);
                    if (parsed.HasValue)
                    {
                        var (mapId, regionName) = parsed.Value;

                        if (!result.TryGetValue(mapId, out var regionSet))
                        {
                            regionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            result[mapId] = regionSet;
                        }

                        regionSet.Add(regionName);
                        linesParsed++;
                    }
                }

                Logger.Info($"[Cleanup] Parsed {linesParsed} region entries from {lines.Length} lines");
            }
            catch (Exception ex)
            {
                Logger.Error($"[Cleanup] Error loading UOR_RegionList.txt: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Find the UOR_RegionList.txt file in expected locations
        /// </summary>
        private static string? FindRegionListFile()
        {
            var serverFolder = Settings.ServUODataFolder;

            var possiblePaths = new[]
            {
                // Check in linked server's Data folder first (most authoritative)
                !string.IsNullOrEmpty(serverFolder) ? Path.Combine(serverFolder, "UOR_RegionList.txt") : "",

                // Check in app's local data folder
                Path.Combine(PathConstants.LocalDataPath, "UOR_RegionList.txt"),

                // Check in Resources/Raw (bundled with app)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", "UOR_RegionList.txt"),

                // Alternative bundled location (MAUI on Windows)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UOR_RegionList.txt"),

                // Debug/dev location
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Raw", "UOR_RegionList.txt"),
            };

            foreach (var path in possiblePaths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// Parse a single region line from UOR_RegionList.txt
        /// Format: MapID:RegionName:(X,Y,W,H)
        /// Example: 0:Britain:(1416,1498,324,279)
        /// Returns (mapId, regionName) or null if invalid
        /// </summary>
        private static (int mapId, string name)? ParseRegionLine(string line)
        {
            try
            {
                // Split by colon - format is MapID:RegionName:(X,Y,W,H)
                var parts = line.Split(':');
                if (parts.Length < 3)
                    return null;

                // Parse MapID (first part)
                if (!int.TryParse(parts[0], out int mapId))
                    return null;

                // Parse Region Name (everything between first and last colon)
                // This handles region names that might contain colons
                var regionName = string.Join(":", parts.Skip(1).Take(parts.Length - 2));

                if (string.IsNullOrWhiteSpace(regionName))
                    return null;

                return (mapId, regionName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if a specific region name is valid for a given map
        /// </summary>
        public static bool IsValidRegion(int mapId, string regionName)
        {
            var validRegions = LoadValidRegionsFromFile();

            // Apply name correction first
            string nameToCheck = regionName;
            if (NameCorrections.TryGetValue(regionName, out var correctedName))
            {
                nameToCheck = correctedName;
            }

            if (validRegions.TryGetValue(mapId, out var validNames))
            {
                return validNames.Contains(nameToCheck);
            }

            return false;
        }

        /// <summary>
        /// Get the corrected name for a region (or the original if no correction needed)
        /// </summary>
        public static string GetCorrectedName(string regionName)
        {
            return NameCorrections.TryGetValue(regionName, out var corrected) ? corrected : regionName;
        }
    }
}
