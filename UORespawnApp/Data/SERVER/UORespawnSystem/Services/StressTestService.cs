using System;
using System.Collections.Generic;

using Server.Mobiles;

namespace Server.Custom.UORespawnSystem.Services
{
    /// <summary>
    /// Standalone stress test service for performance testing.
    /// Creates virtual players that move around the map to trigger spawn system activity.
    /// NO direct knowledge of UORespawn - just creates/manages test players.
    /// </summary>
    public static class StressTestService
    {
        // Test state
        private static List<PlayerMobile> _testPlayers = null;
        private static Timer _movementTimer = null;
        private static DateTime _testStartTime = DateTime.MinValue;

        // Configuration
        private const int DEFAULT_PLAYER_COUNT = 10;
        private const int MIN_PLAYER_COUNT = 1;
        private const int MAX_PLAYER_COUNT = 50;
        private const int MOVEMENT_INTERVAL_SECONDS = 15; // Move every 15 seconds

        /// <summary>
        /// Check if test is currently running
        /// </summary>
        public static bool IsRunning => _testPlayers != null && _testPlayers.Count > 0;

        /// <summary>
        /// Get count of active test players
        /// </summary>
        public static int ActivePlayerCount => _testPlayers?.Count ?? 0;

        /// <summary>
        /// Get test start time
        /// </summary>
        public static DateTime StartTime => _testStartTime;

        /// <summary>
        /// Start stress test with specified number of virtual players
        /// </summary>
        public static string Start(int playerCount = DEFAULT_PLAYER_COUNT)
        {
            // Validate not already running
            if (IsRunning)
            {
                return "ERROR: Stress test is already running! Use [StopStressTest to stop it first.";
            }

            // Validate player count
            if (playerCount < MIN_PLAYER_COUNT || playerCount > MAX_PLAYER_COUNT)
            {
                return $"ERROR: Player count must be between {MIN_PLAYER_COUNT} and {MAX_PLAYER_COUNT}.";
            }

            // Create test players
            _testPlayers = CreateTestPlayers(playerCount);

            if (_testPlayers.Count == 0)
            {
                return "ERROR: Failed to create test players!";
            }

            // Record start time
            _testStartTime = DateTime.UtcNow;

            // Start movement timer
            _movementTimer = Timer.DelayCall(
                TimeSpan.FromSeconds(MOVEMENT_INTERVAL_SECONDS), 
                TimeSpan.FromSeconds(MOVEMENT_INTERVAL_SECONDS), 
                MoveTestPlayers
            );

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[Stress Test]: Started with {_testPlayers.Count} virtual players");
            Console.WriteLine($"[Stress Test]: Players will move every {MOVEMENT_INTERVAL_SECONDS} seconds");
            Console.WriteLine($"[Stress Test]: Use [StopStressTest to end test and cleanup");
            Console.ResetColor();

            return $"Stress test started with {_testPlayers.Count} virtual players. They will move every {MOVEMENT_INTERVAL_SECONDS} seconds.";
        }

        /// <summary>
        /// Stop stress test and cleanup all test players
        /// </summary>
        public static string Stop()
        {
            if (!IsRunning)
            {
                return "No stress test is currently running.";
            }

            TimeSpan duration = DateTime.UtcNow - _testStartTime;
            int playerCount = _testPlayers.Count;

            // Stop movement timer
            _movementTimer?.Stop();
            _movementTimer = null;

            // Cleanup test players
            CleanupTestPlayers();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Stress Test]: Stopped after {duration.TotalSeconds:F1} seconds");
            Console.WriteLine($"[Stress Test]: Cleaned up {playerCount} test players");
            Console.ResetColor();

            return $"Stress test stopped. Ran for {duration.TotalSeconds:F1} seconds with {playerCount} players.";
        }

