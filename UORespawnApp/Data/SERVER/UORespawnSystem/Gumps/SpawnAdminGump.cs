using System;
using Server.Gumps;
using Server.Network;
using Server.Custom.UORespawnSystem.Services;
using Server.Custom.UORespawnSystem.SpawnUtility;

namespace Server.Custom.UORespawnSystem.Gumps
{
    /// <summary>
    /// Main admin control panel for UORespawn system
    /// Fixed: Centering issues resolved by separating HTML elements and using DIV alignment.
    /// </summary>
    public class SpawnAdminGump : Gump
    {
        private const int GUMP_WIDTH = 650;
        private const int GUMP_HEIGHT = 540;

        // Layout Constants
        private const int MARGIN = 20;
        private const int SECTION_SPACING = 10;

        // Colors
        private const string COLOR_TITLE = "#FFD700"; // Gold
        private const string COLOR_SUBTITLE = "#CCCCCC"; // Light Grey
        private const string COLOR_HEADER = "#00FFFF"; // Cyan
        private const string COLOR_LABEL = "#FFFFFF"; // White
        private const string COLOR_VALUE = "#FFA500"; // Orange
        private const string COLOR_WARNING = "#FF4444"; // Red

        private const int BG_SUB = 30546; // Dark Stone background

        // Button IDs
        private enum Buttons
        {
            Close = 0,
            Pause = 10,
            Resume = 11,
            Reload = 12,
            Status = 20,
            Metrics = 21,
            MetricsPlayers = 22,
            MetricsReset = 40,
            DebugToggle = 41,
            ClearSpawns = 42,
            ClearRecycle = 43,
            Refresh = 50
        }

        public SpawnAdminGump() : base(50, 50)
        {
            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            // --- Main Background ---
            AddBackground(0, 0, GUMP_WIDTH, GUMP_HEIGHT, 9200); // Main Stone Border
            AddImageTiled(10, 10, GUMP_WIDTH - 20, GUMP_HEIGHT - 20, 2624); // Stone texture
            AddAlphaRegion(10, 10, GUMP_WIDTH - 20, GUMP_HEIGHT - 20); // Darken

            // --- Header Section ---
            AddBackground(MARGIN, MARGIN, GUMP_WIDTH - (MARGIN * 2), 70, BG_SUB);

            // FIX: Split Title and Subtitle into two separate HTML entries.
            // This prevents the <BR> and Font Size change from breaking the centering calculation.

            // 1. Main Title (Larger, Gold)
            string titleText = $"<DIV ALIGN=\"CENTER\"><BASEFONT COLOR={COLOR_TITLE}>UORespawn Admin Control Panel</BASEFONT></DIV>";
            AddHtml(MARGIN, MARGIN + 10, GUMP_WIDTH - (MARGIN * 2), 35, titleText, false, false);

            // 2. Subtitle (Smaller, Grey)
            string subTitleText = $"<DIV ALIGN=\"CENTER\"><BASEFONT COLOR={COLOR_SUBTITLE}>Professional Spawn Management System</BASEFONT></DIV>";
            AddHtml(MARGIN, MARGIN + 45, GUMP_WIDTH - (MARGIN * 2), 20, subTitleText, false, false);

            int currentY = MARGIN + 80;

            // --- System Status Section ---
            currentY = AddSystemStatus(currentY);

            // --- Two-column layout for Control and Monitoring ---
            int leftColumnY = AddSystemControl(currentY);
            int rightColumnY = AddMonitoring(currentY);
            currentY = Math.Max(leftColumnY, rightColumnY) + SECTION_SPACING;

            // --- Advanced Options Section ---
            currentY = AddAdvanced(currentY);

            // --- Footer ---
            AddFooter();
        }

