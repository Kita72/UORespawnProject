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
            CommandSystem.Register("UORAdd", AccessLevel.Counselor, new CommandEventHandler(UORespawnAdd_OnCommand));
            CommandSystem.Register("UORDrop", AccessLevel.Counselor, new CommandEventHandler(UORespawnDrop_OnCommand));
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

        [Usage("UORAdd")]
        [Description("Adds Staff to Respawn")]
        private static void UORespawnDrop_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                if (UOR_Core.AddStaff(pm))
                {
                    pm.SendMessage(63, "Added to UORespawn!");
                }
            }
        }

        [Usage("UORDrop")]
        [Description("Drops Staff from Respawn")]
        private static void UORespawnAdd_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                if (UOR_Core.DropStaff(pm))
                {
                    pm.SendMessage(33, "Dropped from UORespawn!");
                }
            }
        }

        [Usage("ShowRespawn")]
        [Description("Show's Respawn Spawn")]
        private static void ShowUORespawn_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                RunCallOut(pm);

                if (UOR_Core.ToggleValidateCallOut())
                {
                    pm.SendMessage(63, "Callout Respawn - [ON]");
                }
                else
                {
                    pm.SendMessage(33, "Callout Respawn - [OFF]");
                }
            }

            UOR_Utility.SendMsg(ConsoleColor.Yellow, "CONTROL-[Accessed]");
        }

        internal static void RunCallOut(PlayerMobile pm)
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
    }
}
