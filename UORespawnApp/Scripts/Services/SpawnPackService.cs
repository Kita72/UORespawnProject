using System.IO.Compression;
using System.Text.Json;
using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    /// <summary>
    /// Loads spawn packs from Data/PACKS and provides stats and apply workflows.
    /// </summary>
    public class SpawnPackService
    {
        private const string PackManifestFileName = "pack.json";
        private static readonly string[] PackDataFiles =
        [
            PathConstants.SETTINGS_FILENAME,
            PathConstants.BOX_FILENAME,
            PathConstants.TILE_FILENAME,
            PathConstants.REGION_FILENAME
        ];

        /// <summary>
        /// Load approved packs only (packs with IsApproved = true in manifest)
        /// </summary>
        public List<SpawnPackInfo> LoadApprovedPacks(bool includeMock = true)
        {
            var packs = LoadAllPacks().Where(p => p.Metadata.IsApproved).ToList();

            if (includeMock && packs.Count == 0)
            {
                packs.Add(CreateMockPack());
            }

            return packs.OrderBy(p => p.Metadata.Name).ToList();
        }

        /// <summary>
        /// Load imported/user packs only (packs with IsApproved = false)
        /// </summary>
        public List<SpawnPackInfo> LoadImportedPacks()
        {
            return LoadAllPacks()
                .Where(p => !p.Metadata.IsApproved && !string.IsNullOrWhiteSpace(p.PackFolderPath))
                .OrderBy(p => p.Metadata.Name)
                .ToList();
        }

        /// <summary>
        /// Load all packs from Data/PACKS folder
        /// </summary>
        public List<SpawnPackInfo> LoadAllPacks()
        {
            var packs = new List<SpawnPackInfo>();
            var packsPath = PathConstants.PacksPath;

            if (Directory.Exists(packsPath))
            {
                foreach (var packFolder in Directory.GetDirectories(packsPath))
                {
                    var pack = LoadPackInfo(packFolder);
                    if (pack != null)
                    {
                        packs.Add(pack);
                    }
                }
            }

            return packs;
        }

        public List<SpawnPackInfo> LoadPacks(bool includeMock = true)
        {
            var packs = LoadAllPacks();

            if (includeMock && packs.Count == 0)
            {
                packs.Add(CreateMockPack());
            }

            return packs.OrderBy(p => p.Metadata.Name).ToList();
        }

        public SpawnPackInfo? LoadPackInfo(string packFolder)
        {
            if (string.IsNullOrWhiteSpace(packFolder) || !Directory.Exists(packFolder))
            {
                return null;
            }

            var metadata = LoadPackMetadata(packFolder);
            metadata.Id = string.IsNullOrWhiteSpace(metadata.Id) ? Path.GetFileName(packFolder) : metadata.Id;
            metadata.Name = string.IsNullOrWhiteSpace(metadata.Name) ? metadata.Id : metadata.Name;
            if (!string.IsNullOrWhiteSpace(metadata.ImageFileName) && !Path.IsPathRooted(metadata.ImageFileName))
            {
                metadata.ImageFileName = Path.Combine(packFolder, metadata.ImageFileName);
            }

            var stats = ComputeStats(packFolder);

            return new SpawnPackInfo
            {
                Metadata = metadata,
                Stats = stats,
                PackFolderPath = packFolder
            };
        }

        public bool ApplyPack(SpawnPackInfo pack, bool reloadAfterApply = true)
        {
            if (pack == null || string.IsNullOrWhiteSpace(pack.PackFolderPath))
            {
                return false;
            }

            try
            {
                var packDataPath = ResolvePackDataPath(pack.PackFolderPath);
                if (packDataPath == null)
                {
                    Logger.Warning("Spawn pack does not contain any data files to apply.");
                    return false;
                }

                var destinationPath = PathConstants.LocalDataPath;

                foreach (var fileName in PackDataFiles)
                {
                    var sourceFile = Path.Combine(packDataPath, fileName);
                    if (!File.Exists(sourceFile))
                    {
                        continue;
                    }

                    var destinationFile = Path.Combine(destinationPath, fileName);
                    File.Copy(sourceFile, destinationFile, true);
                }

                if (reloadAfterApply)
                {
                    Utility.LoadSettings();
                    Utility.LoadSpawnData();
                    Utility.LoadTileSpawnData();
                    Utility.LoadRegionSpawnData();
                }

                Logger.Info($"Applied spawn pack: {pack.Metadata.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error applying spawn pack", ex);
                return false;
            }
        }

        /// <summary>
        /// Validates a zip file contains valid spawn pack data files.
        /// Returns validation result with list of found files and any errors.
        /// </summary>
        public (bool IsValid, string[] FoundFiles, string? Error) ValidateSpawnPackZip(string zipPath)
        {
            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            {
                return (false, [], "Zip file not found.");
            }

            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                var foundFiles = new List<string>();

                foreach (var entry in archive.Entries)
                {
                    var fileName = Path.GetFileName(entry.FullName);

                    // Check for spawn data files (at any level in the zip)
                    if (PackDataFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                    {
                        foundFiles.Add(fileName);
                    }
                }

                if (foundFiles.Count == 0)
                {
                    return (false, [], "No valid spawn data files found. Expected files: " + string.Join(", ", PackDataFiles));
                }

                // Must have at least one spawn file (box, tile, or region)
                var hasSpawnFile = foundFiles.Any(f => 
                    f.Equals(PathConstants.BOX_FILENAME, StringComparison.OrdinalIgnoreCase) ||
                    f.Equals(PathConstants.TILE_FILENAME, StringComparison.OrdinalIgnoreCase) ||
                    f.Equals(PathConstants.REGION_FILENAME, StringComparison.OrdinalIgnoreCase));

                if (!hasSpawnFile)
                {
                    return (false, [.. foundFiles], "Zip must contain at least one spawn file (BoxSpawns, TileSpawns, or RegionSpawns).");
                }

                return (true, [.. foundFiles], null);
            }
            catch (InvalidDataException)
            {
                return (false, [], "Invalid or corrupted zip file.");
            }
            catch (Exception ex)
            {
                return (false, [], $"Error reading zip: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes an imported spawn pack. Only allows deletion of non-approved packs.
        /// </summary>
        public (bool Success, string? Error) DeleteImportedPack(SpawnPackInfo pack)
        {
            if (pack == null)
            {
                return (false, "Pack not specified.");
            }

            // Safety check - never delete approved packs
            if (pack.Metadata.IsApproved)
            {
                return (false, "Cannot delete approved packs.");
            }

            if (string.IsNullOrWhiteSpace(pack.PackFolderPath))
            {
                return (false, "Pack has no folder path.");
            }

            if (!Directory.Exists(pack.PackFolderPath))
            {
                return (false, "Pack folder not found.");
            }

            // Verify the pack is actually in our PACKS folder (security check)
            var packsPath = PathConstants.PacksPath;
            var normalizedPackPath = Path.GetFullPath(pack.PackFolderPath);
            var normalizedPacksPath = Path.GetFullPath(packsPath);

            if (!normalizedPackPath.StartsWith(normalizedPacksPath, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Pack is not in the valid PACKS folder.");
            }

            try
            {
                Directory.Delete(pack.PackFolderPath, recursive: true);
                Logger.Info($"Deleted imported spawn pack: {pack.Metadata.Name}");
                return (true, null);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting spawn pack: {pack.Metadata.Name}", ex);
                return (false, $"Failed to delete: {ex.Message}");
            }
        }

        private static string? ResolvePackDataPath(string packFolder)
        {
            if (PackDataFiles.Any(file => File.Exists(Path.Combine(packFolder, file))))
            {
                return packFolder;
            }

            var nestedDataPath = Path.Combine(packFolder, PathConstants.UOR_DATA_SUBFOLDER);
            if (Directory.Exists(nestedDataPath) && PackDataFiles.Any(file => File.Exists(Path.Combine(nestedDataPath, file))))
            {
                return nestedDataPath;
            }

            return null;
        }

        private static SpawnPackMetadata LoadPackMetadata(string packFolder)
        {
            var metadata = new SpawnPackMetadata();
            var manifestPath = Path.Combine(packFolder, PackManifestFileName);

            if (!File.Exists(manifestPath))
            {
                return metadata;
            }

            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<SpawnPackMetadata>(json);
                if (manifest != null)
                {
                    metadata = manifest;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error reading spawn pack manifest", ex);
            }

            return metadata;
        }

        private static SpawnPackStats ComputeStats(string packFolder)
        {
            var stats = new SpawnPackStats
            {
                SpawnTypeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Water"] = 0,
                    ["Weather"] = 0,
                    ["Timed"] = 0,
                    ["Common"] = 0,
                    ["Uncommon"] = 0,
                    ["Rare"] = 0
                }
            };

            var packDataPath = ResolvePackDataPath(packFolder);
            if (packDataPath == null)
            {
                return stats;
            }

            var uniqueCreatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mapIds = new HashSet<int>();

            stats.BoxSpawnCount = ReadBoxStats(Path.Combine(packDataPath, PathConstants.BOX_FILENAME), stats, uniqueCreatures, mapIds);
            stats.TileSpawnCount = ReadTileStats(Path.Combine(packDataPath, PathConstants.TILE_FILENAME), stats, uniqueCreatures, mapIds);
            stats.RegionSpawnCount = ReadRegionStats(Path.Combine(packDataPath, PathConstants.REGION_FILENAME), stats, uniqueCreatures, mapIds);

            stats.TotalSpawnEntries = stats.SpawnTypeCounts.Values.Sum();
            stats.UniqueCreatureCount = uniqueCreatures.Count;
            stats.MapCount = mapIds.Count;

            return stats;
        }

        private static int ReadBoxStats(string filePath, SpawnPackStats stats, HashSet<string> uniqueCreatures, HashSet<int> mapIds)
        {
            if (!File.Exists(filePath))
            {
                return 0;
            }

            try
            {
                using var reader = new BinaryReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
                reader.ReadInt32();
                reader.ReadString();

                int mapCount = reader.ReadInt32();
                int totalBoxes = 0;

                for (int m = 0; m < mapCount; m++)
                {
                    int mapId = reader.ReadInt32();
                    mapIds.Add(mapId);
                    reader.ReadString();
                    int boxCount = reader.ReadInt32();

                    for (int b = 0; b < boxCount; b++)
                    {
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt32();

                        ReadSpawnList(reader, uniqueCreatures, stats, "Water");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Weather");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Timed");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Common");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Uncommon");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Rare");

                        totalBoxes++;
                    }
                }

                return totalBoxes;
            }
            catch (Exception ex)
            {
                Logger.Error("Error reading box spawn pack stats", ex);
                return 0;
            }
        }

        private static int ReadTileStats(string filePath, SpawnPackStats stats, HashSet<string> uniqueCreatures, HashSet<int> mapIds)
        {
            if (!File.Exists(filePath))
            {
                return 0;
            }

            try
            {
                using var reader = new BinaryReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
                reader.ReadInt32();
                reader.ReadString();

                int mapCount = reader.ReadInt32();
                int totalTiles = 0;

                for (int m = 0; m < mapCount; m++)
                {
                    int mapId = reader.ReadInt32();
                    mapIds.Add(mapId);
                    reader.ReadString();
                    int tileCount = reader.ReadInt32();

                    for (int t = 0; t < tileCount; t++)
                    {
                        reader.ReadInt32();
                        reader.ReadString();
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt32();

                        ReadSpawnList(reader, uniqueCreatures, stats, "Water");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Weather");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Timed");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Common");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Uncommon");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Rare");

                        totalTiles++;
                    }
                }

                return totalTiles;
            }
            catch (Exception ex)
            {
                Logger.Error("Error reading tile spawn pack stats", ex);
                return 0;
            }
        }

        private static int ReadRegionStats(string filePath, SpawnPackStats stats, HashSet<string> uniqueCreatures, HashSet<int> mapIds)
        {
            if (!File.Exists(filePath))
            {
                return 0;
            }

            try
            {
                using var reader = new BinaryReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
                reader.ReadInt32();
                reader.ReadString();

                int mapCount = reader.ReadInt32();
                int totalRegions = 0;

                for (int m = 0; m < mapCount; m++)
                {
                    int mapId = reader.ReadInt32();
                    mapIds.Add(mapId);
                    reader.ReadString();
                    int regionCount = reader.ReadInt32();

                    for (int r = 0; r < regionCount; r++)
                    {
                        reader.ReadInt32();
                        reader.ReadString();
                        reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt32();

                        ReadSpawnList(reader, uniqueCreatures, stats, "Water");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Weather");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Timed");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Common");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Uncommon");
                        ReadSpawnList(reader, uniqueCreatures, stats, "Rare");

                        totalRegions++;
                    }
                }

                return totalRegions;
            }
            catch (Exception ex)
            {
                Logger.Error("Error reading region spawn pack stats", ex);
                return 0;
            }
        }

        private static void ReadSpawnList(BinaryReader reader, HashSet<string> uniqueCreatures, SpawnPackStats stats, string typeKey)
        {
            int count = reader.ReadInt32();
            if (!stats.SpawnTypeCounts.ContainsKey(typeKey))
            {
                stats.SpawnTypeCounts[typeKey] = 0;
            }

            for (int i = 0; i < count; i++)
            {
                var name = reader.ReadString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    uniqueCreatures.Add(name.Trim());
                }
            }

            stats.SpawnTypeCounts[typeKey] += count;
        }

        private static SpawnPackInfo CreateMockPack()
        {
            return new SpawnPackInfo
            {
                Metadata = new SpawnPackMetadata
                {
                    Id = "default-pack",
                    Name = "Default Spawn Pack",
                    Author = "Black Box Programming",
                    Description = "Approved default spawn pack.",
                    ImageFileName = string.Empty,
                    Version = "1.0.0",
                    PublishedOn = DateTime.UtcNow,
                    IsApproved = true
                },
                Stats = new SpawnPackStats
                {
                    BoxSpawnCount = 128,
                    TileSpawnCount = 64,
                    RegionSpawnCount = 32,
                    TotalSpawnEntries = 1024,
                    UniqueCreatureCount = 240,
                    MapCount = 6,
                    SpawnTypeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Water"] = 120,
                        ["Weather"] = 80,
                        ["Timed"] = 60,
                        ["Common"] = 520,
                        ["Uncommon"] = 180,
                        ["Rare"] = 64
                    }
                }
            };
        }
    }
}
