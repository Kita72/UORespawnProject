namespace UORespawnApp
{
    /// <summary>
    /// Manual import utility for spawn CSV files from server to local
    /// </summary>
    internal static class ServerImportUtility
    {
        private static readonly string[] SpawnFiles = new[]
        {
            "UOR_Spawn.csv",
            "UOR_WorldSpawn.csv",
            "UOR_StaticSpawn.csv",
            "UOR_SpawnSettings.csv"
        };

        /// <summary>
        /// Imports spawn CSV files from server to local data folder
        /// Returns a tuple of (success, message, filesImported)
        /// </summary>
        public static (bool success, string message, int filesImported) ImportFromServer()
        {
            try
            {
                // Validate server folder is configured
                if (string.IsNullOrEmpty(Settings.ServUODataFolder))
                {
                    return (false, "No ServUO Data folder configured. Please set the server folder path first.", 0);
                }

                if (!Directory.Exists(Settings.ServUODataFolder))
                {
                    return (false, $"ServUO Data folder not found: {Settings.ServUODataFolder}", 0);
                }

                string localDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(localDataFolder))
                {
                    Directory.CreateDirectory(localDataFolder);
                }

                int importedCount = 0;
                int skippedCount = 0;
                List<string> importedFileNames = new();

                foreach (string fileName in SpawnFiles)
                {
                    string serverFile = Path.Combine(Settings.ServUODataFolder, fileName);
                    string localFile = Path.Combine(localDataFolder, fileName);

                    // Check if file exists on server
                    if (!File.Exists(serverFile))
                    {
                        skippedCount++;
                        continue;
                    }

                    // Create backup of existing local file
                    if (File.Exists(localFile))
                    {
                        string backupFile = $"{localFile}.backup";
                        File.Copy(localFile, backupFile, true);
                    }

                    // Copy from server to local
                    File.Copy(serverFile, localFile, true);
                    importedCount++;
                    importedFileNames.Add(fileName);
                    Console.WriteLine($"Imported: {fileName}");
                }

                if (importedCount == 0)
                {
                    return (false, "No spawn files found on server to import.", 0);
                }

                string message = $"Successfully imported {importedCount} file(s) from server:\n" + 
                                string.Join("\n", importedFileNames.Select(f => $"- {f}"));
                
                if (skippedCount > 0)
                {
                    message += $"\n\n({skippedCount} file(s) not found on server)";
                }

                return (true, message, importedCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Import Error: {ex.Message}");
                return (false, $"Import failed: {ex.Message}", 0);
            }
        }
    }
}
