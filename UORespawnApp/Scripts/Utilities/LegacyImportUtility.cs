using UORespawnApp.Scripts.Utilities;
using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.DTO.Enums;

namespace UORespawnApp
{
    /// <summary>
    /// Utility for importing legacy UORespawn v1.0 CSV data to v2.0 format
    /// Converts old SpawnEntity format (3 frequencies) to new format (6 frequencies)
    /// Import is available per spawn type (Box or Tile can be imported independently)
    /// </summary>
    public static class LegacyImportUtility
    {
        /// <summary>
        /// Check if legacy import section should be shown (at least one spawn type is empty)
        /// </summary>
        public static bool IsLegacyImportAvailable()
        {
            return IsBoxImportAvailable() || IsTileImportAvailable();
        }

        /// <summary>
        /// Check if Box spawn import is available (Box spawns must be empty)
        /// </summary>
        public static bool IsBoxImportAvailable()
        {
            return Utility.BoxSpawns == null || 
                   Utility.BoxSpawns.Count == 0 ||
                   Utility.BoxSpawns.All(kvp => kvp.Value.Count == 0);
        }

        /// <summary>
        /// Check if Tile spawn import is available (Tile spawns must be empty)
        /// </summary>
        public static bool IsTileImportAvailable()
        {
            return Utility.TileSpawns == null || 
                   Utility.TileSpawns.Count == 0 ||
                   Utility.TileSpawns.All(kvp => kvp.Value.Count == 0);
        }

