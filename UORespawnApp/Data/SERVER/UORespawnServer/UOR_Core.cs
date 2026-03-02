using System;
using System.Linq;
using System.Collections.Generic;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Managers;
using Server.Custom.UORespawnServer.Services;

namespace Server.Custom.UORespawnServer
{
    /// <summary>
    /// 
    /// </summary>
    internal static class UOR_Core
    {
        private static void LoadLogo()
        {
            UOR_Utility.SendMsg(ConsoleColor.DarkCyan, "*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*");
            UOR_Utility.SendMsg(ConsoleColor.Blue, "|-|-|-|-|-|-|-| UORespawn |-|-|-|-|-|-|-|");
            UOR_Utility.SendMsg(ConsoleColor.Blue, "|-|-|-|-|-|-|-|   ~*~*~   |-|-|-|-|-|-|-|");
            UOR_Utility.SendMsg(ConsoleColor.DarkCyan, "*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*");
        }

        internal static bool IsPaused { get; private set; } = true; // false = IsLocked
        internal static bool IsLocked { get; private set; } = false; // Staff Lock @ Respawn System Gump!

        internal static void ToggleLock()
        {
            IsLocked = !IsLocked;

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"LOCKED-[{IsLocked}]");
        }

        private static Dictionary<Serial, RespawnerEntity> _RespawnerList;

        internal static bool GetRespawners(out List<RespawnerEntity> list)
        {
            if (_RespawnerList.Count > 0)
            {
                list = _RespawnerList.Values.ToList();

                return true;
            }

            list = null;

            return false;
        }

        private static ProcessService _ProcessService;

        private static RecycleService _RecycleService;

        private static TrackService _TrackService;

        private static ValidateService _ValidateService;

        private static TimedService _TimedService;

        private static StatsService _StatsService;

        private static VendorService _VendorService;

        private static ControlService _ControlService;

        private static int TotalFlagsCleaned = 0;

        internal static void AddFlag()
        {
            TotalFlagsCleaned++;
        }

        private static int TotalGatesCleaned = 0;

        internal static void AddGate()
        {
            TotalGatesCleaned++;
        }

