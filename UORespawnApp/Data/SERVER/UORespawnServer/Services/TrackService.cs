using System;

using Server.Custom.UORespawnServer.Spawners;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Startup-only cleanup service.
    /// Deletes all UOR_MobSpawner spawn from previous session on server start.
    /// No ongoing tracking needed - ISpawner handles ownership.
    /// </summary>
    internal class TrackService
    {
        internal TrackService()
        {
            // Run cleanup once at startup
            CleanupStartupSpawn();
        }

        /// <summary>
        /// Deletes all stray mob spawn from previous server session.
        /// Called once at startup. Vendors (UOR_VendorSpawner) are preserved.
        /// </summary>
        private void CleanupStartupSpawn()
        {
            int deleted = UOR_MobSpawner.CleanupAll();

            UOR_Utility.SendMsg(ConsoleColor.Cyan, $"STARTUP CLEANUP-[{deleted} Stray Mobs Deleted]");
        }
    }
}
