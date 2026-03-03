using System;
using System.Linq;
using System.Collections.Generic;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Core;
using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Managers;
using Server.Custom.UORespawnServer.Services;
using Server.Custom.UORespawnServer.Spawners;

namespace Server.Custom.UORespawnServer
{
    /// <summary>
    /// UORespawn Core - Central initialization and management.
    /// 
    /// STARTUP SEQUENCE:
    /// 1. Initialize() - called during ScriptCompiler.Invoke("Initialize")
    ///    - Minimal setup: logo, utility, data loading
    ///    - Subscribes to ServerStarted for deferred startup
    /// 
    /// 2. OnServerStarted() - called after World.Load() completes
    ///    - Reclaims spawner references (Mobile.Spawner not serialized)
    ///    - Cleans up mob spawn (fresh world)
    ///    - Initializes vendors (checks for existing)
    ///    - Creates services and starts timers
    ///    - Subscribes to game events
    /// 
    /// This ensures World.Mobiles is fully populated before any spawn operations.
    /// </summary>
    internal static class UOR_Core
    {
        private static void LoadLogo()
        {
            UOR_Utility.SendMsg(ConsoleColor.DarkBlue, $"*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~{UOR_Settings.VERSION}~~*");
            UOR_Utility.SendMsg(ConsoleColor.Blue, "*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*");
            UOR_Utility.SendMsg(ConsoleColor.DarkMagenta, "|-|-|-|-|-|-|-| UORespawn |-|-|-|-|-|-|-|");
            UOR_Utility.SendMsg(ConsoleColor.Magenta, "|-|-|-|-|-|-|-| ~~~*G*~~~ |-|-|-|-|-|-|-|");
            UOR_Utility.SendMsg(ConsoleColor.Magenta, "|-|-|-|-|-|-|-|  ~~1#3~~  |-|-|-|-|-|-|-|");
            UOR_Utility.SendMsg(ConsoleColor.Magenta, "|-|-|-|-|-|-|-|   ~*D*~   |-|-|-|-|-|-|-|");
            UOR_Utility.SendMsg(ConsoleColor.Blue, "*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*");
            UOR_Utility.SendMsg(ConsoleColor.DarkBlue, "*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~Wilson~~*");
        }

        /// <summary>
        /// Whether the respawn system is paused (no spawning occurs).
        /// </summary>
        internal static bool IsPaused { get; private set; } = true;

        /// <summary>
        /// Staff lock toggle from Respawn System Gump - prevents system state changes.
        /// </summary>
        internal static bool IsLocked { get; private set; } = false;

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

        #region Startup Sequence

        /// <summary>
        /// Called during ScriptCompiler.Invoke("Initialize") - BEFORE World.Load completes.
        /// Performs minimal setup and defers main initialization to ServerStarted.
        /// </summary>
        public static void Initialize()
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Started]");

            _RespawnerList = new Dictionary<Serial, RespawnerEntity>();

            LogManager.Initialize(); // Start Session Logger!

