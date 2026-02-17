using System;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Custom.UORespawnSystem.SpawnUtility;

namespace Server.Custom.UORespawnSystem.Services
{
    /// <summary>
    /// Cleanup service runs on a separate timer (10 seconds).
    /// Processes spawns marked as too far and routes them to recycle or deletion.
    /// </summary>
    internal static class SpawnCleanupService
    {
        private static Timer _cleanupTimer;
        private static List<Mobile> _deletionQueue;
        private static bool _isRunning = false;

        // Reference to core's spawned list (set during initialization)
        private static List<(Mobile mob, bool isTooFar)> _spawnedListRef;
        private static Action _onCleanupCompleteCallback;

        /// <summary>
        /// Initialize the cleanup service
        /// </summary>
        internal static void Initialize(
            List<(Mobile mob, bool isTooFar)> spawnedListRef,
            Action onCleanupCompleteCallback)
        {
            _spawnedListRef = spawnedListRef;
            _onCleanupCompleteCallback = onCleanupCompleteCallback;
            _deletionQueue = new List<Mobile>();

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Cleanup Service Initialized...");
        }

        /// <summary>
        /// Start the cleanup timer
        /// </summary>
        internal static void Start()
        {
            if (_isRunning)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "CLEANUP SERVICE: Already running");
                return;
            }

            _cleanupTimer = Timer.DelayCall(
                TimeSpan.FromSeconds(UORespawnSettings.CLEANUP_INTERVAL),
                TimeSpan.FromSeconds(UORespawnSettings.CLEANUP_INTERVAL),
                OnTimerTick);

            _isRunning = true;

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green,
                $"CLEANUP SERVICE: Started (Interval: {UORespawnSettings.CLEANUP_INTERVAL}s)");
        }

        /// <summary>
        /// Stop the cleanup timer
        /// </summary>
        internal static void Stop()
        {
            if (_cleanupTimer != null && _cleanupTimer.Running)
            {
                _cleanupTimer.Stop();
                _cleanupTimer = null;
            }

            _isRunning = false;

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "CLEANUP SERVICE: Stopped");
        }

        /// <summary>
        /// Timer tick - process cleanup
        /// </summary>
        private static void OnTimerTick()
        {
            try
            {
                // Start timing
                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

                ProcessTooFarSpawns();
                BatchDelete();

                // Stop timing and record metrics
                stopwatch.Stop();
                double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
                SpawnMetricsService.RecordCleanupTime(elapsedMs);

                if (UORespawnSettings.ENABLE_DEBUG)
                {
                    UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkYellow,
                        $"CLEANUP CYCLE: Completed in {elapsedMs:F2}ms");
                }
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red,
                    $"ERROR: Cleanup service cycle failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Process all spawns marked as too far.
        /// Route to recycle pool or deletion queue.
        /// </summary>
        private static void ProcessTooFarSpawns()
        {
            if (_spawnedListRef == null || _spawnedListRef.Count == 0)
                return;

            int recycledCount = 0;
            int markedForDeletionCount = 0;
            int invalidRemoved = 0;

            // Process from end to beginning (safe removal)
            for (int i = _spawnedListRef.Count - 1; i >= 0; i--)
            {
                var (mob, isTooFar) = _spawnedListRef[i];

                // Remove invalid/dead mobs immediately
                if (mob == null || mob.Deleted)
                {
                    _spawnedListRef.RemoveAt(i);
                    invalidRemoved++;
                    continue;
                }

                // Skip if not too far
                if (!isTooFar)
                    continue;

                // Too far - attempt to recycle or mark for deletion
                bool canRecycle = false;

                // Only recycle if alive and not in combat
                if (mob.Alive && mob is BaseCreature bc && bc.Combatant == null)
                {
                    canRecycle = SpawnRecycleService.TryAddToRecycle(mob);
                }

                if (canRecycle)
                {
                    // Successfully added to recycle pool
                    _spawnedListRef.RemoveAt(i);
                    recycledCount++;

                    UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkYellow,
                        $"CLEANUP: Recycled {mob.Name} at {mob.Location.X},{mob.Location.Y}");
                }
                else
                {
                    // Recycle failed or not eligible - mark for deletion
                    if (!_deletionQueue.Contains(mob))
                    {
                        _deletionQueue.Add(mob);
                        _spawnedListRef.RemoveAt(i);
                        markedForDeletionCount++;

                        UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkYellow,
                            $"CLEANUP: Marked for deletion {mob.Name} at {mob.Location.X},{mob.Location.Y}");
                    }
                }
            }

            // Record metrics
            if (recycledCount > 0)
            {
                SpawnMetricsService.RecordRecyclePoolAddition(recycledCount);
            }

            if (markedForDeletionCount > 0)
            {
                SpawnMetricsService.RecordCleanupAddition(markedForDeletionCount);
            }

            // Log summary
            if (recycledCount > 0 || markedForDeletionCount > 0 || invalidRemoved > 0)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow,
                    $"CLEANUP CYCLE: Recycled={recycledCount}, MarkedDelete={markedForDeletionCount}, InvalidRemoved={invalidRemoved}");
            }

            // Notify core that cleanup completed (for cache rebuilds, etc.)
            _onCleanupCompleteCallback?.Invoke();
        }

        /// <summary>
        /// Delete all mobs in the deletion queue
        /// </summary>
        private static void BatchDelete()
        {
            if (_deletionQueue == null || _deletionQueue.Count == 0)
                return;

            int deletedCount = 0;

            foreach (Mobile mob in _deletionQueue)
            {
                if (mob != null && !mob.Deleted)
                {
                    mob.Delete();
                    deletedCount++;
                }
            }

            _deletionQueue.Clear();

            if (deletedCount > 0)
            {
                SpawnMetricsService.RecordDeletion(deletedCount);
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, 
                    $"CLEANUP: Deleted {deletedCount} mob(s) from deletion queue");
            }
        }

        /// <summary>
        /// Force immediate cleanup (for shutdown/crash)
        /// </summary>
        internal static void ForceCleanup()
        {
            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "CLEANUP SERVICE: Force cleanup initiated");

            Stop();

            // Process any remaining too-far spawns
            ProcessTooFarSpawns();

            // Delete everything in deletion queue
            BatchDelete();

            // Clear recycle pool
            SpawnRecycleService.ClearAll();
        }

        /// <summary>
        /// Get status string for display
        /// </summary>
        internal static string GetStatusString()
        {
            return $"Cleanup Service: {(_isRunning ? "Running" : "Stopped")}, " +
                   $"Deletion Queue: {_deletionQueue.Count}, " +
                   $"Timer Interval: {UORespawnSettings.CLEANUP_INTERVAL}s";
        }

        /// <summary>
        /// Get deletion queue count
        /// </summary>
        internal static int GetDeletionQueueCount()
        {
            return _deletionQueue?.Count ?? 0;
        }

        /// <summary>
        /// Check if service is running
        /// </summary>
        internal static bool IsRunning()
        {
            return _isRunning;
        }
    }
}
