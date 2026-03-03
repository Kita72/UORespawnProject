using System.Linq;
using System.Collections.Generic;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Recycle pool for mob spawn.
    /// Keeps dictionary of type -> serials for reuse instead of creating new.
    /// Recycled spawn KEEP their ISpawner reference (never released) - this maintains
    /// the "leash" so spawn can't escape tracking. When pulled from pool, we check
    /// if Spawner is already set (no need to re-claim).
    /// </summary>
    internal class RecycleService
    {
        private readonly Dictionary<string, List<Serial>> _RecycledList;
        private int _TotalRecycled;
        private int _TotalDeleted;

        internal RecycleService()
        {
            _RecycledList = new Dictionary<string, List<Serial>>();
            _TotalRecycled = 0;
            _TotalDeleted = 0;
        }

        /// <summary>
        /// Add a spawn to the recycle pool.
        /// Spawn KEEPS ISpawner reference - never release! This maintains tracking.
        /// </summary>
        internal void Add(Serial serial)
        {
            var spawn = World.FindMobile(serial);

            if (spawn == null || spawn.Deleted)
                return;

            // DO NOT release ISpawner - recycled spawn stays "leashed"
            // When pulled from pool, ProcessService checks Spawner field

            string typeName = spawn.GetType().Name;

            if (HasRoom())
            {
                if (!_RecycledList.ContainsKey(typeName))
                {
                    _RecycledList.Add(typeName, new List<Serial>() { serial });
                    _TotalRecycled++;
                }
                else if (_RecycledList[typeName].Count < UOR_Settings.MAX_RECYCLE_TYPE)
                {
                    _RecycledList[typeName].Add(serial);
                    _TotalRecycled++;
                }
                else
                {
                    // Type pool is full - delete
                    spawn.Delete();
                    _TotalDeleted++;
                }
            }
            else
            {
                // Total pool is full - delete
                spawn.Delete();
                _TotalDeleted++;
            }
        }

        /// <summary>
        /// Clears all recycled spawn (deletes them from world).
        /// </summary>
        internal void ClearRecycled()
        {
            foreach (var kvp in _RecycledList)
            {
                foreach (var serial in kvp.Value)
                {
                    var mob = World.FindMobile(serial);
                    mob?.Delete();
                }
            }

            _RecycledList.Clear();
            _TotalRecycled = 0;
            _TotalDeleted = 0;
        }

        internal int GetRecycledTotal()
        {
            int count = 0;
            foreach (var kvp in _RecycledList)
            {
                count += kvp.Value.Count;
            }
            return count;
        }

        /// <summary>
        /// Remove and return a spawn from the recycle pool by type name.
        /// Returns null if none available.
        /// </summary>
        internal Mobile Remove(string name = "")
        {
            if (!string.IsNullOrEmpty(name) && _RecycledList.TryGetValue(name, out var list) && list.Count > 0)
            {
                var spawn = World.FindMobile(list.First());

                list.RemoveAt(0);

                if (spawn == null || spawn.Deleted)
                    return null;

                return spawn;
            }

            return null;
        }

        private bool HasRoom()
        {
            return GetRecycledTotal() < UOR_Settings.MAX_RECYCLE_TOTAL;
        }
    }
}
