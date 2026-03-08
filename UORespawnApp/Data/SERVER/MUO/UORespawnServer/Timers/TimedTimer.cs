using System;

using Server.Custom.UORespawnServer.Services;

namespace Server.Custom.UORespawnServer.Timers;
/// <summary>
/// Global timer that triggers time-of-day updates for the spawn system.
/// Handles day/night transitions and timed spawn events.
/// </summary>
internal class TimedTimer : Timer
{
    private readonly TimedService _Service;

    public TimedTimer(TimedService service, TimeSpan ts) : base(ts, ts)
    {
        _Service = service;

        UOR_Utility.SendMsg(ConsoleColor.Green, $"TIMED-[{ts.TotalSeconds} Created]");
    }

    protected override void OnTick()
    {
        if (UOR_Core.IsPaused) return;

        if (_Service == null)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"TIMED-[Null]");

            Stop();
            return;
        }

        _Service.Update();
    }
}
