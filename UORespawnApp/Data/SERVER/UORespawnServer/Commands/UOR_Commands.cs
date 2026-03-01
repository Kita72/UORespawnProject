using System;

using Server.Commands;
using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Commands
{
    internal static class UOR_Commands
    {
        public static void Initialize()
        {
            CommandSystem.Register("UORespawn", AccessLevel.Administrator, new CommandEventHandler(UORespawn_OnCommand));
            CommandSystem.Register("UOR", AccessLevel.Administrator, new CommandEventHandler(UORespawn_OnCommand));
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
    }
}
