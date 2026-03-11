using System;
using System.IO;

namespace Server.Custom.UORespawnServer
{
    internal class UOR_DIR
    {
        private static readonly string DATA_DIR = GetValidDir(AppDomain.CurrentDomain.BaseDirectory, "Data");

        // System Directory
        internal static readonly string UOR_Main = GetValidDir(DATA_DIR, "UORespawn");
        internal static readonly string INPUT_DIR = GetValidDir(UOR_Main, "INPUT");
        internal static readonly string OUTPUT_DIR = GetValidDir(UOR_Main, "OUTPUT");
        internal static readonly string STAT_DIR = GetValidDir(UOR_Main, "STATS");
        internal static readonly string SYSTEM_DIR = GetValidDir(UOR_Main, "SYS");
        internal static readonly string COMMANDS_DIR = GetValidDir(UOR_Main, "COMMANDS");

        // Binary File Paths (Editor creates, Server loads)
        internal static readonly string BOX_DATA_FILE = Path.Combine(INPUT_DIR, "UOR_BoxSpawn.bin");
        internal static readonly string REGION_DATA_FILE = Path.Combine(INPUT_DIR, "UOR_RegionSpawn.bin");
        internal static readonly string TILE_DATA_FILE = Path.Combine(INPUT_DIR, "UOR_TileSpawn.bin");
        internal static readonly string VENDOR_DATA_FILE = Path.Combine(INPUT_DIR, "UOR_VendorSpawn.bin");
        internal static readonly string SETTINGS_DATA_FILE = Path.Combine(INPUT_DIR, "UOR_SpawnSettings.csv");

        // Command Edit Files (Server creates, Editor consumes OR Editor creates, Server consumes)
        internal static readonly string SETTINGS_EDIT_FILE = Path.Combine(COMMANDS_DIR, "settings_edits.txt");
        internal static readonly string BOX_EDIT_FILE = Path.Combine(COMMANDS_DIR, "box_edits.txt");
        internal static readonly string REGION_EDIT_FILE = Path.Combine(COMMANDS_DIR, "region_edits.txt");
        internal static readonly string TILE_EDIT_FILE = Path.Combine(COMMANDS_DIR, "tile_edits.txt");
        internal static readonly string VENDOR_EDIT_FILE = Path.Combine(COMMANDS_DIR, "vendor_edits.txt");

        // Text File Paths (Sever creates, Editor loads)
        internal static readonly string MAP_LIST_FILE = Path.Combine(OUTPUT_DIR, "UOR_MapList.txt");
        internal static readonly string BESTIARY_LIST_FILE = Path.Combine(OUTPUT_DIR, "UOR_BestiaryList.txt");
        internal static readonly string REGIONS_LIST_FILE = Path.Combine(OUTPUT_DIR, "UOR_RegionList.txt");
        internal static readonly string TILE_LIST_FILE = Path.Combine(OUTPUT_DIR, "UOR_TileList.txt");
        internal static readonly string SPAWNERS_LIST_FILE = Path.Combine(OUTPUT_DIR, "UOR_SpawnerList.txt");
        internal static readonly string VENDORS_LIST_FILE = Path.Combine(OUTPUT_DIR, "UOR_VendorList.txt");
        internal static readonly string SIGN_DATA_FILE = Path.Combine(OUTPUT_DIR, "UOR_SignData.txt");
        internal static readonly string HIVE_DATA_FILE = Path.Combine(OUTPUT_DIR, "UOR_HiveData.txt");

        // Cache Files (Server builds on first run, loads on subsequent starts)
        internal static readonly string SPAWN_LOCATION_CACHE_FILE = Path.Combine(SYSTEM_DIR, "UOR_SpawnLocations.bin");

        // System Files
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
