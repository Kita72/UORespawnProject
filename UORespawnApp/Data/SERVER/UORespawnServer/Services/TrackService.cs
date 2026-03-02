using System;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Track service placeholder.
    /// 
    /// NOTE: Startup reclaim and cleanup is now handled centrally in UOR_Core.OnServerStarted().
    /// This ensures proper ordering: Reclaim → Cleanup → Vendor Init → Services → Events → Timers.
    /// 
    /// This class remains for potential future tracking features.
    /// </summary>
    internal class TrackService
    {
        internal TrackService()
        {
            // Startup logic moved to UOR_Core.OnServerStarted() for centralized control
        }
    }
}
