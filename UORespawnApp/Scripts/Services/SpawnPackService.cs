using System.IO.Compression;
using System.Text.Json;
using UORespawnApp.Scripts.Constants;
using UORespawnApp.Scripts.Entities;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    /// <summary>
    /// Loads spawn packs from Data/PACKS and provides stats and apply workflows.
    /// Folder structure:
    ///   Data/PACKS/Approved/  - Unpacked approved packs (ready to use)
    ///   Data/PACKS/Imported/  - User-imported packs
    ///   Data/PACKS/Backup/    - Original ZIP files for approved packs (for reset)
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
        /// Unpacks approved pack ZIPs from Backup folder to Approved folder.
        /// Called on first launch to initialize approved packs.
        /// </summary>
        public void UnpackApprovedPacks()
        {
            var backupPath = PathConstants.PacksBackupPath;
            var approvedPath = PathConstants.PacksApprovedPath;

            if (!Directory.Exists(backupPath))
            {
                return;
            }

            foreach (var zipFile in Directory.GetFiles(backupPath, "*.zip"))
            {
                try
                {
                    var packName = Path.GetFileNameWithoutExtension(zipFile);
                    var packFolder = Path.Combine(approvedPath, packName);

                    // Skip if already unpacked
                    if (Directory.Exists(packFolder) && Directory.GetFiles(packFolder, "*.bin").Length > 0)
                    {
                        continue;
                    }

                    // Create folder and extract
                    Directory.CreateDirectory(packFolder);
                    ZipFile.ExtractToDirectory(zipFile, packFolder, overwriteFiles: true);

                    Logger.Info($"Unpacked approved pack: {packName}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error unpacking approved pack: {zipFile}", ex);
                }
            }
        }

        /// <summary>
        /// Load approved packs from Data/PACKS/Approved/ folder
        /// </summary>
        public List<SpawnPackInfo> LoadApprovedPacks()
        {
            // Ensure approved packs are unpacked from Backup folder
            UnpackApprovedPacks();

            var packs = new List<SpawnPackInfo>();
            var approvedPath = PathConstants.PacksApprovedPath;

            if (Directory.Exists(approvedPath))
            {
                foreach (var packFolder in Directory.GetDirectories(approvedPath))
                {
                    var pack = LoadPackInfo(packFolder);
                    if (pack != null)
                    {
                        // Force IsApproved for packs in Approved folder
                        pack.Metadata.IsApproved = true;

                        // Check if pack has been modified from backup
                        pack.IsModified = IsPackModified(pack);

                        packs.Add(pack);
                    }
                }
            }

            return packs.OrderBy(p => p.Metadata.Name).ToList();
        }

        /// <summary>
        /// Load imported/user packs from Data/PACKS/Imported/ folder
        /// </summary>
        public List<SpawnPackInfo> LoadImportedPacks()
        {
            var packs = new List<SpawnPackInfo>();
            var importedPath = PathConstants.PacksImportedPath;

            if (Directory.Exists(importedPath))
            {
                foreach (var packFolder in Directory.GetDirectories(importedPath))
                {
                    var pack = LoadPackInfo(packFolder);
                    if (pack != null)
                    {
                        // Force IsApproved = false for imported packs
                        pack.Metadata.IsApproved = false;
                        packs.Add(pack);
                    }
                }
            }

            return packs.OrderBy(p => p.Metadata.Name).ToList();
        }

        /// <summary>
        /// Load all packs from both Approved and Imported folders
        /// </summary>
        public List<SpawnPackInfo> LoadAllPacks()
        {
            var packs = new List<SpawnPackInfo>();
            packs.AddRange(LoadApprovedPacks());
            packs.AddRange(LoadImportedPacks());
            return packs;
        }

        public List<SpawnPackInfo> LoadPacks()
        {
            return LoadAllPacks().OrderBy(p => p.Metadata.Name).ToList();
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

        /// <summary>
        /// Applies a spawn pack by copying its data files to UOR_DATA and syncing to server.
        /// Optionally saves current data back to the currently active pack before applying.
        /// </summary>
        /// <param name="pack">The pack to apply</param>
        /// <param name="currentPack">Optional: The currently active pack to save current data to before applying new pack</param>
        /// <param name="reloadAfterApply">Whether to reload data and sync to server after applying</param>
        public bool ApplyPack(SpawnPackInfo pack, SpawnPackInfo? currentPack = null, bool reloadAfterApply = true)
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

                // Save current data back to the active pack before applying new one
                if (currentPack != null && !string.IsNullOrWhiteSpace(currentPack.PackFolderPath))
                {
                    SaveCurrentDataToPack(currentPack);
                    Logger.Info($"Saved current data to pack: {currentPack.Metadata.Name}");
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
                    // Load the pack data into memory
                    Utility.LoadSettings();
                    Utility.LoadSpawnData();
                    Utility.LoadTileSpawnData();
                    Utility.LoadRegionSpawnData();

                    // Save to sync data to server (if linked)
                    // This ensures the server gets the pack data immediately
                    Utility.SaveSettings();
                    Utility.SaveSpawnData();
                    Utility.SaveTileSpawnData();
                    Utility.SaveRegionSpawnData();

                    Logger.Info("Spawn pack data synced to server");
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

        /// <summary>
        /// Gets the resolved data path for a pack (where the .bin files are located).
        /// Returns the pack folder path if files are there, or the UOR_DATA subfolder if nested.
        /// </summary>
        public string? GetPackDataPath(SpawnPackInfo pack)
        {
            if (pack == null || string.IsNullOrEmpty(pack.PackFolderPath))
            {
                return null;
            }
            return ResolvePackDataPath(pack.PackFolderPath);
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

        /// <summary>
        /// Gets the backup ZIP path for an approved pack.
        /// </summary>
        public string? GetBackupZipPath(SpawnPackInfo pack)
        {
            if (pack == null || string.IsNullOrWhiteSpace(pack.PackFolderPath))
            {
                return null;
            }

            var packName = Path.GetFileName(pack.PackFolderPath);
            var backupZipPath = Path.Combine(PathConstants.PacksBackupPath, $"{packName}.zip");

            return File.Exists(backupZipPath) ? backupZipPath : null;
        }

        /// <summary>
        /// Checks if an approved pack has a backup ZIP available for reset.
        /// </summary>
        public bool HasBackupZip(SpawnPackInfo pack)
        {
            return GetBackupZipPath(pack) != null;
        }

        /// <summary>
        /// Checks if an approved pack has been modified from its original backup.
        /// Compares file sizes and checksums of data files against the backup ZIP.
        /// </summary>
        public bool IsPackModified(SpawnPackInfo pack)
        {
            if (pack == null || !pack.Metadata.IsApproved)
            {
                return false;
            }

            var backupZipPath = GetBackupZipPath(pack);
            if (backupZipPath == null)
            {
                return false; // No backup to compare against
            }

            try
            {
                var packDataPath = ResolvePackDataPath(pack.PackFolderPath) ?? pack.PackFolderPath;

                using var archive = ZipFile.OpenRead(backupZipPath);
                foreach (var entry in archive.Entries)
                {
                    if (!PackDataFiles.Contains(entry.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var currentFilePath = Path.Combine(packDataPath, entry.Name);
                    if (!File.Exists(currentFilePath))
                    {
                        return true; // File missing = modified
                    }

                    // Compare file sizes first (fast check)
                    var currentFileInfo = new FileInfo(currentFilePath);
                    if (currentFileInfo.Length != entry.Length)
                    {
                        return true; // Size differs = modified
                    }

                    // Compare file contents (byte comparison)
                    using var zipStream = entry.Open();
                    using var memoryStream = new MemoryStream();
                    zipStream.CopyTo(memoryStream);
                    var zipBytes = memoryStream.ToArray();

                    var currentBytes = File.ReadAllBytes(currentFilePath);
                    if (!zipBytes.SequenceEqual(currentBytes))
                    {
                        return true; // Content differs = modified
                    }
                }

                return false; // All files match
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking if pack is modified: {pack.Metadata.Name}", ex);
                return false; // On error, assume not modified
            }
        }

        /// <summary>
        /// Resets an approved pack from its backup ZIP in the Backup folder.
        /// Restores all data files to their original state.
        /// </summary>
        public (bool Success, string? Error) ResetPackFromBackup(SpawnPackInfo pack)
        {
            if (pack == null || string.IsNullOrWhiteSpace(pack.PackFolderPath))
            {
                return (false, "Pack not specified.");
            }

            if (!pack.Metadata.IsApproved)
            {
                return (false, "Only approved packs can be reset from backup.");
            }

            // Get pack name from folder path
            var packName = Path.GetFileName(pack.PackFolderPath);
            var backupZipPath = Path.Combine(PathConstants.PacksBackupPath, $"{packName}.zip");

            if (!File.Exists(backupZipPath))
            {
                return (false, $"No backup ZIP found for this pack at: {backupZipPath}");
            }

            try
            {
                var packDataPath = ResolvePackDataPath(pack.PackFolderPath) ?? pack.PackFolderPath;

                using var archive = ZipFile.OpenRead(backupZipPath);
                foreach (var entry in archive.Entries)
                {
                    if (PackDataFiles.Contains(entry.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        var destinationPath = Path.Combine(packDataPath, entry.Name);
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }

                Logger.Info($"Reset pack from backup: {pack.Metadata.Name}");
                return (true, null);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error resetting pack from backup: {pack.Metadata.Name}", ex);
                return (false, $"Failed to reset: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves current UOR_DATA files to a pack folder.
        /// Used to preserve current state before applying a different pack.
        /// </summary>
        public bool SaveCurrentDataToPack(SpawnPackInfo pack)
        {
            if (pack == null || string.IsNullOrWhiteSpace(pack.PackFolderPath))
            {
                return false;
            }

            try
            {
                var sourcePath = PathConstants.LocalDataPath;
                var packDataPath = ResolvePackDataPath(pack.PackFolderPath) ?? pack.PackFolderPath;

                foreach (var fileName in PackDataFiles)
                {
                    var sourceFile = Path.Combine(sourcePath, fileName);
                    if (File.Exists(sourceFile))
                    {
                        var destinationFile = Path.Combine(packDataPath, fileName);
                        File.Copy(sourceFile, destinationFile, overwrite: true);
                    }
                }

                Logger.Info($"Saved current data to pack: {pack.Metadata.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving current data to pack: {pack.Metadata.Name}", ex);
                return false;
            }
        }
    }
}