        private int AddSystemStatus(int startY)
        {
            int sectionHeight = 90;
            int width = GUMP_WIDTH - (MARGIN * 2);

            AddBackground(MARGIN, startY, width, sectionHeight, BG_SUB);

            // FIX: Used DIV ALIGN="CENTER" for section headers
            string headerText = $"<DIV ALIGN=\"CENTER\"><BASEFONT COLOR={COLOR_HEADER}>═══ System Status ═══</BASEFONT></DIV>";
            AddHtml(MARGIN, startY + 8, width, 20, headerText, false, false);

            // Get Data
            bool isPaused = UORespawnCore.IsPaused();
            int activeSpawns = UORespawnCore.GetActiveSpawnCount();
            int recyclePool = UORespawnCore.GetRecyclePoolCount();
            int activePlayers = UORespawnCore.GetActivePlayerCount();
            double recycleRate = SpawnMetricsService.GetRecycleRate();
            double cleanupTime = SpawnMetricsService.GetLastCleanupTime();

            string statusColor = isPaused ? "#FF4444" : "#44FF44";
            string statusText = isPaused ? "PAUSED" : "RUNNING";

            // Grid Layout for Stats
            int col1 = MARGIN + 40;
            int col2 = MARGIN + 240;
            int col3 = MARGIN + 440;
            int row1 = startY + 35;
            int row2 = startY + 60;

            // Row 1
            AddImage(col1 - 25, row1 + 3, isPaused ? 2360 : 2361);
            AddHtml(col1, row1, 180, 20, $"<BASEFONT COLOR={statusColor}><B>{statusText}</B></BASEFONT>", false, false);

            AddHtml(col2, row1, 180, 20, $"<BASEFONT COLOR={COLOR_LABEL}>Active Spawns: <B><BASEFONT COLOR={COLOR_VALUE}>{activeSpawns}</BASEFONT></B></BASEFONT>", false, false);
            AddHtml(col3, row1, 180, 20, $"<BASEFONT COLOR={COLOR_LABEL}>Active Players: <B><BASEFONT COLOR={COLOR_VALUE}>{activePlayers}</BASEFONT></B></BASEFONT>", false, false);

            // Row 2
            AddHtml(col1, row2, 180, 20, $"<BASEFONT COLOR={COLOR_LABEL}>Recycle Pool: <B><BASEFONT COLOR={COLOR_VALUE}>{recyclePool}</BASEFONT></B></BASEFONT>", false, false);
            AddHtml(col2, row2, 180, 20, $"<BASEFONT COLOR={COLOR_LABEL}>Recycle Rate: <B><BASEFONT COLOR={COLOR_VALUE}>{recycleRate:F1}%</BASEFONT></B></BASEFONT>", false, false);
            AddHtml(col3, row2, 180, 20, $"<BASEFONT COLOR={COLOR_LABEL}>Cleanup Time: <B><BASEFONT COLOR={COLOR_VALUE}>{cleanupTime:F2}ms</BASEFONT></B></BASEFONT>", false, false);

            return startY + sectionHeight + SECTION_SPACING;
        }

        private int AddSystemControl(int startY)
        {
            int sectionWidth = (GUMP_WIDTH - (MARGIN * 2) - SECTION_SPACING) / 2;
            int sectionHeight = 140;

            AddBackground(MARGIN, startY, sectionWidth, sectionHeight, BG_SUB);

            // FIX: Section Header Centering
            string headerText = $"<DIV ALIGN=\"CENTER\"><BASEFONT COLOR={COLOR_HEADER}>═══ System Control ═══</BASEFONT></DIV>";
            AddHtml(MARGIN, startY + 8, sectionWidth, 20, headerText, false, false);

            bool isPaused = UORespawnCore.IsPaused();
            int buttonX = MARGIN + 30;
            int labelX = buttonX + 35;
            int currentY = startY + 40;
            int spacing = 30;

            // Pause/Resume
            if (isPaused)
            {
                AddButton(buttonX, currentY, 4024, 4025, (int)Buttons.Resume, GumpButtonType.Reply, 0);
                AddLabel(labelX, currentY + 2, 0x44, "Resume System");
            }
            else
            {
                AddButton(buttonX, currentY, 4021, 4022, (int)Buttons.Pause, GumpButtonType.Reply, 0);
                AddLabel(labelX, currentY + 2, 0x22, "Pause System");
            }

            currentY += spacing;

            // Reload
            AddButton(buttonX, currentY, 4014, 4015, (int)Buttons.Reload, GumpButtonType.Reply, 0);
            AddLabel(labelX, currentY + 2, 1153, "Reload Spawn Data");

            // Access Text - Fixed Centering
            string accessText = $"<DIV ALIGN=\"CENTER\"><BASEFONT COLOR=#999999>Administrator Access Required</BASEFONT></DIV>";
            AddHtml(MARGIN, startY + 110, sectionWidth, 20, accessText, false, false);

            return startY + sectionHeight;
        }

