using System;

using Server.Custom.UORespawnServer.Services;

namespace Server.Custom.UORespawnServer.Timers
{
    internal class ValidateTimer : Timer
    {
        private readonly ValidateService _Service;

        public ValidateTimer(ValidateService service, TimeSpan ts) : base(ts, ts)
        {
            _Service = service;

            UOR_Utility.SendMsg(ConsoleColor.Green, $"VALIDATE-[{ts.TotalSeconds} Created]");
        }

        protected override void OnTick()
        {
            if (UOR_Core.IsPaused) return;

            if (_Service == null)
            {
                UOR_Utility.SendMsg(ConsoleColor.Red, $"VALIDATE-[Null]");

                Stop();
                return;
            }

            _Service.Validate();
        }
    }
}
