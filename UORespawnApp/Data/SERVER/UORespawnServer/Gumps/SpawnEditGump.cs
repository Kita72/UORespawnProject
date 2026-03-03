using System.Collections.Generic;

using Server.Gumps;
using Server.Mobiles;
using Server.Network;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Services;
using Server.Custom.UORespawnServer.Helpers;

namespace Server.Custom.UORespawnServer.Gumps
{
    /// <summary>
    /// Gump for editing spawn lists on Box, Region, or Tile entities.
    /// Follows SRP: Gump handles UI only, SpawnEditService handles logic.
    /// </summary>
    internal class SpawnEditGump : Gump
    {
        private readonly PlayerMobile _Player;
        private readonly SpawnEditService _Service;

        // Layout constants
        private const int WIDTH = 320;
        private const int HEIGHT = 555;
        private const int MAX_ROWS = 10;
        private const int ROW_HEIGHT = 22;

        // Button IDs
        private const int BTN_CLOSE = 0;
        private const int BTN_SAVE = 1;
        private const int BTN_ADD = 2;

        // Section tabs (10-15)
        private const int BTN_TAB_WATER = 10;
        private const int BTN_TAB_WEATHER = 11;
        private const int BTN_TAB_TIMED = 12;
        private const int BTN_TAB_COMMON = 13;
        private const int BTN_TAB_UNCOMMON = 14;
        private const int BTN_TAB_RARE = 15;

        // Trigger cycle buttons
        private const int BTN_TRIGGER_PREV = 20;
        private const int BTN_TRIGGER_NEXT = 21;

        // Scroll buttons
        private const int BTN_SCROLL_UP = 30;
        private const int BTN_SCROLL_DOWN = 31;

        // Delete buttons start at 100 (100 + row index)
        private const int BTN_DELETE_BASE = 100;

        // Text entry ID for new spawn name
        private const int ENTRY_NEW_SPAWN = 1;

        // Colors
        private const int COLOR_LABEL = 0x480;
        private const int COLOR_VALUE = 0x34;
        private const int COLOR_TAB_ON = 0x40;
        private const int COLOR_TAB_OFF = 0x3B2;

        public SpawnEditGump(PlayerMobile pm, SpawnEditService service, int xOffset = 0) : base(50 + xOffset, 50)
        {
            _Player = pm;
            _Service = service;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            BuildGump();
        }

        private void BuildGump()
        {
            int y;

            // Main background (starts at y=15 to leave room for title in shadow)
            AddBackground(0, 15, WIDTH, HEIGHT, 2620);

            // Title alpha region (above main background, in shadow - matches ControlGump style)
            AddAlphaRegion(5, 20, WIDTH - 10, 18);
            string targetInfo = _Service.GetTargetInfo();
            AddHtml(0, 20, WIDTH, 18, GumpHelper.BoldCenter($"<BASEFONT COLOR=#FFAA00>{targetInfo}</BASEFONT>"), false, false);

            // === SECTION TABS (solid background with proper padding) ===
            AddBackground(0, 42, WIDTH, 85, 30546);
            AddSectionTabs(57);

            // === TRIGGER SELECTOR (Weather/Timed only) ===
            y = 137;
            if (_Service.CurrentSection == SpawnSection.Weather || _Service.CurrentSection == SpawnSection.Timed)
            {
                AddTriggerSelector(y);
            }
            // Always advance Y by same amount to keep layout consistent
            y += 30;

            // Section header (spawn list title)
            AddAlphaRegion(5, y, WIDTH - 10, 20);
            AddHtml(0, y, WIDTH, 20, GumpHelper.BoldCenter($"<BASEFONT COLOR=#88AAFF>{_Service.CurrentSection} Spawn List</BASEFONT>"), false, false);
            y += 28;

            // === SPAWN LIST ===
            int listStartY = y;
            AddSpawnList(y);

            // Scroll buttons (positioned to the right of the list)
            AddButton(WIDTH - 50, listStartY, 5600, 5604, BTN_SCROLL_UP, GumpButtonType.Reply, 0);
            AddButton(WIDTH - 50, listStartY + (MAX_ROWS * ROW_HEIGHT) - 20, 5602, 5606, BTN_SCROLL_DOWN, GumpButtonType.Reply, 0);

            y += (MAX_ROWS * ROW_HEIGHT) + 30;

            // === ADD NEW SPAWN SECTION ===
            AddAlphaRegion(5, y, WIDTH - 10, 20);
            AddHtml(0, y, WIDTH, 20, GumpHelper.BoldCenter("<BASEFONT COLOR=#88AAFF>Add New Spawn</BASEFONT>"), false, false);
            y += 28;

            // Text entry for new spawn name
            AddBackground(15, y, WIDTH - 70, 22, 9350);
            AddTextEntry(20, y + 2, WIDTH - 80, 18, COLOR_VALUE, ENTRY_NEW_SPAWN, "");
            AddButton(WIDTH - 45, y, 4011, 4012, BTN_ADD, GumpButtonType.Reply, 0);
            y += 40;

            // === FOOTER (solid background - compact like title) ===
            AddBackground(0, y, WIDTH, 35, 30546);

            // Save button (left side)
            AddButton(20, y + 5, 4023, 4024, BTN_SAVE, GumpButtonType.Reply, 0);
            AddLabel(60, y + 7, COLOR_TAB_ON, "Save Changes");

            // Close button (right side)
            AddButton(WIDTH - 45, y + 5, 4017, 4018, BTN_CLOSE, GumpButtonType.Reply, 0);

            // Version (in alpha region below footer solid - matches title style)
            y += 35;
            AddAlphaRegion(5, y, WIDTH - 10, 18);
            AddHtml(0, y, WIDTH, 18, GumpHelper.BoldCenter($"<BASEFONT COLOR=#FFAA00>Version-[{UOR_Settings.VERSION}]</BASEFONT>"), false, false);
        }