        private int AddMonitoring(int startY)
        {
            int sectionWidth = (GUMP_WIDTH - (MARGIN * 2) - SECTION_SPACING) / 2;
            int sectionHeight = 140;
            int leftColumnX = MARGIN + sectionWidth + SECTION_SPACING;

            AddBackground(leftColumnX, startY, sectionWidth, sectionHeight, BG_SUB);

            // FIX: Section Header Centering
            string headerText = $"<DIV ALIGN=\"CENTER\"><BASEFONT COLOR={COLOR_HEADER}>═══ Monitoring ═══</BASEFONT></DIV>";
            AddHtml(leftColumnX, startY + 8, sectionWidth, 20, headerText, false, false);

            int buttonX = leftColumnX + 30;
            int labelX = buttonX + 35;
            int currentY = startY + 40;
            int spacing = 30;

            AddButton(buttonX, currentY, 4005, 4006, (int)Buttons.Status, GumpButtonType.Reply, 0);
            AddLabel(labelX, currentY + 2, 1153, "Quick Status Report");
            currentY += spacing;

            AddButton(buttonX, currentY, 4005, 4006, (int)Buttons.Metrics, GumpButtonType.Reply, 0);
            AddLabel(labelX, currentY + 2, 1153, "Full Metrics Report");
            currentY += spacing;

            AddButton(buttonX, currentY, 4005, 4006, (int)Buttons.MetricsPlayers, GumpButtonType.Reply, 0);
            AddLabel(labelX, currentY + 2, 1153, "Player Metrics");

            return startY + sectionHeight;
        }

        private int AddAdvanced(int startY)
        {
            int sectionHeight = 110;
            int width = GUMP_WIDTH - (MARGIN * 2);

            AddBackground(MARGIN, startY, width, sectionHeight, BG_SUB);

            // FIX: Section Header Centering
            string headerText = $"<DIV ALIGN=\"CENTER\"><BASEFONT COLOR={COLOR_HEADER}>═══ Advanced Options ═══</BASEFONT></DIV>";
            AddHtml(MARGIN, startY + 8, width, 20, headerText, false, false);

            int slotWidth = width / 4;
            int startX = MARGIN + (slotWidth / 2) - 10;
            int yPos = startY + 40;

            // 1. Reset Metrics
            int b1 = MARGIN + 20;
            AddButton(b1, yPos, 4017, 4018, (int)Buttons.MetricsReset, GumpButtonType.Reply, 0);
            AddLabel(b1 + 35, yPos + 2, 0x480, "Reset Metrics");

            // 2. Debug Toggle
            string debugState = UORespawnSettings.ENABLE_DEBUG ? "ON" : "OFF";
            int debugHue = UORespawnSettings.ENABLE_DEBUG ? 0x44 : 0x22;
            int b2 = MARGIN + 170;
            AddButton(b2, yPos, 4008, 4009, (int)Buttons.DebugToggle, GumpButtonType.Reply, 0);
            AddLabel(b2 + 35, yPos + 2, debugHue, $"Debug: {debugState}");

            // 3. Clear Spawns
            int b3 = MARGIN + 320;
            AddButton(b3, yPos, 4020, 4021, (int)Buttons.ClearSpawns, GumpButtonType.Reply, 0);
            AddLabel(b3 + 35, yPos + 2, 38, "Clear Spawns");

            // 4. Clear Pool
            int b4 = MARGIN + 470;
            AddButton(b4, yPos, 4032, 4033, (int)Buttons.ClearRecycle, GumpButtonType.Reply, 0);
            AddLabel(b4 + 35, yPos + 2, 0x480, "Clear Pool");

            // Warning - Fixed Centering
            string warnText = $"<DIV ALIGN=\"CENTER\"><BASEFONT COLOR={COLOR_WARNING}>⚠ Administrator Only - Use with Caution ⚠</BASEFONT></DIV>";
            AddHtml(MARGIN, startY + 75, width, 20, warnText, false, false);

            return startY + sectionHeight + SECTION_SPACING;
        }

