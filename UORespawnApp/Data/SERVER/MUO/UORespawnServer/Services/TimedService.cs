using System;

using Server.Custom.UORespawnServer.Timers;

namespace Server.Custom.UORespawnServer.Services;
/// <summary>
/// Handles periodic time-based spawn updates.
/// Triggers day/night spawn transitions and timed spawn events.
/// </summary>
internal class TimedService
{
    private readonly TimedTimer _TimedTimer;
    private int _UpdateCount;

    /// <summary>
    /// Creates the timed service with configured interval.
    /// </summary>
    internal TimedService()
    {
        _TimedTimer = new TimedTimer(this, TimeSpan.FromMinutes(UOR_Settings.TIMED_INTERVAL));

        _UpdateCount = 0;

        UOR_Utility.SendMsg(ConsoleColor.Yellow, $"TIMED-[{UOR_Settings.TIMED_INTERVAL} Created]");
    }

    internal void Start()
    {
        if (!_TimedTimer.Running)
        {
            _TimedTimer?.Start();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"TIMED-[Started]");
        }
    }

    internal void Stop()
    {
        if (_TimedTimer.Running)
        {
            _TimedTimer?.Stop();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"TIMED-[Stopped]");
        }
    }

    internal void Update()
    {
        _UpdateCount++;

        UOR_Utility.SendMsg(ConsoleColor.Yellow, $"TIMED-[{_UpdateCount} Updated]");

        UOR_Core.UpdateTimed();
    }
}
