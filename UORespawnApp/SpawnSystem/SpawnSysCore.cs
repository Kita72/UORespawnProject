using System;
using System.IO;
using System.Linq;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Custom.SpawnSystem
{
    internal static class SpawnSysCore
    {
        private static readonly string STAT_DIR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_Stats");
        private static readonly string SAVE_DIR = Path.Combine(Core.BaseDirectory, "Saves", "SpawnSystem");
        private static readonly string TRACKED_SPAWNS_FILE = Path.Combine(SAVE_DIR, "TrackedSpawns.bin");

        private static SpawnSysTimer _SpawnTimer;

        internal static Dictionary<PlayerMobile, Queue<(string mob, Point3D loc)>> _SpawnQueue;

        internal static List<PlayerMobile> _Players;

        private static readonly object _lockObject = new object();

        private static readonly HashSet<Serial> _TrackedSpawns = new HashSet<Serial>();

        private static readonly int spawn_MinQueued = SpawnSysSettings.MIN_QUE;

        private static List<Mobile> _SpawnedList;

        private static List<Mobile> _CleanUpList;

        internal static bool HasChanged { get; set; } = false;

        private static void UpdateSpawnedList(Mobile m)
        {
            if (_SpawnedList.Contains(m))
            {
                _SpawnedList.Remove(m);
            }
            else if (_CleanUpList.Contains(m))
            {
                _CleanUpList.Remove(m);
            }

            lock (_lockObject)
            {
                if (m != null && !m.Deleted)
                {
                    _TrackedSpawns.Remove(m.Serial);
                }
            }
        }

        internal static void EnqueueSpawn(PlayerMobile pm, string mob, Point3D loc)
        {
            lock (_lockObject)
            {
                if (_SpawnQueue.ContainsKey(pm))
                {
                    _SpawnQueue[pm].Enqueue((mob, loc));
                }
            }
        }

        private static (string mob, Point3D loc) DequeueSpawn(PlayerMobile pm)
        {
            List<(string mob, Point3D loc)> queuedSpawns = new List<(string mob, Point3D loc)>();

            lock (_lockObject)
            {
                if (_SpawnQueue.ContainsKey(pm) && _SpawnQueue[pm].Count > 0)
                {
                    while (_SpawnQueue[pm].Count > 0)
                    {
                        queuedSpawns.Add(_SpawnQueue[pm].Dequeue());
                    }
                }
            }

            if (queuedSpawns.Count > 0)
            {
                foreach (var spawnInfo in queuedSpawns)
                {
                    if (!IsTooFar(pm, spawnInfo.loc))
                    {
                        return spawnInfo;
                    }
                }
            }

            return (string.Empty, SpawnSysUtility.Default_Point);
        }

        public static void Initialize()
        {
            SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkYellow, $"Started => {DateTime.Now.ToShortTimeString()}");

            LoadLogo();

            SpawnSysDataBase.LoadSpawns();

            InitializeLists();

            LoadTrackedSpawns();

            SubscribeEvents();

            StartTimer();

            SpawnSysUtility.SendConsoleMsg(ConsoleColor.Green, $"Dynamic Spawner Running...");
        }

        private static void LoadLogo()
        {
            SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkCyan, "*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*");
            SpawnSysUtility.SendConsoleMsg(ConsoleColor.Cyan, "|-|-|-|-|-|-|-| UORespawn |-|-|-|-|-|-|-|");
            SpawnSysUtility.SendConsoleMsg(ConsoleColor.Cyan, "|-|-|-|-|-|-|-|   ~*~*~   |-|-|-|-|-|-|-|");
            SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkCyan, "*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*");
        }

        private static void InitializeLists()
        {
            _SpawnQueue = new Dictionary<PlayerMobile, Queue<(string mob, Point3D loc)>>();

            _Players = new List<PlayerMobile>();

            _SpawnedList = new List<Mobile>();

            _CleanUpList = new List<Mobile>();

            SpawnSysUtility.SendConsoleMsg(ConsoleColor.Yellow, "Lists Initialized...");
        }

        private static void SubscribeEvents()
        {
            EventSink.ServerStarted += EventSink_ServerStarted;

            EventSink.TameCreature += EventSink_TameCreature;

            EventSink.CreatureDeath += EventSink_CreatureDeath;

            EventSink.MobileDeleted += EventSink_MobileDeleted;

            EventSink.BeforeWorldSave += EventSink_BeforeWorldSave;

            EventSink.AfterWorldSave += EventSink_AfterWorldSave;

            EventSink.Login += EventSink_Login;

            EventSink.Logout += EventSink_Logout;

            EventSink.Shutdown += EventSink_Shutdown;

            EventSink.Crashed += EventSink_Crashed;

            SpawnSysUtility.SendConsoleMsg(ConsoleColor.Yellow, "Events Subscribed...");
        }

        private static void StartTimer()
        {
            _SpawnTimer = new SpawnSysTimer();

            _SpawnTimer.Start();

            SpawnSysUtility.SendConsoleMsg(ConsoleColor.Yellow, "Timer Activated...");
        }

        private static void EventSink_ServerStarted()
        {
            int cleaned = 0;
            List<Serial> toRemove = new List<Serial>();

            lock (_lockObject)
            {
                foreach (Serial serial in _TrackedSpawns)
                {
                    Mobile mob = World.FindMobile(serial);

                    if (mob != null && !mob.Deleted)
                    {
                        if (mob is BaseCreature bc && (bc.Controlled || bc.IsStabled))
                        {
                            toRemove.Add(serial);
                        }
                        else
                        {
                            mob.Delete();
                            cleaned++;
                        }
                    }
                    else
                    {
                        toRemove.Add(serial);
                    }
                }

                foreach (Serial serial in toRemove)
                {
                    _TrackedSpawns.Remove(serial);
                }
            }

            if (cleaned > 0)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkYellow, $"Cleaned {cleaned} Spawn!");
            }
        }

        private static void EventSink_TameCreature(TameCreatureEventArgs e)
        {
            if (e.Creature is BaseCreature bc && bc.Controlled)
            {
                UpdateSpawnedList(e.Creature);
            }
        }

        private static void EventSink_CreatureDeath(CreatureDeathEventArgs e)
        {
            UpdateSpawnedList(e.Creature);
        }

        private static void EventSink_MobileDeleted(MobileDeletedEventArgs e)
        {
            UpdateSpawnedList(e.Mobile);
        }

        private static void EventSink_BeforeWorldSave(BeforeWorldSaveEventArgs e)
        {
            if (_SpawnTimer.Running)
            {
                _SpawnTimer.Stop();
            }

            SaveTrackedSpawns();
        }

        private static void EventSink_AfterWorldSave(AfterWorldSaveEventArgs e)
        {
            ClearSpawnSystem();

            _SpawnTimer.Start();

            try
            {
                if (!Directory.Exists(STAT_DIR))
                {
                    Directory.CreateDirectory(STAT_DIR);
                }

                foreach (var spawn in SpawnSysFactory.SpawnStats)
                {
                    string converted = $"{spawn.Item1.ToShortTimeString()}|{spawn.Item2.Name}|{spawn.Item3}|{spawn.Item4.X}|{spawn.Item4.Y}|{spawn.Item5.X}|{spawn.Item5.Y}";

                    File.AppendAllText(Path.Combine(STAT_DIR, $"{DateTime.Now.Year}_{DateTime.Now.DayOfYear}.txt"), converted + Environment.NewLine);
                }

                SpawnSysFactory.SpawnStats.Clear();

                SpawnSysUtility.CleanUpStatFiles(STAT_DIR);
            }
            catch(Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkRed, $"Stat File Error - {ex.Message}");
            }
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                lock (_lockObject)
                {
                    if (!_SpawnQueue.ContainsKey(pm))
                    {
                        _SpawnQueue.Add(pm, new Queue<(string mob, Point3D loc)>());

                        _Players.Add(pm);

                        HasChanged = true;
                    }
                }
            }
        }

        private static void EventSink_Logout(LogoutEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                lock (_lockObject)
                {
                    if (_SpawnQueue.Count > 0 && _SpawnQueue.ContainsKey(pm))
                    {
                        _SpawnQueue.Remove(pm);
                    }

                    if (_Players.Count > 0 && _Players.Contains(pm))
                    {
                        _Players.Remove(pm);

                        HasChanged = true;
                    }
                }
            }

            ClearSpawnSystem();
        }

        private static void EventSink_Shutdown(ShutdownEventArgs e)
        {
            ClearSpawnSystem();
        }

        private static void EventSink_Crashed(CrashedEventArgs e)
        {
            ClearSpawnSystem();
        }

        //Override Method : Dev Only
        internal static void UpdateWorldSpawn()
        {
            if (_SpawnQueue.Count > 0)
            {
                foreach (PlayerMobile pm in _SpawnQueue.Keys)
                {
                    UpdatePlayerSpawn(pm, pm.Map, pm.Location, pm == _SpawnQueue.Keys.Last());
                }
            }
        }

        internal static void UpdatePlayerSpawn(PlayerMobile pm, Map map, Point3D location, bool isLast)
        {
            try
            {
                if (_CleanUpList.Count > (SpawnSysUtility.Max_Mobs * _SpawnQueue.Count) && isLast)
                {
                    RunSpawnCleanUp();
                }

                if (IsValidPlayer(pm))
                {
                    if (SpawnSysSettings.SCALE_SPAWN)
                    {
                        var players = pm.GetClientsInRange(SpawnSysSettings.MAX_RANGE);

                        if (players != null)
                        {
                            int playerCount = players.Count();

                            players.Free();

                            if (playerCount > 0)
                            {
                                SpawnSysSettings.UpdateStats(0.1 * playerCount);
                            }
                        }
                    }

                    if (_SpawnQueue[pm].Count > spawn_MinQueued)
                    {
                        var spawnInfo = DequeueSpawn(pm);

                        if (!string.IsNullOrEmpty(spawnInfo.mob))
                        {
                            Mobile mob = SpawnSysUtility.GetSpawn(ref _CleanUpList, spawnInfo.mob);

                            if (mob != null)
                            {
                                lock (_lockObject)
                                {
                                    _TrackedSpawns.Add(mob.Serial);
                                }

                                mob.OnBeforeSpawn(spawnInfo.loc, map);

                                mob.MoveToWorld(spawnInfo.loc, map);

                                Effects.SendLocationEffect(mob.Location, mob.Map, 0x375A, 15, 0, 0);

                                mob.OnAfterSpawn();

                                if (!_SpawnedList.Contains(mob))
                                {
                                    _SpawnedList.Add(mob);
                                }

                                if ((map == Map.Trammel || map == Map.Felucca) && !mob.CanSwim)
                                {
                                    if (map == Map.Felucca)
                                    {
                                        if (!pm.Criminal && !pm.Murderer)
                                        {
                                            mob.Combatant = pm;
                                        }
                                    }
                                    else
                                    {
                                        if (pm.Criminal || pm.Murderer)
                                        {
                                            mob.Combatant = pm;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (isLast)
                    {
                        GetSpawnCleanUp();
                    }
                }

                if (map.Width > location.X && map.Height > location.Y)
                {
                    _ = SpawnSysUtility.LoadSpawn(pm, map, location);
                }
            }
            catch (Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkRed, $"Update Player Spawn Error: {ex.Message}");
            }
        }

        private static void GetSpawnCleanUp()
        {
            foreach (var mobile in _SpawnedList)
            {
                if (mobile.Alive)
                {
                    if (IsTooFar(mobile) && !_CleanUpList.Contains(mobile))
                    {
                        _CleanUpList.Add(mobile);
                    }
                }
            }
        }

        private static bool IsTooFar(Mobile mobile)
        {
            foreach (PlayerMobile pm in _SpawnQueue.Keys)
            {
                if (IsValidPlayer(pm) && mobile.InRange(pm, (int)(SpawnSysUtility.Max_Range * 1.5)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsTooFar(PlayerMobile pm, Point3D location)
        {
            if (IsValidPlayer(pm) && pm.InRange(location, (int)(SpawnSysUtility.Max_Range * 1.5)))
            {
                return false;
            }

            return true;
        }

        private static void RunSpawnCleanUp()
        {
            if (_CleanUpList.Count > 0)
            {
                for (int i = _CleanUpList.Count - 1; i >= 0; i--)
                {
                    if (IsTooFar(_CleanUpList[i]))
                    {
                        if (_SpawnedList.Contains(_CleanUpList[i]))
                        {
                            _SpawnedList.Remove(_CleanUpList[i]);
                        }

                        if (_CleanUpList[i] is BaseCreature bc && (bc.Controlled || bc.IsStabled))
                        {
                            lock (_lockObject)
                            {
                                _TrackedSpawns.Remove(bc.Serial);
                            }
                        }
                        else
                        { 
                            if (_CleanUpList[i].Alive && _CleanUpList[i].Map != Map.Internal)
                            {
                                _CleanUpList[i].Delete();
                            }
                        }
                    }
                }

                _CleanUpList.Clear();
            }
        }

        private static bool IsValidPlayer(PlayerMobile pm)
        {
            return pm.Map != Map.Internal;
        }

        private static void ClearSpawnSystem()
        {
            GetSpawnCleanUp();

            RunSpawnCleanUp();
        }

        #region Persistent Tracking Save/Load

        private static void SaveTrackedSpawns()
        {
            try
            {
                if (!Directory.Exists(SAVE_DIR))
                {
                    Directory.CreateDirectory(SAVE_DIR);
                }

                lock (_lockObject)
                {
                    using (FileStream fs = new FileStream(TRACKED_SPAWNS_FILE, FileMode.Create, FileAccess.Write))
                    using (BinaryWriter writer = new BinaryWriter(fs))
                    {
                        writer.Write(_TrackedSpawns.Count);

                        foreach (Serial serial in _TrackedSpawns)
                        {
                            writer.Write(serial.Value);
                        }
                    }
                }

                SpawnSysUtility.SendConsoleMsg(ConsoleColor.Green, $"Saved {_TrackedSpawns.Count} tracked spawns.");
            }
            catch (Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkRed, $"Save Tracked Spawns Error: {ex.Message}");
            }
        }

        private static void LoadTrackedSpawns()
        {
            try
            {
                if (!File.Exists(TRACKED_SPAWNS_FILE))
                {
                    SpawnSysUtility.SendConsoleMsg(ConsoleColor.Yellow, "No tracked spawns file found. Starting fresh.");
                    return;
                }

                lock (_lockObject)
                {
                    _TrackedSpawns.Clear();

                    using (FileStream fs = new FileStream(TRACKED_SPAWNS_FILE, FileMode.Open, FileAccess.Read))
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        int count = reader.ReadInt32();

                        for (int i = 0; i < count; i++)
                        {
                            Serial serial = (Serial)reader.ReadInt32();

                            Mobile mob = World.FindMobile(serial);

                            if (mob != null && !mob.Deleted)
                            {
                                _TrackedSpawns.Add(serial);
                            }
                        }
                    }
                }

                SpawnSysUtility.SendConsoleMsg(ConsoleColor.Green, $"Loaded {_TrackedSpawns.Count} tracked spawns.");
            }
            catch (Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.DarkRed, $"Load Tracked Spawns Error: {ex.Message}");
            }
        }

        #endregion

        #region Public Command Support Methods

        public static int GetTrackedSpawnCount()
        {
            lock (_lockObject)
            {
                return _TrackedSpawns.Count;
            }
        }

        public static (int ValidCount, int TamedCount, int DeletedCount) GetTrackedSpawnDetails()
        {
            int validCount = 0;
            int tamedCount = 0;
            int deletedCount = 0;

            lock (_lockObject)
            {
                foreach (Serial serial in _TrackedSpawns)
                {
                    Mobile mob = World.FindMobile(serial);

                    if (mob != null && !mob.Deleted)
                    {
                        if (mob is BaseCreature bc && (bc.Controlled || bc.IsStabled))
                        {
                            tamedCount++;
                        }
                        else
                        {
                            validCount++;
                        }
                    }
                    else
                    {
                        deletedCount++;
                    }
                }
            }

            return (validCount, tamedCount, deletedCount);
        }

        public static int ClearAllTrackedSpawns()
        {
            int deletedCount = 0;
            List<Serial> toRemove = new List<Serial>();

            lock (_lockObject)
            {
                foreach (Serial serial in _TrackedSpawns)
                {
                    Mobile mob = World.FindMobile(serial);

                    if (mob != null && !mob.Deleted)
                    {
                        if (mob is BaseCreature bc && (bc.Controlled || bc.IsStabled))
                        {
                            toRemove.Add(serial);
                        }
                        else
                        {
                            mob.Delete();
                            deletedCount++;
                            toRemove.Add(serial);
                        }
                    }
                    else
                    {
                        toRemove.Add(serial);
                    }
                }

                foreach (Serial serial in toRemove)
                {
                    _TrackedSpawns.Remove(serial);
                }

                _SpawnedList.Clear();
                _CleanUpList.Clear();
            }

            SaveTrackedSpawns();

            SpawnSysUtility.SendConsoleMsg(ConsoleColor.Yellow, $"Cleared {deletedCount} tracked spawns.");

            return deletedCount;
        }

        #endregion
    }
}
