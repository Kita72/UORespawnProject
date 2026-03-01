using System;
using System.IO;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Custom.UORespawnServer.Services
{
    internal class TrackService
    {
        private readonly List<Serial> _TrackedSpawns;

        internal TrackService()
        {
            _TrackedSpawns = new List<Serial>();

            Load();
        }

        internal void AddTracked(Serial serial)
        {
            if (!_TrackedSpawns.Contains(serial))
            {
                _TrackedSpawns.Add(serial);
            }
        }

        internal void RemoveTracked(Serial serial)
        {
            if (_TrackedSpawns.Contains(serial))
            {
                _TrackedSpawns.Remove(serial);
            }
        }

        internal void Calibrate()
        {
            int beforeCount = _TrackedSpawns.Count;

            List<Serial> list = new List<Serial>();

            foreach (var mobile in World.Mobiles.Values)
            {
                if (mobile is BaseCreature bc && bc.Home.Z == UOR_Settings.SPAWN_MARKER)
                {
                    list.Add(mobile.Serial);
                }
            }

            _TrackedSpawns.Clear();
            _TrackedSpawns.AddRange(list);

            int removed = beforeCount - _TrackedSpawns.Count;

            if (removed > 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"TRACKED-[{_TrackedSpawns.Count} Remain]");
            }

            Save();
        }

        internal void Reset()
        {
            Timer.DelayCall(TimeSpan.FromSeconds(1), () => RunReset());
        }

        private void RunReset()
        {
            int deleted = 0;

            for (int i = 0; i < _TrackedSpawns.Count; i++)
            {
                if (UOR_Utility.IsValidSpawn(_TrackedSpawns[i], out Mobile m))
                {
                    m.Delete();

                    deleted++;
                }
            }

            _TrackedSpawns.Clear();

            UOR_Utility.SendMsg(ConsoleColor.Cyan, $"TRACKED-[{deleted} Deleted]");
        }

        internal void Save()
        {
            List<string> list = new List<string>();

            foreach (var spawn in _TrackedSpawns)
            {
                if (UOR_Utility.IsValidSpawn(spawn, out _))
                {
                    list.Add($"{spawn.Value}");
                }
            }

            File.WriteAllLines(UOR_DIR.TRACK_SPAWN_FILE, list);

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"TRACKED-[{list.Count} Saved]");
        }

        private void Load()
        {
            if (!File.Exists(UOR_DIR.TRACK_SPAWN_FILE))
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"TRACKED-[No tacked spawns file found]");

                return;
            }

            var lines = File.ReadAllLines(UOR_DIR.TRACK_SPAWN_FILE);

            for (int i = 0; i < lines.Length; i++)
            {
                if (Int32.TryParse(lines[i], out int serial))
                {
                    _TrackedSpawns.Add(serial);
                }
            }

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"TRACKED-[{_TrackedSpawns.Count} Loaded]");

            if (_TrackedSpawns.Count > 0)
            {
                Reset();
            }
            else
            {
                UOR_Utility.SendMsg(ConsoleColor.Cyan, $"TRACKED-[0 Deleted]");
            }
        }
    }
}