        /// <summary>
        /// Get current test status
        /// </summary>
        public static string GetStatus()
        {
            if (!IsRunning)
            {
                return "No stress test is currently running.";
            }

            TimeSpan elapsed = DateTime.UtcNow - _testStartTime;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║              Stress Test Status                              ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Status:         RUNNING");
            sb.AppendLine($"Test Players:   {_testPlayers.Count}");
            sb.AppendLine($"Elapsed Time:   {elapsed.TotalSeconds:F1} seconds");
            sb.AppendLine($"Movement:       Every {MOVEMENT_INTERVAL_SECONDS} seconds");
            sb.AppendLine();
            sb.AppendLine("Use [SpawnMetrics to view spawn system performance.");
            sb.AppendLine("Use [StopStressTest to end test.");

            return sb.ToString();
        }

        /// <summary>
        /// Create virtual test players at various map locations
        /// </summary>
        private static List<PlayerMobile> CreateTestPlayers(int count)
        {
            List<PlayerMobile> players = new List<PlayerMobile>();
            Map testMap = Map.Felucca;

            // Spread players across different locations
            Point3D[] testLocations = new Point3D[]
            {
                new Point3D(1400, 1600, 0),  // Britain area
                new Point3D(2500, 500, 0),   // Minoc area
                new Point3D(3700, 2200, 0),  // Magincia area
                new Point3D(2900, 3400, 0),  // Trinsic area
                new Point3D(600, 2100, 0),   // Yew area
                new Point3D(1800, 2800, 0),  // Central area
                new Point3D(4400, 1100, 0),  // Eastern area
                new Point3D(700, 800, 0),    // Northern area
                new Point3D(5200, 3900, 0),  // Southeastern area
                new Point3D(300, 3800, 0)    // Southwestern area
            };

            for (int i = 0; i < count; i++)
            {
                try
                {
                    PlayerMobile player = new PlayerMobile
                    {
                        Name = $"StressTest_{i + 1}",
                        Map = testMap,
                        Location = testLocations[i % testLocations.Length],
                        AccessLevel = AccessLevel.GameMaster // Staff access for debug spawns
                    };

                    player.MoveToWorld(player.Location, player.Map);
                    players.Add(player);
                    UORespawnCore.AddPlayer(player); // Register with spawn system for tracking

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[Stress Test]: Created test player {player.Name} at {player.Location}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Stress Test]: Failed to create player {i + 1}: {ex.Message}");
                    Console.ResetColor();
                }
            }

            return players;
        }

        /// <summary>
        /// Move test players to random nearby locations (triggers spawn system activity)
        /// </summary>
        private static void MoveTestPlayers()
        {
            if (_testPlayers == null || _testPlayers.Count == 0)
                return;

            int movedCount = 0;

            foreach (PlayerMobile player in _testPlayers)
            {
                if (player == null || player.Deleted)
                    continue;

                try
                {
                    // Move player 50-100 tiles in random direction
                    int offsetX = Utility.RandomMinMax(-100, 100);
                    int offsetY = Utility.RandomMinMax(-100, 100);

                    Point3D newLocation = new Point3D(
                        player.Location.X + offsetX,
                        player.Location.Y + offsetY,
                        player.Location.Z
                    );

                    // Ensure location is valid
                    if (player.Map.CanFit(newLocation.X, newLocation.Y, newLocation.Z, 16))
                    {
                        player.Location = newLocation;
                        movedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Stress Test]: Failed to move player {player.Name}: {ex.Message}");
                    Console.ResetColor();
                }
            }

            if (movedCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[Stress Test]: Moved {movedCount} test players (triggers spawn activity)");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Cleanup all test players
        /// </summary>
        private static void CleanupTestPlayers()
        {
            if (_testPlayers == null)
                return;

            int deletedCount = 0;

            foreach (PlayerMobile player in _testPlayers)
            {
                // Only delete test players (no account = virtual player)
                if (player != null && !player.Deleted && player.Account == null)
                {
                    UORespawnCore.RemovePlayer(player); // Unregister from spawn system

                    try
                    {
                        player.Delete();
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Stress Test]: Failed to delete player {player.Name}: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }

            _testPlayers.Clear();
            _testPlayers = null;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[Stress Test]: Deleted {deletedCount} test players");
            Console.ResetColor();
        }

        /// <summary>
        /// Force cleanup on server shutdown
        /// </summary>
        public static void Shutdown()
        {
            if (IsRunning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Stress Test]: Server shutting down - cleaning up test players");
                Console.ResetColor();

                Stop();
            }
        }
    }
}
