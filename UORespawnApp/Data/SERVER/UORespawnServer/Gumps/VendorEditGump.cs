using System.Collections.Generic;

using Server.Gumps;
using Server.Mobiles;
using Server.Network;

using Server.Custom.UORespawnServer.Services;

namespace Server.Custom.UORespawnServer.Gumps
{
    /// <summary>
    /// Gump for editing vendor spawn lists.
    /// Follows SRP: Gump handles UI only, VendorEditService handles logic.
    /// Simplified gump - single vendor list, no section tabs.
    /// </summary>
    internal class VendorEditGump : Gump
    {
        private readonly PlayerMobile _Player;
        private readonly VendorEditService _Service;

        // Layout constants
        private const int WIDTH = 320;
        private const int HEIGHT = 420;
        private const int MAX_ROWS = 10;
        private const int ROW_HEIGHT = 22;

        // Button IDs
        private const int BTN_CLOSE = 0;
        private const int BTN_SAVE = 1;
        private const int BTN_ADD = 2;

        // Scroll buttons
        private const int BTN_SCROLL_UP = 30;
        private const int BTN_SCROLL_DOWN = 31;

        // Delete buttons start at 100 (100 + row index)
        private const int BTN_DELETE_BASE = 100;

        // Text entry ID for new vendor name
        private const int ENTRY_NEW_VENDOR = 1;

        // Colors
        private const int COLOR_LABEL = 0x480;
        private const int COLOR_VALUE = 0x34;
        private const int COLOR_TAB_ON = 0x40;

        public VendorEditGump(PlayerMobile pm, VendorEditService service) : base(50, 50)
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
            int y = 0;

            // Main background (starts at y=15 to leave room for title in shadow)
            AddBackground(0, 15, WIDTH, HEIGHT, 2620);

            // Title alpha region (above main background, in shadow - matches ControlGump style)
            AddAlphaRegion(5, 20, WIDTH - 10, 18);
            string locationType = _Service.IsSign ? "Sign" : "Beehive";
            string locationInfo = _Service.GetLocationInfo();
            AddHtml(0, 20, WIDTH, 18, Bold(Center($"<BASEFONT COLOR=#FFAA00>Vendor: {locationInfo}</BASEFONT>")), false, false);

            // === VENDOR INFO SECTION ===
            y = 48;
            AddBackground(0, y, WIDTH, 40, 30546);
            AddAlphaRegion(10, y + 8, WIDTH - 20, 24);
            
            string typeText = _Service.IsSign ? "Shop Sign Location" : "Beehive Location";
            int vendorCount = _Service.GetVendorCount();
            AddHtml(0, y + 10, WIDTH, 20, Bold(Center($"<BASEFONT COLOR=#88AAFF>{typeText} ({vendorCount} vendors)</BASEFONT>")), false, false);
            y += 48;

            // Section header (vendor list title)
            AddAlphaRegion(5, y, WIDTH - 10, 20);
            AddHtml(0, y, WIDTH, 20, Bold(Center("<BASEFONT COLOR=#88AAFF>Vendor Spawn List</BASEFONT>")), false, false);
            y += 28;

            // === VENDOR LIST ===
            int listStartY = y;
            AddVendorList(y);

            // Scroll buttons (positioned to the right of the list)
            AddButton(WIDTH - 50, listStartY, 5600, 5604, BTN_SCROLL_UP, GumpButtonType.Reply, 0);
            AddButton(WIDTH - 50, listStartY + (MAX_ROWS * ROW_HEIGHT) - 20, 5602, 5606, BTN_SCROLL_DOWN, GumpButtonType.Reply, 0);

            y += (MAX_ROWS * ROW_HEIGHT) + 30;

            // === ADD NEW VENDOR SECTION ===
            AddAlphaRegion(5, y, WIDTH - 10, 20);
            AddHtml(0, y, WIDTH, 20, Bold(Center("<BASEFONT COLOR=#88AAFF>Add New Vendor</BASEFONT>")), false, false);
            y += 28;

            // Text entry for new vendor name
            AddBackground(15, y, WIDTH - 70, 22, 9350);
            AddTextEntry(20, y + 2, WIDTH - 80, 18, COLOR_VALUE, ENTRY_NEW_VENDOR, "");
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
            AddHtml(0, y, WIDTH, 18, Bold(Center($"<BASEFONT COLOR=#FFAA00>Version-[{UOR_Settings.VERSION}]</BASEFONT>")), false, false);
        }

        private void AddVendorList(int y)
        {
            List<string> list = _Service.GetVendorList();
            int startIndex = _Service.ScrollOffset;
            int count = list?.Count ?? 0;

            for (int i = 0; i < MAX_ROWS; i++)
            {
                int dataIndex = startIndex + i;

                if (dataIndex < count)
                {
                    string vendorName = list[dataIndex] ?? "Unknown";

                    // Delete button
                    AddButton(15, y, 4017, 4018, BTN_DELETE_BASE + i, GumpButtonType.Reply, 0);

                    // Vendor name
                    AddLabel(50, y + 2, COLOR_VALUE, $"{dataIndex + 1}. {vendorName}");
                }

                y += ROW_HEIGHT;
            }
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
                    _Service.RefreshGump();
                    return;

                case BTN_ADD:
                    string newVendor = info.GetTextEntry(ENTRY_NEW_VENDOR)?.Text?.Trim();
                    _Service.AddVendor(newVendor);
                    _Service.RefreshGump();
                    return;

                case BTN_SCROLL_UP:
                    _Service.Scroll(-1);
                    _Service.RefreshGump();
                    return;

                case BTN_SCROLL_DOWN:
                    _Service.Scroll(1);
                    _Service.RefreshGump();
                    return;
            }

            // Delete buttons (100+)
            if (buttonId >= BTN_DELETE_BASE && buttonId < BTN_DELETE_BASE + MAX_ROWS)
            {
                int rowIndex = buttonId - BTN_DELETE_BASE;
                int actualIndex = _Service.ScrollOffset + rowIndex;
                _Service.RemoveVendor(actualIndex);
                _Service.RefreshGump();
                return;
            }
        }

        // Helper methods for HTML formatting
        private string Bold(string text) => $"<B>{text}</B>";
        private string Center(string text) => $"<CENTER>{text}</CENTER>";
    }
}