        public static void Initialize()
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Started]");

            _RespawnerList = new Dictionary<Serial, RespawnerEntity>();

            LogManager.Initialize(); // Start Session Logger!

            LoadLogo();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Sequence Started]");

            UOR_Utility.InitializeUtility(); // Get Utility AllSpawns Ready!

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[1/6]");

            GameManager.InitializeData(); // Create Game Data Files : Output to Editor

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[2/6]");

            SpawnManager.LoadSpawns(); // Load Spawn Data : Input to Server

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[3/6]");

            InitializeServices(); // Get Serrvices Loaded and Ready!

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[4/6]");

            InitializeEvents(); // Hook into the Events!

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[5/6]");

            StartTimers(); // Start Service Timers!

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[6/6]");

            IsPaused = IsLocked; // Enable System : Start!

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Sequence Complete]");

            UOR_Utility.SendMsg(ConsoleColor.Cyan, $"FLAGS-[{TotalFlagsCleaned} Deleted]");

            UOR_Utility.SendMsg(ConsoleColor.Cyan, $"GATES-[{TotalGatesCleaned} Deleted]");

            UOR_Utility.SendMsg(ConsoleColor.Magenta, "STARTED - Running ...");
        }

        private static void InitializeServices()
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Services Created]");

            _ProcessService = new ProcessService();
            _RecycleService = new RecycleService();
            _TrackService = new TrackService();
            _ValidateService = new ValidateService();
            _TimedService = new TimedService();
            _StatsService = new StatsService();
            _VendorService = new VendorService();
            _ControlService = new ControlService();

            UOR_Utility.SendMsg(ConsoleColor.Green, "SERVICES-[Initialized]");
        }

        private static bool _EventsSubscribed = false;

        private static void InitializeEvents()
        {
            // Prevent double subscription
            if (_EventsSubscribed)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Events Already Subscribed]");
                return;
            }

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Subscribe Events]");

            EventSink.TameCreature += EventSink_TameCreature;
            EventSink.CreatureDeath += EventSink_CreatureDeath;
            EventSink.MobileDeleted += EventSink_MobileDeleted;
            EventSink.BeforeWorldSave += EventSink_BeforeWorldSave;
            EventSink.AfterWorldSave += EventSink_AfterWorldSave;
            EventSink.Login += EventSink_Login;
            EventSink.Logout += EventSink_Logout;
            EventSink.Shutdown += EventSink_Shutdown;
            EventSink.Crashed += EventSink_Crashed;

            _EventsSubscribed = true;

            UOR_Utility.SendMsg(ConsoleColor.Green, "EVENTS-[Subscribed]");
        }

        private static void UnsubscribeEvents()
        {
            if (!_EventsSubscribed)
                return;

            EventSink.TameCreature -= EventSink_TameCreature;
            EventSink.CreatureDeath -= EventSink_CreatureDeath;
            EventSink.MobileDeleted -= EventSink_MobileDeleted;
            EventSink.BeforeWorldSave -= EventSink_BeforeWorldSave;
            EventSink.AfterWorldSave -= EventSink_AfterWorldSave;
            EventSink.Login -= EventSink_Login;
            EventSink.Logout -= EventSink_Logout;
            EventSink.Shutdown -= EventSink_Shutdown;
            EventSink.Crashed -= EventSink_Crashed;

            _EventsSubscribed = false;

            UOR_Utility.SendMsg(ConsoleColor.Green, "EVENTS-[Unsubscribed]");
        }

        private static void EventSink_TameCreature(TameCreatureEventArgs e)
        {
            // ISpawner handles release automatically when creature is tamed
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"UORespawn-[{e.Creature.Name} Tamed]");
        }

        private static void EventSink_CreatureDeath(CreatureDeathEventArgs e)
        {
            // ISpawner handles release automatically when creature dies
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"UORespawn-[{e.Creature.Name} Killed]");
        }

        private static void EventSink_MobileDeleted(MobileDeletedEventArgs e)
        {
            // ISpawner handles release automatically when mobile is deleted
        }

        private static void EventSink_BeforeWorldSave(BeforeWorldSaveEventArgs e)
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"UORespawn-[Saving...]");

            IsPaused = true;
        }

        private static void EventSink_AfterWorldSave(AfterWorldSaveEventArgs e)
        {
            IsPaused = IsLocked;

            _StatsService.Save();
            _VendorService.Save();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"UORespawn-[Saved]");
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm && !_RespawnerList.ContainsKey(pm.Serial))
            {
                _RespawnerList.Add(pm.Serial, new RespawnerEntity(pm));

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"UORespawn-[{pm.Name} Logged In]");
            }

            IsPaused = IsLocked;
        }

        private static void EventSink_Logout(LogoutEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm && _RespawnerList.ContainsKey(pm.Serial))
            {
                _RespawnerList[pm.Serial].Stop();
                _RespawnerList.Remove(pm.Serial);

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"UORespawn-[{pm.Name} Logged Out]");
            }

            if (_RespawnerList.Count == 0)
            {
                IsPaused = true;

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"UORespawn-[Paused]");
            }
        }

        private static void EventSink_Shutdown(ShutdownEventArgs e)
        {
            LogManager.FlushToFile("Server Shutdown");

            StopTimers();
        }

        private static void EventSink_Crashed(CrashedEventArgs e)
        {
            LogManager.FlushToFile("Server Crash");

            StopTimers();
        }

        private static void StartTimers()
        {
            _ProcessService.Start();
            _ValidateService.Start();
            _TimedService.Start();

            UOR_Utility.SendMsg(ConsoleColor.Green, "TIMERS-[Initialized]");
        }

        private static void StopTimers()
        {
            _ProcessService.Stop();
            _ValidateService.Stop();
            _TimedService.Stop();

            UOR_Utility.SendMsg(ConsoleColor.Green, "TIMERS-[Stopped]");
        }

        internal static void SendToRecycled(Serial serial)
        {
            // ISpawner releases ownership in RecycleService.Add()
            _RecycleService.Add(serial);
        }

        internal static Mobile GetRecycled(string name, out bool isRecycled)
        {
            Mobile m = _RecycleService.Remove(0, name);

            if (m == null)
            {
                isRecycled = false;

                return UOR_Utility.CreateSpawn(name);
            }

            isRecycled = true;

            return m;
        }

        internal static int GetRecycledCount()
        {
            return _RecycleService.GetRecycledTotal();
        }

        internal static int GetPlayerCount()
        {
            return _RespawnerList.Count;
        }

        internal static void AddStat(PlayerMobile pm, Mobile spawn)
        {
            _StatsService.AddStats(pm, spawn);
        }

        internal static void UpdateTimed()
        {
            _VendorService.UpdateTime();
        }

        internal static void UpdateVendorService()
        {
            _VendorService.ResetVendors();
        }

        /// <summary>
        /// Respawns vendors at a specific location using ISpawner pattern.
        /// Deletes existing vendors near location and spawns from updated config.
        /// </summary>
        internal static int RespawnVendorsAtLocation(Map map, VendorEntity entity)
        {
            return _VendorService.RespawnVendorsAtLocation(map, entity);
        }

        internal static void OpenControlGump(PlayerMobile pm)
        {
            _ControlService.OpenGump(pm);
        }

        internal static void SHUTDOWN()
        {
            IsLocked = true;

            // Unsubscribe events to prevent double subscription on restart
            UnsubscribeEvents();

            // Clean up all vendor spawn via ISpawner
            _VendorService.DeleteAllVendors();

            // Clear recycled pool
            _RecycleService.ClearRecycled();

            // Clean up all mob spawn via ISpawner pattern
            int deleted = UOR_Utility.ClearAllSpawns();

            UOR_Utility.SendMsg(ConsoleColor.Magenta, $"SHUTDOWN - Stopped ({deleted} spawn deleted)");
        }

        internal static void RelogPlayers()
        {
            var instances = Network.NetState.Instances;

            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i].Mobile is PlayerMobile pm && !_RespawnerList.ContainsKey(pm.Serial))
                {
                    _RespawnerList.Add(pm.Serial, new RespawnerEntity(pm));

                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"UORespawn-[{pm.Name} Relogged In]");
                }
            }
        }
    }
}
