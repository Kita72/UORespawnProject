using System;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Custom.UORespawnSystem.SpawnUtility;

namespace Server.Custom.UORespawnSystem.Services
{
    /// <summary>
    /// Monitors spawn distances from players and marks spawns that are too far.
    /// Runs on its own independent timer (DISTANCE_INTERVAL = 1 second).
    /// Single Responsibility: Check distances and update isTooFar flags.
    /// </summary>
    internal static class SpawnDistanceService
    {
        private static Timer _distanceTimer;
        private static bool _isRunning = false;

        // References to core data (set during initialization)
        private static List<(Mobile mob, bool isTooFar)> _spawnedListRef;
        private static List<PlayerMobile> _playersRef;

        /// <summary>
        /// Initialize the distance monitor with references to spawn data
        /// </summary>
        internal static void Initialize(
            List<(Mobile mob, bool isTooFar)> spawnedListRef,
            List<PlayerMobile> playersRef)
        {
            _spawnedListRef = spawnedListRef;
            _playersRef = playersRef;

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Green, "Distance Monitor - Initialized");
        }

        /// <summary>
        /// Start the distance monitoring timer
        /// </summary>
        internal static void Start()
        {
            if (_isRunning)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "DISTANCE MONITOR: Already running");
                return;
            }

            _distanceTimer = Timer.DelayCall(
                TimeSpan.FromMilliseconds(UORespawnSettings.DISTANCE_INTERVAL),
                TimeSpan.FromMilliseconds(UORespawnSettings.DISTANCE_INTERVAL),
                OnTimerTick);

            _isRunning = true;

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, 
                $"DISTANCE MONITOR: Started (Interval: {UORespawnSettings.DISTANCE_INTERVAL}ms)");
        }

        /// <summary>
        /// Stop the distance monitoring timer
        /// </summary>
        internal static void Stop()
        {
            if (_distanceTimer != null && _distanceTimer.Running)
            {
                _distanceTimer.Stop();
                _distanceTimer = null;
            }

            _isRunning = false;

            UORespawnUtility.SendConsoleMsg(ConsoleColor.Yellow, "DISTANCE MONITOR: Stopped");
        }

        /// <summary>
        /// Check if monitor is running
        /// </summary>
        internal static bool IsRunning()
        {
            return _isRunning;
        }

        /// <summary>
        /// Timer tick - check distances automatically
        /// </summary>
        private static void OnTimerTick()
        {
            try
            {
                CheckDistances();
            }
            catch (Exception ex)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.Red, 
                    $"ERROR: Distance monitor cycle failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Check distances for all spawns and update their isTooFar flag.
        /// Now called automatically by timer.
        /// </summary>
        private static void CheckDistances()
        {
            if (_spawnedListRef == null || _spawnedListRef.Count == 0)
                return;

            if (_playersRef == null || _playersRef.Count == 0)
            {
                // No players online - mark all spawns as too far
                for (int i = 0; i < _spawnedListRef.Count; i++)
                {
                    var (mob, _) = _spawnedListRef[i];
                    _spawnedListRef[i] = (mob, true);
                }
                return;
            }

            int markedTooFar = 0;
            int markedNearby = 0;

            // Check each spawn against all players
            for (int i = 0; i < _spawnedListRef.Count; i++)
            {
                var (mob, _) = _spawnedListRef[i];

                if (mob == null || mob.Deleted)
                {
                    _spawnedListRef[i] = (mob, true); // Mark for cleanup
                    markedTooFar++;
                    continue;
                }

                bool isTooFar = IsTooFarFromAllPlayers(mob, _playersRef);

                _spawnedListRef[i] = (mob, isTooFar);

                if (isTooFar)
                    markedTooFar++;
                else
                    markedNearby++;
            }

            // Only log if there are spawns marked too far
            if (markedTooFar > 0 && UORespawnSettings.ENABLE_DEBUG)
            {
                UORespawnUtility.SendConsoleMsg(ConsoleColor.DarkYellow, 
                    $"DISTANCE CHECK: Nearby={markedNearby}, TooFar={markedTooFar}, Total={_spawnedListRef.Count}");
            }
        }

        /// <summary>
        /// Check if a mobile is too far from ALL players
        /// </summary>
        private static bool IsTooFarFromAllPlayers(Mobile mob, List<PlayerMobile> players)
        {
            if (mob == null || mob.Deleted || mob.Map == null || mob.Map == Map.Internal)
                return true;

            int maxDistance = (int)(UORespawnSettings.MAX_RANGE * 1.5); // 1.5x spawn range

            foreach (PlayerMobile pm in players)
            {
                if (!UORespawnUtility.IsValidPlayer(pm))
                    continue;

                // Different maps = too far
                if (pm.Map != mob.Map)
                    continue;

                // If close to ANY player, not too far
                if (pm.InRange(mob.Location, maxDistance))
                {
                    return false;
                }
            }

            return true; // Too far from all players
        }

        /// <summary>
        /// Get count of spawns marked as too far
        /// </summary>
        internal static int GetTooFarCount(List<(Mobile mob, bool isTooFar)> spawnedList)
        {
            if (spawnedList == null || spawnedList.Count == 0)
                return 0;

            int count = 0;

            foreach (var (_, isTooFar) in spawnedList)
            {
                if (isTooFar)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Get count of spawns marked as nearby (not too far)
        /// </summary>
        internal static int GetNearbyCount(List<(Mobile mob, bool isTooFar)> spawnedList)
        {
            if (spawnedList == null || spawnedList.Count == 0)
                return 0;

            int count = 0;

            foreach (var (_, isTooFar) in spawnedList)
            {
                if (!isTooFar)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Get status string for display
        /// </summary>
        internal static string GetStatusString()
        {
            return $"Distance Monitor: {(_isRunning ? "Running" : "Stopped")}, " +
                   $"Interval: {UORespawnSettings.DISTANCE_INTERVAL}ms, " +
                   $"Check Range: {(int)(UORespawnSettings.MAX_RANGE * 1.5)}";
        }
    }
}

