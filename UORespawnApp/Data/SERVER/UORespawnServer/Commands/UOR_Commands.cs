using System;
using System.Linq;
using Server.Commands;
using Server.Custom.UORespawnServer.Spawners;
using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Commands
{
    internal static class UOR_Commands
    {
        public static void Initialize()
        {
            CommandSystem.Register("UORespawn", AccessLevel.Administrator, new CommandEventHandler(UORespawn_OnCommand));
            CommandSystem.Register("UOR", AccessLevel.Administrator, new CommandEventHandler(UORespawn_OnCommand));
            CommandSystem.Register("ShowRespawn", AccessLevel.Administrator, new CommandEventHandler(ShowUORespawn_OnCommand));
        }

        [Usage("UORespawn")]
        [Aliases("UOR")]
        [Description("Opens UORespawn Control Panel")]
        private static void UORespawn_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                UOR_Core.OpenControlGump(pm);
            }

            UOR_Utility.SendMsg(ConsoleColor.Yellow, "CONTROL-[Accessed]");
        }

        [Usage("ShowRespawn")]
        [Description("Show's Respawn Spawn")]
        private static void ShowUORespawn_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                var mobs = pm.Map.GetMobilesInRange(pm.Location, 40)?.ToList();

                if (mobs.Count > 0)
                {
                    for (int i = 0; i < mobs.Count; i++)
                    {
                        if (mobs[i] is BaseCreature bc && bc.MySpawner is UOR_Spawner)
                        {
                            bc.Say("I AM RESPAWNED!");
                        }
                    }
                }
            }

            UOR_Utility.SendMsg(ConsoleColor.Yellow, "CONTROL-[Accessed]");
        }
    }
}