        /// <summary>
        /// Import legacy Box Spawn CSV file and convert to v2.0 format
        /// Returns (success, message, importedCount)
        /// </summary>
        public static (bool success, string message, int importedCount) ImportLegacyBoxSpawns(string csvFilePath)
        {
            try
            {
                if (!File.Exists(csvFilePath))
                {
                    return (false, "File not found.", 0);
                }

                var lines = File.ReadAllLines(csvFilePath);
                if (lines.Length == 0)
                {
                    return (false, "File is empty.", 0);
                }

                int importedCount = 0;
                var groupedByMap = new Dictionary<int, List<LegacySpawnData>>();

                // Parse CSV lines (skip header if present)
                int startLine = lines[0].Contains("Position") || lines[0].Contains("SpawnBox") ? 1 : 0;

                for (int i = startLine; i < lines.Length; i++)
                {
                    var legacyData = ParseLegacySpawnLine(lines[i]);
                    if (legacyData != null)
                    {
                        int mapId = legacyData.MapId;
                        if (!groupedByMap.TryGetValue(mapId, out List<LegacySpawnData>? value))
                        {
                            value = [];
                            groupedByMap[mapId] = value;
                        }

                        value.Add(legacyData);
                    }
                }

                // Convert to new format
                foreach (var kvp in groupedByMap)
                {
                    int mapId = kvp.Key;
                    var legacySpawns = kvp.Value;

                    if (!Utility.BoxSpawns.TryGetValue(mapId, out List<BoxSpawnEntity>? newSpawns))
                    {
                        newSpawns = [];
                        Utility.BoxSpawns[mapId] = newSpawns;
                    }

                    foreach (var legacy in legacySpawns.OrderBy(s => s.Position))
                    {
                        var newSpawn = ConvertToBoxSpawnEntity(legacy);
                        newSpawns.Add(newSpawn);
                        importedCount++;
                    }
                }

                // Save the imported data
                Utility.SaveSpawnData();

                return (true, $"Successfully imported {importedCount} Box Spawn(s) from legacy CSV.\n" +
                             $"All spawns have been mapped to Common/Uncommon/Rare categories.\n" +
                             $"You can now add Water/Weather spawns as needed.", importedCount);
            }
            catch (Exception ex)
            {
                Logger.Error("Legacy Box Spawn import failed", ex);
                return (false, $"Import failed: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Import legacy Tile Spawn CSV file and convert to v2.0 format
        /// Returns (success, message, importedCount)
        /// </summary>
        public static (bool success, string message, int importedCount) ImportLegacyTileSpawns(string csvFilePath)
        {
            try
            {
                if (!File.Exists(csvFilePath))
                {
                    return (false, "File not found.", 0);
                }

                var lines = File.ReadAllLines(csvFilePath);
                if (lines.Length == 0)
                {
                    return (false, "File is empty.", 0);
                }

                int importedCount = 0;
                var groupedByMap = new Dictionary<int, List<LegacySpawnData>>();

                // Parse CSV lines (skip header if present)
                int startLine = lines[0].Contains("Position") || lines[0].Contains("TileId") ? 1 : 0;

                for (int i = startLine; i < lines.Length; i++)
                {
                    var legacyData = ParseLegacyTileSpawnLine(lines[i]);
                    if (legacyData != null)
                    {
                        int mapId = legacyData.MapId;
                        if (!groupedByMap.TryGetValue(mapId, out List<LegacySpawnData>? value))
                        {
                            value = [];
                            groupedByMap[mapId] = value;
                        }

                        value.Add(legacyData);
                    }
                }

                // Convert to new format
                foreach (var kvp in groupedByMap)
                {
                    int mapId = kvp.Key;
                    var legacySpawns = kvp.Value;

                    if (!Utility.TileSpawns.TryGetValue(mapId, out List<TileSpawnEntity>? newSpawns))
                    {
                        newSpawns = [];
                        Utility.TileSpawns[mapId] = newSpawns;
                    }

                    foreach (var legacy in legacySpawns.OrderBy(s => s.Position))
                    {
                        var newSpawn = ConvertToTileSpawnEntity(legacy);
                        newSpawns.Add(newSpawn);
                        importedCount++;
                    }
                }

                // Save the imported data
                Utility.SaveTileSpawnData();

                return (true, $"Successfully imported {importedCount} Tile Spawn(s) from legacy CSV.\n" +
                             $"All spawns have been mapped to Common/Uncommon/Rare categories.\n" +
                             $"You can now add Water/Weather spawns as needed.", importedCount);
            }
            catch (Exception ex)
            {
                Logger.Error("Legacy Tile Spawn import failed", ex);
                return (false, $"Import failed: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Parse a legacy Box Spawn CSV line
        /// Format: MapId,Position,X,Y,Width,Height,Priority,TimedSpawn,Common1;Common2,Uncommon1;Uncommon2,Rare1;Rare2
        /// </summary>
        private static LegacySpawnData? ParseLegacySpawnLine(string line)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                    return null;

                var parts = line.Split(',');
                if (parts.Length < 8)
                    return null;

                var data = new LegacySpawnData
                {
                    MapId = int.Parse(parts[0]),
                    Position = int.Parse(parts[1]),
                    SpawnBox = new Rect(
                        int.Parse(parts[2]),
                        int.Parse(parts[3]),
                        int.Parse(parts[4]),
                        int.Parse(parts[5])
                    ),
                    Priority = int.Parse(parts[6]),
                    TimedSpawn = parts[7]
                };

                // Parse spawn lists (semicolon-separated)
                if (parts.Length > 8 && !string.IsNullOrWhiteSpace(parts[8]))
                {
                    data.CommonSpawns = [.. parts[8].Split(';', StringSplitOptions.RemoveEmptyEntries)];
                }
                if (parts.Length > 9 && !string.IsNullOrWhiteSpace(parts[9]))
                {
                    data.UncommonSpawns = [.. parts[9].Split(';', StringSplitOptions.RemoveEmptyEntries)];
                }
                if (parts.Length > 10 && !string.IsNullOrWhiteSpace(parts[10]))
                {
                    data.RareSpawns = [.. parts[10].Split(';', StringSplitOptions.RemoveEmptyEntries)];
                }

                return data;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to parse legacy spawn line: {line} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse a legacy Tile Spawn CSV line
        /// Format: MapId,Position,TileId,X,Y,Z,Priority,TimedSpawn,Common1;Common2,Uncommon1;Uncommon2,Rare1;Rare2
        /// </summary>
        private static LegacySpawnData? ParseLegacyTileSpawnLine(string line)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                    return null;

                var parts = line.Split(',');
                if (parts.Length < 9)
                    return null;

                var data = new LegacySpawnData
                {
                    MapId = int.Parse(parts[0]),
                    Position = int.Parse(parts[1]),
                    TileId = int.Parse(parts[2]),
                    X = int.Parse(parts[3]),
                    Y = int.Parse(parts[4]),
                    Z = int.Parse(parts[5]),
                    Priority = int.Parse(parts[6]),
                    TimedSpawn = parts[7]
                };

                // Parse spawn lists (semicolon-separated)
                if (parts.Length > 8 && !string.IsNullOrWhiteSpace(parts[8]))
                {
                    data.CommonSpawns = [.. parts[8].Split(';', StringSplitOptions.RemoveEmptyEntries)];
                }
                if (parts.Length > 9 && !string.IsNullOrWhiteSpace(parts[9]))
                {
                    data.UncommonSpawns = [.. parts[9].Split(';', StringSplitOptions.RemoveEmptyEntries)];
                }
                if (parts.Length > 10 && !string.IsNullOrWhiteSpace(parts[10]))
                {
                    data.RareSpawns = [.. parts[10].Split(';', StringSplitOptions.RemoveEmptyEntries)];
                }

                return data;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to parse legacy tile spawn line: {line} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert legacy spawn data to new BoxSpawnEntity v2.0 format
        /// </summary>
        private static BoxSpawnEntity ConvertToBoxSpawnEntity(LegacySpawnData legacy)
        {
            var entity = new BoxSpawnEntity
            {
                Position = legacy.Position,
                MapId = legacy.MapId,
                SpawnBox = legacy.SpawnBox,
                Priority = legacy.Priority,
                
                // Map old 3-category system to new 6-category system
                CommonSpawns = [.. legacy.CommonSpawns],
                UncommonSpawns = [.. legacy.UncommonSpawns],
                RareSpawns = [.. legacy.RareSpawns],
                
                // New v2.0 categories start empty (user can add later)
                WaterSpawns = [],
                WeatherSpawns = [],
                TimedSpawns = [],
                
                // Convert old TimedSpawn string to new enum
                TimedSpawn = ConvertTimedSpawnString(legacy.TimedSpawn),
                WeatherSpawn = WeatherTypes.None
            };

            return entity;
        }

        /// <summary>
        /// Convert legacy spawn data to new TileSpawnEntity v2.0 format
        /// Note: v2.0 uses tile type names instead of individual tile positions
        /// This creates a tile spawn entry named after the position for backwards compatibility
        /// </summary>
        private static TileSpawnEntity ConvertToTileSpawnEntity(LegacySpawnData legacy)
        {
            var entity = new TileSpawnEntity
            {
                Id = legacy.Position,
                // Use tile ID or position as name for backwards compatibility
                Name = $"Tile{legacy.TileId}",
                MapId = legacy.MapId,

                // Map old 3-category system to new 6-category system
                CommonSpawns = [.. legacy.CommonSpawns],
                UncommonSpawns = [.. legacy.UncommonSpawns],
                RareSpawns = [.. legacy.RareSpawns],

                // New v2.0 categories start empty (user can add later)
                WaterSpawns = [],
                WeatherSpawns = [],
                TimedSpawns = [],

                // Convert old TimedSpawn string to new enum
                TimedSpawn = ConvertTimedSpawnString(legacy.TimedSpawn),
                WeatherSpawn = WeatherTypes.None
            };

            return entity;
        }

        /// <summary>
        /// Convert old TimedSpawn string to new TimeNames enum
        /// Old format: "None", "Witching_Hour", "Middle_of_Night", etc.
        /// </summary>
        private static TimeNames ConvertTimedSpawnString(string timedSpawn)
        {
            if (string.IsNullOrWhiteSpace(timedSpawn) || timedSpawn.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                return TimeNames.None;
            }

            // Try to parse as enum
            if (Enum.TryParse<TimeNames>(timedSpawn, true, out TimeNames result))
            {
                return result;
            }

            Logger.Warning($"Could not convert TimedSpawn value '{timedSpawn}' to TimeNames enum, using None");
            return TimeNames.None;
        }

        /// <summary>
        /// Helper class to hold legacy spawn data during parsing
        /// </summary>
        private class LegacySpawnData
        {
            public int MapId { get; set; }
            public int Position { get; set; }
            public Rect SpawnBox { get; set; }
            public int TileId { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
            public int Priority { get; set; }
            public string TimedSpawn { get; set; } = "None";
            public List<string> CommonSpawns { get; set; } = [];
            public List<string> UncommonSpawns { get; set; } = [];
            public List<string> RareSpawns { get; set; } = [];
        }
    }
}