            LoadLogo();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Pre-Load Setup]");

            UOR_Utility.InitializeUtility(); // Get Utility Ready!

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[1/2]");

            GameManager.InitializeData(); // Create Game Data Files : Output to Editor

                        XmlCommandProcessor.ProcessCommands(); // Process XML spawner commands from Editor

                        UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[2/2]");

            SpawnManager.LoadSpawns(); // Load Spawn Data : Input to Server

            // Subscribe to ServerStarted - fires AFTER World.Load() completes
            EventSink.ServerStarted += OnServerStarted;

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Waiting for ServerStarted...]");
        }

        /// <summary>
        /// Called when server has fully started - World loaded, scripts initialized, timers running.
        /// This is the main initialization entry point where World.Mobiles is fully populated.
        /// </summary>
        private static void OnServerStarted()
        {
            // Unsubscribe - one-time startup only
            EventSink.ServerStarted -= OnServerStarted;

            UOR_Utility.SendMsg(ConsoleColor.DarkCyan, $"Respawn-[ServerStarted - Beginning Full Init]");

            // PHASE 1: Reclaim spawner references (Mobile.Spawner is not serialized)
            ReclaimSpawners();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[1/5]");

            // PHASE 2: Clean up mob spawn from previous session (fresh world)

            CleanupMobSpawn();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[2/5]");

            // PHASE 3: Initialize vendors (checks for existing, only spawns if needed)
            InitializeVendors();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[3/5]");

            // PHASE 4: Create services
            InitializeServices();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[4/5]");

            // PHASE 5: Subscribe to game events
            InitializeEvents();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[5/5]");

            // PHASE 6: Start service timers
            StartTimers();

            // Enable system
            IsPaused = IsLocked;

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Sequence Complete]");

            UOR_Utility.SendMsg(ConsoleColor.DarkBlue, "STARTED - Running ...");
        }

        /// <summary>
        /// Reclaims all spawn from spawners after world load.
        /// Mobile.Spawner is not serialized by ServUO, so we must restore references.
        /// </summary>
        private static void ReclaimSpawners()
        {
            int mobsReclaimed = UOR_MobSpawner.Instance.ReclaimAll();
            int vendorsReclaimed = UOR_VendorSpawner.Instance.ReclaimAll();

            if (mobsReclaimed > 0)
                UOR_Utility.SendMsg(ConsoleColor.Cyan, $"SPAWN-[{mobsReclaimed} Reclaimed]");

            if (vendorsReclaimed > 0)
                UOR_Utility.SendMsg(ConsoleColor.Cyan, $"VENDORS-[{vendorsReclaimed} Reclaimed]");

            if (TotalFlagsCleaned > 0)
                UOR_Utility.SendMsg(ConsoleColor.Cyan, $"FLAGS-[{TotalFlagsCleaned} Deleted]");

            if (TotalGatesCleaned > 0)
                UOR_Utility.SendMsg(ConsoleColor.Cyan, $"GATES-[{TotalGatesCleaned} Deleted]");
        }

        /// <summary>
        /// Deletes all mob spawn from previous server session.
        /// Keeps world fresh for players. Vendors persist across restarts.
        /// </summary>
        private static void CleanupMobSpawn()
        {
            int deleted = UOR_MobSpawner.CleanupAll();

            if (deleted > 0)
                UOR_Utility.SendMsg(ConsoleColor.Cyan, $"SPAWN-[{deleted} Deleted]");

            UOR_Utility.SendMsg(ConsoleColor.Magenta, "Fresh World Ready!");
        }

        /// <summary>
        /// Initializes vendors - only spawns if none exist from previous save.
        /// Must run AFTER ReclaimSpawners() so GetCount() returns accurate values.
        /// </summary>
        private static void InitializeVendors()
        {
            if (!UOR_Settings.ENABLE_VENDOR_SPAWN)
            {
                // Delete any existing vendors if system is disabled
                int deleted = UOR_VendorSpawner.CleanupAll();
                if (deleted > 0)
                {
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{deleted} Deleted - System Disabled]");
                }
                return;
            }

            var vendorSpawns = SpawnManager.VendorSpawns;

            if (vendorSpawns == null || vendorSpawns.Count == 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[No vendor spawn data found]");
                return;
            }

            // Check if vendors already exist (via ISpawner pattern)
            int existingCount = UOR_VendorSpawner.GetCount();

            if (existingCount > 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Green, $"VENDORS-[{existingCount} persisted from save]");
                return;
            }

            // No existing vendors - spawn them
            int totalSpawned = 0;
            int totalEntities = 0;

            foreach (var kvp in vendorSpawns)
            {
                Map map = kvp.Key;

                foreach (var entity in kvp.Value)
                {
                    totalEntities++;

                    if (entity.VendorList.Count > 0)
                    {
                        int spawned = VendorSpawner.SpawnVendors(map, entity);
                        totalSpawned += spawned;
                    }
                }
            }

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{totalSpawned} spawned across {totalEntities} locations]");
        }

        /// <summary>
        /// Creates service instances. Called during ServerStarted.
        /// Note: TrackService and VendorService functionality is now handled directly in OnServerStarted.
        /// </summary>
        private static void InitializeServices()
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[Services Created]");

            _ProcessService = new ProcessService();
            _RecycleService = new RecycleService();
            _TrackService = new TrackService();      // Now a simple service, no ServerStarted subscription
            _ValidateService = new ValidateService();
            _TimedService = new TimedService();
            _StatsService = new StatsService();
            _VendorService = new VendorService();    // Now a simple service, no ServerStarted subscription
            _ControlService = new ControlService();

            UOR_Utility.SendMsg(ConsoleColor.Green, "SERVICES-[Initialized]");
        }

        #endregion

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
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"UORespawn-[{e.Mobile.Name} Deleted]");
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
            _TrackService.CleanUpItems();

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
            _RecycleService.Add(serial);
        }

        internal static Mobile GetRecycled(string name, out bool isRecycled)
        {
            Mobile m = _RecycleService.Remove(name);

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
            IsPaused = true;

            // Stop all timers first
            StopTimers();

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

        /// <summary>
        /// Starts the respawn system at runtime (power toggle ON).
        /// Unlike Initialize() + OnServerStarted() which are for server boot,
        /// this method can be safely called while the server is running.
        /// </summary>
        internal static void STARTUP()
        {
            UOR_Utility.SendMsg(ConsoleColor.DarkCyan, $"Respawn-[STARTUP - Runtime Init]");

            // PHASE 1: Vendors - spawn if none exist (ISpawner manages ownership)
            InitializeVendors();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[1/3]");

            // PHASE 2: Subscribe to game events (if not already subscribed)
            InitializeEvents();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[2/3]");

            // PHASE 3: Start service timers
            StartTimers();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[3/3]");

            // Enable system
            IsLocked = false;
            IsPaused = false;

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"Respawn-[STARTUP Complete]");

            UOR_Utility.SendMsg(ConsoleColor.DarkBlue, "STARTED - Running ...");
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
