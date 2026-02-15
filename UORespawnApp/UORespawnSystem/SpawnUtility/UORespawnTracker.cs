using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Server.Custom.UORespawnSystem.SpawnUtility
{
    internal static class UORespawnTracker
    {
        private static bool HasStarted = false;

        private static List<int> TrackedSpawn;

        private static readonly string TrackSpawnFile = Path.Combine(UORespawnSettings.UOR_DATA, "UOR_TrackSpawn.txt");

        public static void Initialize()
        {
            EventSink.ServerStarted += EventSink_ServerStarted;
            EventSink.MobileDeleted += EventSink_MobileDeleted;
        }

        private static void EventSink_ServerStarted()
        {
            TrackedSpawn = new List<int>();

            if (File.Exists(TrackSpawnFile))
            {
                var lines = File.ReadAllLines(TrackSpawnFile);

                foreach (var line in lines)
                {
                    TrackedSpawn.Add(Int32.Parse(line));
                }
            }

            if (TrackedSpawn.Count > 0)
            {
                int deleted = 0;

                for (int i = 0; i < TrackedSpawn.Count; i++)
                {
                    if (World.Mobiles.ContainsKey(TrackedSpawn[i]))
                    {
                        var spawn = World.Mobiles[TrackedSpawn[i]];

                        if (spawn != null)
                        {
                            spawn.Delete();

                            deleted++;
                        }
                    }
                }

                ResetTracking();

                UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkBlue, $"{deleted} Spawn Cleaned!");

                HasStarted = true;
            }
        }

        private static void EventSink_MobileDeleted(MobileDeletedEventArgs e)
        {
            if (!HasStarted) return;

            if (e.Mobile != null && TrackedSpawn.Contains(e.Mobile.Serial.Value))
            {
                TrackedSpawn.Remove(e.Mobile.Serial.Value);
            }
        }

        private static readonly StringBuilder SB = new StringBuilder();

        internal static void AddTrackedSpawn(int serial)
        {
            if (!HasStarted) return;

            SB.Clear();

            if (!TrackedSpawn.Contains(serial))
            {
                TrackedSpawn.Add(serial);
            }

            if (TrackedSpawn.Count > 0)
            {
                foreach (var spawn in TrackedSpawn)
                {
                    SB.AppendLine(spawn.ToString());
                }

                File.WriteAllText(TrackSpawnFile, SB.ToString());
            }
        }

        private static void ResetTracking()
        {
            TrackedSpawn.Clear();

            if (File.Exists(TrackSpawnFile))
            {
                File.Delete(TrackSpawnFile);
            }
        }
    }
}

