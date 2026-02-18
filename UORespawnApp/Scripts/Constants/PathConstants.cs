namespace UORespawnApp.Scripts.Constants
{
    /// <summary>
    /// Centralized constants for all file and folder paths used in UORespawn v2.0
    /// Single source of truth to prevent scattered string literals
    /// </summary>
    public static class PathConstants
    {
        // ==================== FOLDER NAMES ====================

        private static readonly string BASE_DIR = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Main data folder in editor base directory
        /// </summary>
        public const string DATA_FOLDER = "Data";

        /// <summary>
        /// Resources folder containing bundled app files
        /// </summary>
        public const string RESOURCES_FOLDER = "Resources";

        /// <summary>
        /// Raw subfolder in Resources (contains default bestiary/static lists)
        /// </summary>
        public const string RAW_SUBFOLDER = "Raw";

        /// <summary>
        /// UORespawn data subfolder (contains binary and text files)
        /// Editor: Data/UOR_DATA/
        /// Server: ServUO/Data/UOR_DATA/
        /// </summary>
        public const string UOR_DATA_SUBFOLDER = "UOR_DATA";
        
        /// <summary>
        /// Statistics subfolder for heatmap data (server-side only)
        /// Server: Data/UOR_DATA/UOR_STATS/
        /// NOT copied to editor (live session data only)
        /// </summary>
        public const string UOR_STATS_SUBFOLDER = "UOR_STATS";
        
        /// <summary>
        /// Maps subfolder in Data folder (contains map image files)
        /// Editor: Data/maps/Map0.bmp, Map1.bmp, etc.
        /// Separate from UOR_DATA to allow easy visual browsing of large BMP files
        /// User can add/replace maps via Settings page or directly in file explorer
        /// Standard maps (0-5): Felucca, Trammel, Ilshenar, Malas, Tokuno, Ter Mur
        /// Custom maps (6+): Simply add Map6.bmp, Map7.bmp, etc. - auto-detected
        /// </summary>
        public const string MAPS_SUBFOLDER = "MAPS";

        /// <summary>
        /// Packs subfolder in Data folder (contains spawn pack data)
        /// Structure: Data/PACKS/Approved/, Data/PACKS/Imported/, Data/PACKS/Backup/
        /// </summary>
        public const string PACKS_SUBFOLDER = "PACKS";

        /// <summary>
        /// Approved packs subfolder - contains unpacked approved packs ready to use
        /// </summary>
        public const string PACKS_APPROVED_SUBFOLDER = "Approved";

        /// <summary>
        /// Imported packs subfolder - contains user-imported packs
        /// </summary>
        public const string PACKS_IMPORTED_SUBFOLDER = "Imported";

        /// <summary>
        /// Created packs subfolder - contains user-created packs (from scratch)
        /// </summary>
        public const string PACKS_CREATED_SUBFOLDER = "Created";

        /// <summary>
        /// Backup subfolder - contains original ZIP files for approved packs
        /// Used to reset packs to default state
        /// </summary>
        public const string PACKS_BACKUP_SUBFOLDER = "Backup";

        /// <summary>
        /// Server scripts/data folder in Data directory
        /// Contains UORespawnSystem.zip for server setup
        /// </summary>
        public const string SERVER_SUBFOLDER = "SERVER";

        // ==================== BINARY DATA FILE NAMES (.bin) ====================
        // Editor creates/saves these files, server reads them
        // Uses BinaryReader/BinaryWriter (ServUO-style) for .NET 10 compatibility

        /// <summary>
        /// Spawn settings binary file 
        /// Contains: chances, ranges, mob limits, debug flags
        /// </summary>
        public const string SETTINGS_FILENAME = "UOR_SpawnSettings.bin";

        /// <summary>
        /// Box spawn binary file 
        /// Contains: rectangular spawn areas with creature lists
        /// </summary>
        public const string BOX_FILENAME = "UOR_BoxSpawn.bin";

        /// <summary>
        /// Tile spawn binary file
        /// Contains: tile-type based spawns (grass, snow, etc.)
        /// </summary>
        public const string TILE_FILENAME = "UOR_TileSpawn.bin";

        /// <summary>
        /// Region spawn binary file
        /// Contains: server region-based spawns (Britain, Despise, etc.)
        /// </summary>
        public const string REGION_FILENAME = "UOR_RegionSpawn.bin";

        // ==================== TEXT FILE NAMES (.txt) ====================
        // Server generates these files, editor copies and reads them
        
        /// <summary>
        /// Bestiary list text file (server-generated)
        /// Contains: List of all valid creature class names
        /// Editor can merge with custom additions
        /// </summary>
        public const string BESTIARY_FILENAME = "UOR_BestiaryList.txt";
        
        /// <summary>
        /// Region list text file (server-generated)
        /// Format: MapID:RegionName:(X,Y,Width,Height)
        /// Used for Region Spawn page (pre-defined areas)
        /// </summary>
        public const string REGION_LIST_FILENAME = "UOR_RegionList.txt";
        
        /// <summary>
        /// Spawner list text file (server-generated)
        /// Format: MapIndex:X:Y:HomeRange
        /// Lists existing Spawner/XmlSpawner items for heatmap display
        /// </summary>
        public const string SPAWNER_LIST_FILENAME = "UOR_SpawnerList.txt";

        // ==================== LEGACY CSV FILE NAMES ====================
        // Old v1.0 format - only used for one-time import
        
        /// <summary>
        /// Legacy box spawn CSV file (v1.0 format)
        /// User places in Data/ folder manually for import
        /// NOT in UOR_DATA/ to avoid confusion with server files
        /// </summary>
        public const string LEGACY_BOX_CSV = "BoxSpawn.csv";
        
        /// <summary>
        /// Legacy tile spawn CSV file (v1.0 format)
        /// User places in Data/ folder manually for import
        /// NOT in UOR_DATA/ to avoid confusion with server files
        /// </summary>
        public const string LEGACY_TILE_CSV = "TileSpawn.csv";

        // ==================== STATS FILE PATTERNS ====================
        // Server-only files, NOT copied to editor
        
        /// <summary>
        /// Pattern for spawn statistics files (server-only)
        /// Example: Map0_SpawnStats.txt, Map1_SpawnStats.txt
        /// Contains live session heatmap data
        /// </summary>
        public const string STATS_FILE_PATTERN = "*_SpawnStats.txt";
        
        /// <summary>
        /// Alternative stats file pattern
        /// </summary>
        public const string STATS_FILE_SUFFIX = "_Stats.txt";

        // ==================== FOLDER PATHS ====================
        
        /// <summary>
        /// Get the local data folder path (editor's Data/UOR_DATA/)
        /// This is where the editor stores local backup copies
        /// </summary>
        public static string LocalDataPath
        {
            get
            {
                var dataPath = Path.Combine(BASE_DIR, DATA_FOLDER, UOR_DATA_SUBFOLDER);
                
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
                
                return dataPath;
            }
        }
        
        /// <summary>
        /// Get the server data folder path (Server/Data/UOR_DATA/)
        /// Returns null if server folder not configured or doesn't exist
        /// </summary>
        public static string? ServerDataPath
        {
            get
            {
                var serverFolder = Settings.ServUODataFolder;
                
                if (string.IsNullOrEmpty(serverFolder) || !Directory.Exists(serverFolder))
                {
                    return null;
                }

                var serverDataPath = Path.Combine(serverFolder, UOR_DATA_SUBFOLDER);
                
                if (!Directory.Exists(serverDataPath))
                {
                    try
                    {
                        Directory.CreateDirectory(serverDataPath);
                    }
                    catch
                    {
                        return null;
                    }
                }
                
                return serverDataPath;
            }
        }
        
        /// <summary>
        /// Get the server stats folder path (Server/Data/UOR_DATA/UOR_STATS/)
        /// Used for heatmap statistics (live data only, not copied to editor)
        /// Returns null if server folder not configured
        /// </summary>
        public static string? ServerStatsPath
        {
            get
            {
                var serverDataPath = ServerDataPath;
                if (serverDataPath == null)
                    return null;
                
                var statsPath = Path.Combine(serverDataPath, UOR_STATS_SUBFOLDER);
                
                if (!Directory.Exists(statsPath))
                {
                    try
                    {
                        Directory.CreateDirectory(statsPath);
                    }
                    catch
                    {
                        return null;
                    }
                }
                
                return statsPath;
            }
        }
        
        /// <summary>
        /// Get the maps folder path (Data/maps/)
        /// Used for storing map image files (.bmp)
        /// All map images stored in Data/maps/ for easy user access and replacement
        /// Converted to base64 data URLs when loaded for Blazor WebView display
        /// See MapUtility.cs for full map storage architecture documentation
        /// </summary>
        public static string MapsPath
        {
            get
            {
                var mapsPath = Path.Combine(BASE_DIR, DATA_FOLDER, MAPS_SUBFOLDER);

                if (!Directory.Exists(mapsPath))
                {
                    Directory.CreateDirectory(mapsPath);
                }

                return mapsPath;
            }
        }

        /// <summary>
        /// Get the packs folder path (Data/PACKS/)
        /// Used for storing spawn pack backups and staging data
        /// </summary>
        public static string PacksPath
        {
            get
            {
                var packsPath = Path.Combine(BASE_DIR, DATA_FOLDER, PACKS_SUBFOLDER);

                if (!Directory.Exists(packsPath))
                {
                    Directory.CreateDirectory(packsPath);
                }

                return packsPath;
            }
        }

        /// <summary>
        /// Get the approved packs folder path (Data/PACKS/Approved/)
        /// Contains unpacked approved packs ready to use
        /// </summary>
        public static string PacksApprovedPath
        {
            get
            {
                var path = Path.Combine(PacksPath, PACKS_APPROVED_SUBFOLDER);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        /// <summary>
        /// Get the imported packs folder path (Data/PACKS/Imported/)
        /// Contains user-imported packs
        /// </summary>
        public static string PacksImportedPath
        {
            get
            {
                var path = Path.Combine(PacksPath, PACKS_IMPORTED_SUBFOLDER);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        /// <summary>
        /// Get the created packs folder path (Data/PACKS/Created/)
        /// Contains user-created packs (made from scratch in the editor)
        /// </summary>
        public static string PacksCreatedPath
        {
            get
            {
                var path = Path.Combine(PacksPath, PACKS_CREATED_SUBFOLDER);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        /// <summary>
        /// Get the backup packs folder path (Data/PACKS/Backup/)
        /// Contains original ZIP files for approved packs
        /// </summary>
        public static string PacksBackupPath
        {
            get
            {
                var path = Path.Combine(PacksPath, PACKS_BACKUP_SUBFOLDER);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        /// <summary>
        /// Currently active approved pack's data folder path.
        /// Set when an approved pack is applied. When spawn edits are saved,
        /// they're also synced to this folder (similar to server sync).
        /// Null if no approved pack is active.
        /// </summary>
        public static string? ActivePackDataPath { get; set; }

        /// <summary>
        /// When true, suppresses syncing files back to the active pack folder.
        /// Used during ApplyPack to prevent re-serializing data that would cause
        /// byte-level differences when comparing against backup ZIPs.
        /// </summary>
        public static bool SuppressPackSync { get; set; }

        /// <summary>
        /// Get the server folder path (Data/SERVER/)
        /// Contains UORespawnSystem.zip for server setup
        /// </summary>
        public static string ServerPath
        {
            get
            {
                var path = Path.Combine(BASE_DIR, DATA_FOLDER, SERVER_SUBFOLDER);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        /// <summary>
        /// Get the legacy CSV folder path (Data/ - NOT Data/UOR_DATA/)
        /// User manually places old CSV files here for import
        /// </summary>
        public static string LegacyDataPath
        {
            get
            {
                var dataPath = Path.Combine(BASE_DIR, DATA_FOLDER);

                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }

                return dataPath;
            }
        }

        /// <summary>
        /// Get the Resources/Raw folder path (bundled app resources)
        /// Contains default bestiary and static lists
        /// </summary>
        public static string ResourcesRawPath
        {
            get
            {
                return Path.Combine(BASE_DIR, RESOURCES_FOLDER, RAW_SUBFOLDER);
            }
        }

        // ==================== HELPER METHODS ====================
        
        /// <summary>
        /// Check if a filename is a stats file (should not be copied to editor)
        /// </summary>
        public static bool IsStatsFile(string fileName)
        {
            return fileName.EndsWith(STATS_FILE_SUFFIX, StringComparison.OrdinalIgnoreCase) ||
                   fileName.Contains("SpawnStats", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if a filename is a bestiary file
        /// </summary>
        public static bool IsBestiaryFile(string fileName)
        {
            return fileName.Equals(BESTIARY_FILENAME, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Check if a filename is a region list file
        /// </summary>
        public static bool IsRegionListFile(string fileName)
        {
            return fileName.Equals(REGION_LIST_FILENAME, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Check if a filename is a spawner list file
        /// </summary>
        public static bool IsSpawnerListFile(string fileName)
        {
            return fileName.Equals(SPAWNER_LIST_FILENAME, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Get the full local path for a binary file
        /// </summary>
        public static string GetLocalFilePath(string filename)
        {
            return Path.Combine(LocalDataPath, filename);
        }
        
        /// <summary>
        /// Get the full server path for a binary file (or null if server not connected)
        /// </summary>
        public static string? GetServerFilePath(string filename)
        {
            var serverPath = ServerDataPath;
            return serverPath != null ? Path.Combine(serverPath, filename) : null;
        }
    }
}
