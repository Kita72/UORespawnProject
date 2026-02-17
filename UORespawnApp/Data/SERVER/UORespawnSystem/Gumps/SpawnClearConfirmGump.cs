using System;

using Server.Gumps;
using Server.Network;

namespace Server.Custom.UORespawnSystem.Gumps
{
    /// <summary>
    /// Confirmation gump for clearing all spawns (dangerous operation)
    /// </summary>
    public class SpawnClearConfirmGump : Gump
    {
        private const int GUMP_WIDTH = 450;
        private const int GUMP_HEIGHT = 250;

        private enum Buttons
        {
            Cancel = 0,
            Confirm = 1
        }

        public SpawnClearConfirmGump() : base(100, 100)
        {
            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            // Background
            AddBackground(0, 0, GUMP_WIDTH, GUMP_HEIGHT, 9200);
            AddImageTiled(10, 10, GUMP_WIDTH - 20, GUMP_HEIGHT - 20, 2624);
            AddAlphaRegion(10, 10, GUMP_WIDTH - 20, GUMP_HEIGHT - 20);

            // Header (warning color)
            AddBackground(20, 20, GUMP_WIDTH - 40, 50, 9350);
            AddHtml(30, 30, GUMP_WIDTH - 60, 30, "<CENTER><BASEFONT COLOR=#FF4444><B>⚠ WARNING ⚠</B></BASEFONT></CENTER>", false, false);

            // Warning message
            int y = 80;
            AddHtml(30, y, GUMP_WIDTH - 60, 80, 
                "<CENTER><BASEFONT COLOR=#FFFFFF><B>Clear All Spawns?</B><br><br>" +
                "This will DELETE all spawns from the world.<br>" +
                "This action CANNOT be undone!<br><br>" +
                "<BASEFONT COLOR=#FF4444>Are you sure you want to continue?</BASEFONT></CENTER>", 
                false, false);

            // Spawn count info
            int activeSpawns = UORespawnCore.GetActiveSpawnCount();
            AddHtml(30, y + 90, GUMP_WIDTH - 60, 20, 
                $"<CENTER><BASEFONT COLOR=#FFAA00>Currently active: <B>{activeSpawns}</B> spawns</BASEFONT></CENTER>", 
                false, false);

            // Buttons
            int buttonY = GUMP_HEIGHT - 60;

            // Cancel button (green, larger)
            AddButton(60, buttonY, 2151, 2152, (int)Buttons.Cancel, GumpButtonType.Reply, 0);
            AddLabel(95, buttonY + 2, 0x44, "Cancel (Safe)");

            // Confirm button (red, warning)
            AddButton(GUMP_WIDTH - 180, buttonY, 2151, 2152, (int)Buttons.Confirm, GumpButtonType.Reply, 0);
            AddLabel(GUMP_WIDTH - 145, buttonY + 2, 0x22, "Confirm Delete");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (from == null || from.Deleted || from.AccessLevel < AccessLevel.Administrator)
                return;

            switch ((Buttons)info.ButtonID)
            {
                case Buttons.Cancel:
                    from.SendMessage(0x44, "Operation cancelled. No spawns were deleted.");
                    from.SendGump(new SpawnAdminGump());
                    break;

                case Buttons.Confirm:
                    int deletedCount = UORespawnCore.ClearAllSpawns();
                    from.SendMessage(0x22, $"⚠ Cleared and deleted {deletedCount} tracked spawns!");
                    
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[UORespawn]: {from.Name} cleared all tracked spawns - {deletedCount} deleted");
                    Console.ResetColor();

                    from.SendGump(new SpawnAdminGump());
                    break;

                default:
                    from.SendGump(new SpawnClearConfirmGump());
                    break;
            }
        }
    }
}
