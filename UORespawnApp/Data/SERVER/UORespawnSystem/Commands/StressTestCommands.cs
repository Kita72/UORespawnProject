using Server.Commands;
using Server.Custom.UORespawnSystem.Services;

namespace Server.Custom.UORespawnSystem.Commands
{
    /// <summary>
    /// Simple stress test commands - creates virtual players that trigger spawn activity
    /// </summary>
    public static class StressTestCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("StartStressTest", AccessLevel.Administrator, new CommandEventHandler(StartStressTest_OnCommand));
            CommandSystem.Register("StopStressTest", AccessLevel.Administrator, new CommandEventHandler(StopStressTest_OnCommand));
            CommandSystem.Register("StressTestStatus", AccessLevel.GameMaster, new CommandEventHandler(StressTestStatus_OnCommand));

            // Subscribe to server shutdown to cleanup
            EventSink.Shutdown += EventSink_Shutdown;
        }

        [Usage("StartStressTest [playerCount]")]
        [Description("Starts stress test with virtual players (default: 10, range: 1-50). Players move every 15 seconds.")]
        private static void StartStressTest_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            int playerCount = 10; // Default

            // Parse player count if provided
            if (e.Arguments.Length > 0)
            {
                if (!int.TryParse(e.Arguments[0], out playerCount))
                {
                    from.SendMessage(0x22, "Invalid player count. Usage: [StartStressTest [1-50]");
                    return;
                }
            }

            // Start the test
            string result = StressTestService.Start(playerCount);

            if (result.StartsWith("ERROR"))
            {
                from.SendMessage(0x22, result);
            }
            else
            {
                from.SendMessage(0x35, "╔══════════════════════════════════════════════════════════════╗");
                from.SendMessage(0x35, "║              Stress Test Started                             ║");
                from.SendMessage(0x35, "╚══════════════════════════════════════════════════════════════╝");
                from.SendMessage(0x48, result);
                from.SendMessage(0x48, "");
                from.SendMessage(0x48, "Test players are virtual staff members (GameMaster access).");
                from.SendMessage(0x48, "They will move every 15 seconds, triggering spawn activity.");
                from.SendMessage(0x48, "");
                from.SendMessage(0x59, "Use [SpawnMetrics to monitor performance.");
                from.SendMessage(0x59, "Use [StopStressTest to end test and cleanup.");
            }
        }

        [Usage("StopStressTest")]
        [Description("Stops running stress test and cleans up all virtual players.")]
        private static void StopStressTest_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            string result = StressTestService.Stop();

            if (result.StartsWith("No stress test"))
            {
                from.SendMessage(0x48, result);
            }
            else
            {
                from.SendMessage(0x35, "╔══════════════════════════════════════════════════════════════╗");
                from.SendMessage(0x35, "║              Stress Test Stopped                             ║");
                from.SendMessage(0x35, "╚══════════════════════════════════════════════════════════════╝");
                from.SendMessage(0x48, result);
                from.SendMessage(0x48, "");
                from.SendMessage(0x59, "Use [SpawnMetrics to review final performance data.");
            }
        }

        [Usage("StressTestStatus")]
        [Description("Shows current stress test status.")]
        private static void StressTestStatus_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            string status = StressTestService.GetStatus();

            from.SendMessage(0x35, status);
        }

        private static void EventSink_Shutdown(ShutdownEventArgs e)
        {
            StressTestService.Shutdown();
        }
    }
}
