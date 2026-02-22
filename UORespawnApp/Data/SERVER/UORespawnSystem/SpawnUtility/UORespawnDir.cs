using System;
using System.IO;

namespace Server.Custom.UORespawnSystem.SpawnUtility
{
    internal static class UORespawnDir
    {
        private static readonly string DATA_DIR = GetValidDir(AppDomain.CurrentDomain.BaseDirectory, "Data");

        // System Directory
        internal static readonly string UOR_DIR = GetValidDir(DATA_DIR, "UORespawn");
        internal static readonly string INPUT_DIR = GetValidDir(UOR_DIR, "INPUT");
        internal static readonly string OUTPUT_DIR = GetValidDir(UOR_DIR, "OUTPUT");
        internal static readonly string STAT_DIR = GetValidDir(UOR_DIR, "STATS");
        internal static readonly string SYSTEM_DIR = GetValidDir(UOR_DIR, "SYS");

        // Binary File Paths (Editor creates, Server loads)
        internal static readonly string BOX_SAVE_FILE = Path.Combine(INPUT_DIR, "UOR_BoxSpawn.bin");
        internal static readonly string REGION_SAVE_FILE = Path.Combine(INPUT_DIR, "UOR_RegionSpawn.bin");
        internal static readonly string TILE_SAVE_FILE = Path.Combine(INPUT_DIR, "UOR_TileSpawn.bin");
        internal static readonly string VENDOR_SAVE_FILE = Path.Combine(INPUT_DIR, "UOR_VendorSpawn.bin");
        internal static readonly string SETTINGS_SAVE_FILE = Path.Combine(INPUT_DIR, "UOR_SpawnSettings.bin");

        // Text File Paths (Sever creates, Editor loads)
        internal static readonly string BESTIARY_LIST_FILE = Path.Combine(OUTPUT_DIR, "UOR_BestiaryList.txt");
        internal static readonly string REGIONS_LIST_FILE = Path.Combine(OUTPUT_DIR, "UOR_RegionList.txt");
        internal static readonly string SPAWNERS_LIST_FILE = Path.Combine(OUTPUT_DIR, "UOR_SpawnerList.txt");
        internal static readonly string VENDORS_LIST_FILE = Path.Combine(OUTPUT_DIR, "UOR_VendorList.txt");
        internal static readonly string SIGN_DATA_FILE = Path.Combine(OUTPUT_DIR, "UOR_SignData.txt");
        internal static readonly string HIVE_DATA_FILE = Path.Combine(OUTPUT_DIR, "UOR_HiveData.txt");

        // System Files
        internal static readonly string TRACK_SPAWN_FILE = Path.Combine(SYSTEM_DIR, "UOR_TrackSpawn.txt");
        internal static readonly string VENDOR_SPAWN_FILE = Path.Combine(SYSTEM_DIR, "UOR_VendorSpawn.txt");
        internal static readonly string LOG_DEBUG_FILE = Path.Combine(SYSTEM_DIR, "UOR_DebugLog.txt");

        private static string GetValidDir(string dir, string route)
        {
            string directory = Path.Combine(dir, route);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }
    }
}
