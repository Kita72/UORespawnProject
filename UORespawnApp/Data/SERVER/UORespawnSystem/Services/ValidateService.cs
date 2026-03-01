using System;
using System.Collections.Generic;

using Server.Custom.UORespawnServer.Timers;

namespace Server.Custom.UORespawnServer.Services
{
    internal class ValidateService
    {
        private readonly ValidateTimer _ValidateTimer;

        public ValidateService()
        {
            _ValidateTimer = new ValidateTimer(this, TimeSpan.FromSeconds(UOR_Settings.VALIDATE_INTERVAL));

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[{UOR_Settings.VALIDATE_INTERVAL} Created]");
        }

        internal void Start()
        {
            if (!_ValidateTimer.Running)
            {
                _ValidateTimer?.Start();

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[Started]");
            }
        }

        internal void Stop()
        {
            if (_ValidateTimer.Running)
            {
                _ValidateTimer?.Stop();

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[Stopped]");
            }
        }

        internal void Validate()
        {
            List<Serial> allSpawn = UOR_Utility.GetAllSpawn();

            int recycledThisPass = 0;

            for (int i = 0; i < allSpawn.Count; i++)
            {
                if (allSpawn[i] is Serial serial && UOR_Utility.GetMobile(serial) is Mobile m)
                {
                    if (!UOR_Utility.PlayersInRange(m.Map, m.Location, UOR_Settings.MAX_RANGE + (UOR_Settings.MAX_RANGE / 2)))
                    {
                        UOR_Core.SendToRecycled(serial);

                        recycledThisPass++;
                    }
                }
            }

            if (recycledThisPass > 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VALIDATE-[{recycledThisPass} Recycled]");
            }
        }
    }
}