        private void AddSectionTabs(int y)
        {
            int tabWidth = 95;
            int x = 15;

            // Row 1: Water, Weather, Timed (with proper vertical spacing)
            AddTabButton(x, y, "Water", SpawnSection.Water, BTN_TAB_WATER);
            AddTabButton(x + tabWidth, y, "Weather", SpawnSection.Weather, BTN_TAB_WEATHER);
            AddTabButton(x + tabWidth * 2, y, "Timed", SpawnSection.Timed, BTN_TAB_TIMED);

            // Row 2: Common, Uncommon, Rare (30px gap between rows)
            y += 30;
            AddTabButton(x, y, "Common", SpawnSection.Common, BTN_TAB_COMMON);
            AddTabButton(x + tabWidth, y, "Uncommon", SpawnSection.Uncommon, BTN_TAB_UNCOMMON);
            AddTabButton(x + tabWidth * 2, y, "Rare", SpawnSection.Rare, BTN_TAB_RARE);
        }

        private void AddTabButton(int x, int y, string label, SpawnSection section, int buttonId)
        {
            bool isActive = _Service.CurrentSection == section;
            int color = isActive ? COLOR_TAB_ON : COLOR_TAB_OFF;

            AddButton(x, y, isActive ? 2154 : 2151, isActive ? 2151 : 2154, buttonId, GumpButtonType.Reply, 0);
            AddLabel(x + 35, y + 3, color, label);
        }

        private void AddTriggerSelector(int y)
        {
            AddLabel(15, y + 2, COLOR_LABEL, "Trigger:");

            // Previous button
            AddButton(75, y, 5603, 5607, BTN_TRIGGER_PREV, GumpButtonType.Reply, 0);

            // Current trigger value
            string triggerText = _Service.GetCurrentTriggerText();
            AddHtml(100, y, 120, 20, GumpHelper.Center($"<BASEFONT COLOR=#00DDFF>{triggerText}</BASEFONT>"), false, false);

            // Next button
            AddButton(220, y, 5601, 5605, BTN_TRIGGER_NEXT, GumpButtonType.Reply, 0);
        }

        private void AddSpawnList(int y)
        {
            List<string> list = _Service.GetCurrentSpawnList();
            int startIndex = _Service.ScrollOffset;
            int count = list?.Count ?? 0;

            for (int i = 0; i < MAX_ROWS; i++)
            {
                int dataIndex = startIndex + i;

                if (dataIndex < count)
                {
                    string spawnName = list[dataIndex] ?? "Unknown";

                    // Delete button
                    AddButton(15, y, 4017, 4018, BTN_DELETE_BASE + i, GumpButtonType.Reply, 0);

                    // Spawn name
                    AddLabel(50, y + 2, COLOR_VALUE, $"{dataIndex + 1}. {spawnName}");
                }
                else
                {
                    // Empty row placeholder
                    AddLabel(50, y + 2, COLOR_TAB_OFF, $"{dataIndex + 1}. ---");
                }

                y += ROW_HEIGHT;
            }

            // Show total count
            AddLabel(15, y + 5, COLOR_LABEL, $"Total: {count}");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_Player == null || _Player.Deleted)
                return;

            int buttonId = info.ButtonID;

            switch (buttonId)
            {
                case BTN_CLOSE:
                    _Service.CancelEdit();
                    return;

                case BTN_SAVE:
                    _Service.SaveChanges();
                    return;

                case BTN_ADD:
                    TextRelay entry = info.GetTextEntry(ENTRY_NEW_SPAWN);
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.Text))
                    {
                        _Service.AddSpawn(entry.Text.Trim());
                    }
                    break;

                // Section tabs
                case BTN_TAB_WATER: _Service.SetSection(SpawnSection.Water); break;
                case BTN_TAB_WEATHER: _Service.SetSection(SpawnSection.Weather); break;
                case BTN_TAB_TIMED: _Service.SetSection(SpawnSection.Timed); break;
                case BTN_TAB_COMMON: _Service.SetSection(SpawnSection.Common); break;
                case BTN_TAB_UNCOMMON: _Service.SetSection(SpawnSection.Uncommon); break;
                case BTN_TAB_RARE: _Service.SetSection(SpawnSection.Rare); break;

                // Trigger cycle
                case BTN_TRIGGER_PREV: _Service.CycleTrigger(-1); break;
                case BTN_TRIGGER_NEXT: _Service.CycleTrigger(1); break;

                // Scroll
                case BTN_SCROLL_UP: _Service.Scroll(-1); break;
                case BTN_SCROLL_DOWN: _Service.Scroll(1); break;

                default:
                    // Check for delete buttons
                    if (buttonId >= BTN_DELETE_BASE && buttonId < BTN_DELETE_BASE + MAX_ROWS)
                    {
                        int rowIndex = buttonId - BTN_DELETE_BASE;
                        int dataIndex = _Service.ScrollOffset + rowIndex;
                        _Service.RemoveSpawn(dataIndex);
                    }
                    break;
            }

            _Service.RefreshGump();
        }
    }
}
