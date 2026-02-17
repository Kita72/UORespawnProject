using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Server.Mobiles;
using Server.Custom.UORespawnSystem.Services;
using Server.Custom.UORespawnSystem.SpawnUtility;
using Server.Custom.UORespawnSystem.SpawnHelpers;

namespace Server.Custom.UORespawnSystem
{
    internal static class UORespawnCore
    {
        #region Console Close Handler (Windows API)

        // Windows API for handling console close events
        private delegate bool ConsoleEventDelegate(int eventType);
        private static ConsoleEventDelegate _consoleHandler;

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate handler, bool add);

        private const int CTRL_CLOSE_EVENT = 2;
        private const int CTRL_LOGOFF_EVENT = 5;
        private const int CTRL_SHUTDOWN_EVENT = 6;

        #endregion

        private static SpawnTimer _SpawnTimer;

        internal static List<PlayerMobile> _Players;

        private static List<(Mobile mob, bool isTooFar)> _SpawnedList;

        internal static Dictionary<PlayerMobile, Queue<(string mob, Point3D loc)>> _SpawnQueue;

        internal static bool HasChanged { get; set; } = false;

        private static bool _isPaused = false;

        private static void UpdateSpawnedList(Mobile m)
        {
            for (int i = _SpawnedList.Count - 1; i >= 0; i--)
            {
                var (mob, _) = _SpawnedList[i];

                if (mob == m)
                {
                    _SpawnedList.RemoveAt(i);
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"RELEASED: {m?.Name ?? "Unknown"} removed from spawn list (tamed/killed)");
                    return;
                }
            }
        }

        internal static void EnqueueSpawn(PlayerMobile pm, string mob, Point3D loc)
        {
            if (_SpawnQueue.ContainsKey(pm))
            {
                _SpawnQueue[pm].Enqueue((mob, loc));

                // Record metrics
                SpawnMetricsService.RecordQueue(_SpawnQueue[pm].Count);

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"QUEUED: {mob} at {loc.X},{loc.Y},{loc.Z} for player {pm.Name} (Queue: {_SpawnQueue[pm].Count})");
            }
        }

        public static void Initialize()
        {
            SpawnDebugService.WriteSessionStart();

            UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkGreen, $"Started => {DateTime.Now:t}");

            LoadLogo();

            UORespawnSettings.VerifyDirectories();

            UORespawnDataBase.LoadSpawns();

            InitializeLists();

            InitializeServices();

            SubscribeEvents();

            RegisterConsoleCloseHandler();

            StartTimer();

            UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkCyan, "System Running...");
        }

        /// <summary>
        /// Register console close handler to cleanup spawns when console window is closed
        /// (X button, task kill, system shutdown, etc.)
        /// </summary>
        private static void RegisterConsoleCloseHandler()
        {
            try
            {
                _consoleHandler = new ConsoleEventDelegate(ConsoleEventCallback);
                SetConsoleCtrlHandler(_consoleHandler, true);
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Console Close Handler Registered...");
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Console close handler registration failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Callback for console close events (X button, logoff, shutdown)
        /// </summary>
        private static bool ConsoleEventCallback(int eventType)
        {
            switch (eventType)
            {
                case CTRL_CLOSE_EVENT:
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "CONSOLE CLOSE: Flushing debug log and cleaning spawns...");
                    SpawnDebugService.FlushToFile("Console close (X button)");
                    ClearAllSpawns();
                    return true;

                case CTRL_LOGOFF_EVENT:
                case CTRL_SHUTDOWN_EVENT:
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "SYSTEM SHUTDOWN: Flushing debug log and cleaning spawns...");
                    SpawnDebugService.FlushToFile("System shutdown/logoff");
                    ClearAllSpawns();
                    return true;

                default:
                    return false;
            }
        }

        private static void LoadLogo()
        {
            UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkCyan, "*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*");
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Blue, "|-|-|-|-|-|-|-| UORespawn |-|-|-|-|-|-|-|");
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Blue, "|-|-|-|-|-|-|-|   ~*~*~   |-|-|-|-|-|-|-|");
            UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkCyan, "*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*");
        }

        private static void InitializeLists()
        {
            _SpawnQueue = new Dictionary<PlayerMobile, Queue<(string mob, Point3D loc)>>();

            _Players = new List<PlayerMobile>();

            _SpawnedList = new List<(Mobile mob, bool isTooFar)>();

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Lists Initialized...");
        }

        private static void InitializeServices()
        {
            // Initialize new cleanup services
            SpawnRecycleService.Initialize();
            SpawnDistanceService.Initialize(_SpawnedList, _Players);
            SpawnCleanupService.Initialize(_SpawnedList, OnCleanupComplete);

            // Start the service timers (separate from spawn timer)
            SpawnDistanceService.Start();
            SpawnCleanupService.Start();
        }

        /// <summary>
        /// Callback invoked when cleanup service completes a cycle
        /// </summary>
        private static void OnCleanupComplete()
        {
            // Optional: Update stats or trigger other actions after cleanup
            // Currently just a placeholder for future enhancements
        }

        private static void SubscribeEvents()
        {
            EventSink.TameCreature += EventSink_TameCreature;

            EventSink.CreatureDeath += EventSink_CreatureDeath;

            EventSink.MobileDeleted += EventSink_MobileDeleted;

            EventSink.BeforeWorldSave += EventSink_BeforeWorldSave;

            EventSink.AfterWorldSave += EventSink_AfterWorldSave;

            EventSink.Login += EventSink_Login;

            EventSink.Logout += EventSink_Logout;

            EventSink.Shutdown += EventSink_Shutdown;

            EventSink.Crashed += EventSink_Crashed;

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Events Subscribed...");
        }

        private static void StartTimer()
        {
            _SpawnTimer = new SpawnTimer();

            _SpawnTimer.Start();

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Timer Activated...");
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

            // Stop cleanup services during save
            SpawnDistanceService.Stop();
            SpawnCleanupService.Stop();
        }

        private static void EventSink_AfterWorldSave(AfterWorldSaveEventArgs e)
        {
            _SpawnTimer.Start();

            // Restart cleanup services after save
            SpawnDistanceService.Start();
            SpawnCleanupService.Start();

            try
            {
                SpawnFactory.SaveStats();
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Failed to save stats file - {ex.Message}");
            }
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                AddPlayer(pm);
            }
        }

        private static void EventSink_Logout(LogoutEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                RemovePlayer(pm);
            }
        }

        private static void EventSink_Shutdown(ShutdownEventArgs e)
        {
            SpawnDebugService.FlushToFile("Server shutdown");
            ClearAllSpawns();
        }

        private static void EventSink_Crashed(CrashedEventArgs e)
        {
            SpawnDebugService.FlushToFile("Server crash");
            ClearAllSpawns();
        }

        internal static void AddPlayer(PlayerMobile pm)
        {
            if (!_SpawnQueue.ContainsKey(pm))
            {
                _SpawnQueue.Add(pm, new Queue<(string mob, Point3D loc)>());

                _Players.Add(pm);

                HasChanged = true;

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"PLAYER LOGIN: {pm.Name} added to spawn system (Total: {_Players.Count} players)");
            }
        }

        internal static void RemovePlayer(PlayerMobile pm)
        {
            if (_SpawnQueue.Count > 0 && _SpawnQueue.ContainsKey(pm))
            {
                _SpawnQueue.Remove(pm);
            }

            if (_Players.Count > 0 && _Players.Contains(pm))
            {
                _Players.Remove(pm);

                // Record player logout in metrics
                SpawnMetricsService.RecordPlayerLogout(pm);

                HasChanged = true;

                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"PLAYER LOGOUT: {pm.Name} removed from spawn system (Remaining: {_Players.Count} players)");
            }
        }

        //Override Method : Dev Only
        internal static void UpdateWorldSpawn()
        {
            if (_SpawnQueue.Count > 0)
            {
                foreach (PlayerMobile pm in _SpawnQueue.Keys)
                {
                    UpdatePlayerSpawn(pm);
                }
            }
        }

        internal static void UpdatePlayerSpawn(PlayerMobile pm)
        {
            try
            {
                // 0. Check if system is paused
                if (_isPaused)
                {
                    return; // Skip all spawning logic
                }

                if (UORespawnUtility.IsValidPlayer(pm))
                {
                    // 1. Queue Protection: If this player already has mobs waiting in queue, 
                    // don't waste CPU looking for more until the timer processes them.
                    if (_SpawnQueue.TryGetValue(pm, out var queue) && queue.Count >= UORespawnSettings.MAX_QUEUE_SIZE)
                        return;

                    // 2. Scaling Logic (MUST happen BEFORE max mob check!)
                    // When multiple players are near each other, increase spawn cap proportionally
                    if (UORespawnSettings.ENABLE_SCALE_SPAWN)
                    {
                        var clients = pm.GetClientsInRange(UORespawnSettings.MAX_RANGE);

                        try
                        {
                            if (clients != null)
                            {
                                int playerCount = clients.Count();

                                if (playerCount > 0)
                                {
                                    // Apply multiplier: 0.1 per player (e.g., 3 players = 0.3 multiplier)
                                    // This makes MAX_MOBS return: baseMobs + (baseMobs * multiplier)
                                    UORespawnSettings.UpdateStats(0.1 * playerCount);
                                }
                            }
                        }
                        finally
                        {
                            clients?.Free();
                        }
                    }
                    else
                    {
                        // Scaling disabled - reset to base values
                        UORespawnSettings.UpdateStats(0.0);
                    }

                    // 3. Max Spawn Per Player Check: Now uses scaled MAX_MOBS if scaling is enabled
                    // Example: 15 base mobs + (15 * 0.3) = ~19 mobs when 3 players are nearby
                    int playerSpawnCount = GetPlayerSpawnCount(pm);
                    if (playerSpawnCount >= UORespawnSettings.MAX_MOBS)
                        return;

                    // 4. Deciding logic: This finds a spot and adds it to the Queue
                    UORespawnUtility.LoadSpawn(pm, pm.Map, pm.Location);
                }
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: UpdatePlayerSpawn failed for {pm?.Name ?? "Unknown"} - {ex.Message}");
            }
        }

        public static void ProcessQueue(PlayerMobile pm)
        {
            if (_SpawnQueue.TryGetValue(pm, out var queue) && queue.Count > 0)
            {
                var (mobTypeName, loc) = queue.Dequeue();

                if (!UORespawnUtility.IsValidPlayer(pm)) return;

                Map map = pm.Map;

                // 1. Try to get from recycle pool first
                Mobile m = SpawnRecycleService.TryGetRecycled(mobTypeName);
                bool wasRecycled = (m != null);

                // 2. If not recycled, create new spawn
                if (m == null)
                {
                    m = UORespawnUtility.CreateSpawn(mobTypeName);
                }

                if (m != null)
                {
                    // 3. Prepare for spawn
                    if (!wasRecycled)
                    {
                        m.OnBeforeSpawn(loc, map);
                    }
                    else
                    {
                        // Restore recycled spawn to full health/mana/stam
                        RestoreRecycledSpawn(m);
                    }

                    // 4. Place in the world
                    m.MoveToWorld(loc, map);

                    // 5. Visual effect
                    Effects.SendLocationEffect(m.Location, m.Map, 0x375A, 15, 0, 0);

                    // 6. Complete spawn
                    if (!wasRecycled)
                        m.OnAfterSpawn();

                    // 7. Add to spawned list (tuple with isTooFar=false initially)
                    _SpawnedList.Add((m, false));

                    // 8. Record Metrics
                    SpawnMetricsService.RecordSpawn(pm, wasRecycled);

                    // 9. Debug Logging
                    if (wasRecycled)
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkYellow, $"RECYCLED: {m.Name} teleported to {loc.X},{loc.Y},{loc.Z} for player {pm.Name}");
                    }
                    else
                    {
                        UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"NEW SPAWN: {m.Name} created at {loc.X},{loc.Y},{loc.Z} for player {pm.Name}");
                    }

                    // 10. Aggression Logic (Combatant assignment)
                    if (map == Map.Felucca)
                    {
                        // In Felucca, spawn attacks everyone except criminals/murderers? 
                        // (Adjust this logic based on your specific server rules)
                        if (!pm.Criminal && !pm.Murderer)
                        {
                            m.Combatant = pm;
                        }
                    }
                    else // Trammel rules
                    {
                        if (pm.Criminal || pm.Murderer)
                        {
                            m.Combatant = pm;
                        }
                    }

                    // 11. Staff Debugging
                    if (pm.IsStaff() && UORespawnSettings.ENABLE_DEBUG)
                    {
                        if (wasRecycled)
                        {
                            pm.SendMessage(53, $"{m.Name} moved to {m.Location}");
                        }
                        else
                        {
                            pm.SendMessage(62, $"{m.Name} spawned at {m.Location}");
                        }
                    }
                }
                else
                {
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, $"ERROR: Failed to spawn '{mobTypeName}' for player {pm.Name} at {loc.X},{loc.Y},{loc.Z}");
                }
            }
        }

        internal static void AddSpawnToList(Mobile m)
        {
            if (_SpawnedList != null && m != null)
            {
                foreach (var (mob, _) in _SpawnedList)
                {
                    if (mob == m)
                        return; // Already in list
                }

                _SpawnedList.Add((m, false));
            }
        }

        /// <summary>
        /// Restore a recycled spawn to full health, mana, and stamina.
        /// Also clears any debuffs and combat state.
        /// </summary>
        private static void RestoreRecycledSpawn(Mobile m)
        {
            if (m == null || m.Deleted || m.Hits == m.HitsMax)
                return;

            // Restore to full health/mana/stam
            m.Hits = m.HitsMax;
            m.Mana = m.ManaMax;
            m.Stam = m.StamMax;

            // Clear combat and targeting
            m.Combatant = null;

            // Clear poison
            if (m.Poisoned)
            {
                m.CurePoison(m);
            }

            // Reset body temperature (if affected by fire/cold)
            if (m is BaseCreature bc)
            {
                // Clear aggressor list
                bc.Aggressors?.Clear();
                bc.Aggressed?.Clear();

                // Reset AI state
                bc.ControlOrder = OrderType.None;
                bc.ControlTarget = null;
                bc.ControlMaster = null;

                // Clear combat-related flags
                bc.Warmode = false;
            }

            if (UORespawnSettings.ENABLE_DEBUG)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkYellow, 
                    $"RESTORED: {m.Name} - HP:{m.Hits}/{m.HitsMax} Mana:{m.Mana}/{m.ManaMax} Stam:{m.Stam}/{m.StamMax}");
            }
        }

        // Using spatial queries (no cache)
        private static int GetPlayerSpawnCount(PlayerMobile pm)
        {
            if (pm == null || pm.Map == null || pm.Map == Map.Internal)
                return 0;

            int count = 0;
            int maxRange = (int)(UORespawnSettings.MAX_RANGE * 1.5);

            // Use spatial indexing to find mobs near this player
            IPooledEnumerable eable = pm.Map.GetMobilesInRange(pm.Location, maxRange);

            try
            {
                foreach (Mobile m in eable)
                {
                    // Check if this mob is in our spawn system
                    foreach (var (mob, isTooFar) in _SpawnedList)
                    {
                        if (mob == m && !isTooFar)
                        {
                            count++;
                            break;
                        }
                    }
                }
            }
            finally
            {
                eable?.Free();
            }

            return count;
        }

        #region Spawn Cleanup Methods

        /// <summary>
        /// Complete cleanup of all spawns - used for shutdown, crashes, console close, and manual clearing
        /// Handles cleanup service, spawned list, and recycle pool
        /// </summary>
        /// <param name="reason">Reason for cleanup (for logging)</param>
        /// <param name="returnCount">Whether to return the count (for commands)</param>
        /// <returns>Number of spawns deleted (if returnCount is true)</returns>
        private static int PerformCompleteCleanup(string reason, bool returnCount = false)
        {
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, $"CLEANUP START: {reason}");

            try
            {
                // 1. Force immediate cleanup through cleanup service (handles delayed deletions)
                SpawnCleanupService.ForceCleanup();

                // 2. Delete all remaining spawns in spawned list
                int deletedCount = 0;

                foreach (var (mob, _) in _SpawnedList)
                {
                    if (mob != null && !mob.Deleted)
                    {
                        mob.Delete();
                        deletedCount++;
                    }
                }

                _SpawnedList.Clear();

                // 3. Clear recycle pool (prevents recycled mobs from persisting)
                int recycledCount = SpawnRecycleService.GetTotalRecycled();
                SpawnRecycleService.ClearAll();

                // 4. Record metrics
                SpawnMetricsService.RecordCleanup(deletedCount, recycledCount, reason);

                // 5. Log results
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                    $"CLEANUP COMPLETE: Deleted {deletedCount} active + {recycledCount} recycled = {deletedCount + recycledCount} total spawns");

                return returnCount ? deletedCount : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Internal cleanup for shutdown/crash events
        /// </summary>
        private static void ClearAllSpawns()
        {
            PerformCompleteCleanup("Shutdown/crash cleanup initiated", returnCount: false);
        }

        /// <summary>
        /// Public cleanup for commands - returns count for user feedback
        /// </summary>
        public static int ClearAllSpawns(string reason = "Manual clear command")
        {
            return PerformCompleteCleanup(reason, returnCount: true);
        }

        #endregion

        #region Status/Stats Methods

        /// <summary>
        /// Get active spawn count (for GUI)
        /// </summary>
        public static int GetActiveSpawnCount()
        {
            return _SpawnedList?.Count ?? 0;
        }

        /// <summary>
        /// Get recycle pool count (for GUI)
        /// </summary>
        public static int GetRecyclePoolCount()
        {
            return SpawnRecycleService.GetTotalRecycled();
        }

        /// <summary>
        /// Get active player count (for GUI)
        /// </summary>
        public static int GetActivePlayerCount()
        {
            return _Players?.Count ?? 0;
        }

        #endregion

        #region Pause/Resume System

        /// <summary>
        /// Check if spawn system is paused
        /// </summary>
        public static bool IsPaused()
        {
            return _isPaused;
        }

        /// <summary>
        /// Pause the spawn system (stops new spawns, cleanup still runs)
        /// </summary>
        public static void Pause()
        {
            _isPaused = true;
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "SYSTEM PAUSED: New spawns stopped, cleanup continues");
        }

        /// <summary>
        /// Resume the spawn system
        /// </summary>
        public static void Resume()
        {
            _isPaused = false;
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "SYSTEM RESUMED: Spawning restarted");
        }

        #endregion
    }
}
