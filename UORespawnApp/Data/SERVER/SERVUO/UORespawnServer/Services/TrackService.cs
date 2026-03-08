using System.Collections.Generic;
using System.Linq;

using Server.Custom.UORespawnServer.Items;

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
            _TrashBin = new List<Item>();
        }

        private List<Item> _TrashBin;

        internal void CleanUpItems()
        {
            if (_TrashBin != null) { _TrashBin = new List<Item>(); }

            _TrashBin.Clear();

            _TrashBin.AddRange(World.Items.Values.Where(g => g is RiftGate).ToList());
            _TrashBin.AddRange(World.Items.Values.Where(g => g is DebugFlag).ToList());

            if (_TrashBin.Count > 0)
            {
                foreach (var item in _TrashBin)
                {
                    if (item != null && !item.Deleted)
                        item.Delete();
                }
            }
        }
    }
}