        private void AddFooter()
        {
            int footerY = GUMP_HEIGHT - 45;

            // Close Button
            AddButton(MARGIN + 10, footerY, 4020, 4021, (int)Buttons.Close, GumpButtonType.Reply, 0);
            AddLabel(MARGIN + 50, footerY + 2, 0x22, "Close");

            // Version Text - Fixed Centering
            string versionText = "<DIV ALIGN=\"CENTER\"><BASEFONT COLOR=#666666>UORespawn v2.0</BASEFONT></DIV>";
            AddHtml(0, footerY + 5, GUMP_WIDTH, 20, versionText, false, false);

            // Refresh Button
            AddButton(GUMP_WIDTH - 60, footerY, 4014, 4015, (int)Buttons.Refresh, GumpButtonType.Reply, 0);
            AddLabel(GUMP_WIDTH - 120, footerY + 2, 0x35, "Refresh");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (from == null || from.Deleted) return;

            switch ((Buttons)info.ButtonID)
            {
                case Buttons.Close: break;

                case Buttons.Refresh:
                    from.SendGump(new SpawnAdminGump());
                    from.SendMessage(0x35, "Panel refreshed.");
                    break;

                case Buttons.Pause:
                    if (CheckAdmin(from)) { UORespawnCore.Pause(); Refresh(from); }
                    break;
                case Buttons.Resume:
                    if (CheckAdmin(from)) { UORespawnCore.Resume(); Refresh(from); }
                    break;
                case Buttons.Reload:
                    if (CheckAdmin(from)) { UORespawnDataBase.ReLoadSpawns(); from.SendMessage("Reloaded."); Refresh(from); }
                    break;

                case Buttons.Status:
                    if (CheckGM(from))
                    {
                        from.SendMessage(0x35, $"Active Spawns: {UORespawnCore.GetActiveSpawnCount()}");
                        Refresh(from);
                    }
                    break;

                case Buttons.Metrics:
                case Buttons.MetricsPlayers:
                    if (CheckAdmin(from))
                    {
                        bool players = (Buttons)info.ButtonID == Buttons.MetricsPlayers;
                        string rpt = SpawnMetricsService.GetReport(players);
                        Console.WriteLine(rpt);
                        from.SendMessage("Report sent to console.");
                        Refresh(from);
                    }
                    break;

                case Buttons.MetricsReset:
                    if (CheckAdmin(from)) { SpawnMetricsService.Reset(); from.SendMessage("Metrics Reset."); Refresh(from); }
                    break;

                case Buttons.DebugToggle:
                    if (CheckAdmin(from))
                    {
                        UORespawnSettings.ENABLE_DEBUG = !UORespawnSettings.ENABLE_DEBUG;
                        Refresh(from);
                    }
                    break;

                case Buttons.ClearSpawns:
                    if (CheckAdmin(from)) from.SendGump(new SpawnClearConfirmGump());
                    break;

                case Buttons.ClearRecycle:
                    if (CheckAdmin(from)) { SpawnRecycleService.ClearAll(); Refresh(from); }
                    break;
            }
        }

        private void Refresh(Mobile m) => m.SendGump(new SpawnAdminGump());
        private bool CheckAdmin(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator) return true;
            m.SendMessage(0x22, "Access Denied: Admin required.");
            return false;
        }
        private bool CheckGM(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster) return true;
            m.SendMessage(0x22, "Access Denied: GM required.");
            return false;
        }
    }
}
