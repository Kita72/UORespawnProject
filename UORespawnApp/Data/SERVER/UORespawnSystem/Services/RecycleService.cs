using System;
using System.Linq;
using System.Collections.Generic;

namespace Server.Custom.UORespawnServer.Services
{
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

        internal void Add(Serial serial)
        {
            var spawn = World.FindMobile(serial);

            if (spawn == null || spawn.Deleted)
            {
                return;
            }

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
            }
            else
            {
                if (!spawn.Deleted)
                {
                    spawn.Delete();

                    _TotalDeleted++;
                }
            }
        }

        internal void ClearRecycled()
        {
            _RecycledList.Clear();
        }

        internal int GetRecycledTotal()
        {
            return _TotalRecycled;
        }

        internal Mobile Remove(Serial serial, string name = "")
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (_RecycledList.ContainsKey(name) && _RecycledList[name].Count > 0)
                {
                    serial = _RecycledList[name].First();
                }
            }

            var spawn = World.FindMobile(serial);

            if (spawn == null || spawn.Deleted) return null;

            if (_RecycledList.ContainsKey(spawn.GetType().Name))
            {
                _RecycledList[spawn.GetType().Name].Remove(serial);
            }

            return spawn;
        }

        private bool HasRoom()
        {
            int count = 0;

            if (_RecycledList.Count > 0)
            {
                foreach (var type in _RecycledList)
                {
                    count += type.Value.Count;
                }
            }

            return UOR_Settings.MAX_RECYCLE_TOTAL > count;
        }
    }
}
