using Server.Gumps;
using Server.Mobiles;
using Server.Network;

using Server.Custom.UORespawnServer.Services;
using Server.Custom.UORespawnServer.Helpers;

namespace Server.Custom.UORespawnServer.Gumps;
internal class ControlGump : Gump
{
    private readonly PlayerMobile _Player;
    private readonly ControlService _Service;

    // Button IDs
    private const int BTN_CLOSE = 0;
    private const int BTN_SAVE = 1;
    private const int BTN_OPEN = 2;
    private const int BTN_POWER = 3;

    // Toggles (10-29)
    private const int BTN_TOGGLE_LOCK = 10;
    private const int BTN_TOGGLE_DEBUG = 11;
    private const int BTN_TOGGLE_EFFECTS = 12;
    private const int BTN_TOGGLE_SCALE = 13;
    private const int BTN_TOGGLE_TOWN = 14;
    private const int BTN_TOGGLE_GRAVE = 15;
    private const int BTN_TOGGLE_RIFT = 16;
    private const int BTN_TOGGLE_VENDOR = 17;
    private const int BTN_TOGGLE_VENDOR_NIGHT = 18;
    private const int BTN_TOGGLE_VENDOR_EXTRA = 19;

    // Adjustments (100+ for decrease, 200+ for increase)
    private const int BTN_SCALE_DOWN = 100;
    private const int BTN_SCALE_UP = 200;
    private const int BTN_SEARCH_DOWN = 101;
    private const int BTN_SEARCH_UP = 201;
    private const int BTN_PROCESS_DOWN = 102;
    private const int BTN_PROCESS_UP = 202;
    private const int BTN_VALIDATE_DOWN = 103;
    private const int BTN_VALIDATE_UP = 203;
    private const int BTN_TIMED_DOWN = 104;
    private const int BTN_TIMED_UP = 204;
    private const int BTN_MAXSPAWN_DOWN = 105;
    private const int BTN_MAXSPAWN_UP = 205;
    private const int BTN_MAXRANGE_DOWN = 106;
    private const int BTN_MAXRANGE_UP = 206;
    private const int BTN_MINRANGE_DOWN = 107;
    private const int BTN_MINRANGE_UP = 207;
    private const int BTN_MAXCROWD_DOWN = 108;
    private const int BTN_MAXCROWD_UP = 208;
    private const int BTN_MAXQUEUE_DOWN = 109;
    private const int BTN_MAXQUEUE_UP = 209;

    // Chances (300+ for decrease, 400+ for increase)
    private const int BTN_CHANCE_WATER_DOWN = 300;
    private const int BTN_CHANCE_WATER_UP = 400;
    private const int BTN_CHANCE_WEATHER_DOWN = 301;
    private const int BTN_CHANCE_WEATHER_UP = 401;
    private const int BTN_CHANCE_TIMED_DOWN = 302;
    private const int BTN_CHANCE_TIMED_UP = 402;
    private const int BTN_CHANCE_COMMON_DOWN = 303;
    private const int BTN_CHANCE_COMMON_UP = 403;
    private const int BTN_CHANCE_UNCOMMON_DOWN = 304;
    private const int BTN_CHANCE_UNCOMMON_UP = 404;
    private const int BTN_CHANCE_RARE_DOWN = 305;
    private const int BTN_CHANCE_RARE_UP = 405;

    // Colors
    private const int COLOR_LABEL = 0x480;
    private const int COLOR_VALUE = 0x34;
    private const int COLOR_ON = 0x40;
    private const int COLOR_OFF = 0x20;

    public ControlGump(PlayerMobile pm, ControlService service) : base(50, 50)
    {
        _Player = pm;
        _Service = service;

        Closable = true;
        Disposable = true;
        Draggable = true;
        Resizable = false;

        BuildGump();
    }

    private void BuildGump()
    {
        bool power = _Service.SystemPower;
        int width = 420;
        int height = 670;
        int y = 0;

        // Background
        AddBackground(0, 20, width, height, 2620);
        AddAlphaRegion(5, 25, width - 10, 20);
        AddAlphaRegion(5, 130, width - 10, 20);
        AddAlphaRegion(5, 220, width - 10, 20);
        AddAlphaRegion(5, 345, width - 10, 20);
        AddAlphaRegion(5, 445, width - 10, 20);
        AddAlphaRegion(5, 520, width - 10, 20);
        AddAlphaRegion(5, 645, width - 10, 20);
        AddBackground(0, 45, width, 85, 30546);
        AddBackground(0, 615, width, 50, 30546);

        // Image
        AddImage(165, y, 30077);

        // Title
        AddHtml(0, 25, width, 20, GumpHelper.BoldCenter("<BASEFONT COLOR=#FFAA00>UORespawn Control Panel</BASEFONT>"), false, false);
        y += 25;

        // === STATS SECTION ===
        y += 30;

        AddStatRow(20, y, "Status", ControlService.GetIsPaused() ? "PAUSED" : "RUNNING", ControlService.GetIsPaused() ? COLOR_OFF : COLOR_ON);
        AddStatRow(190, y, "Locked", ControlService.GetIsLocked() ? "YES" : "NO", ControlService.GetIsLocked() ? COLOR_OFF : COLOR_ON);
        y += 20;

        AddStatRow(20, y, "Players", $"{ControlService.GetPlayerCount()}", COLOR_VALUE);
        AddStatRow(190, y, "Queued", $"{ControlService.GetQueuedCount()}", COLOR_VALUE);

        // Power
        if (power)
        {
            AddButton(355, y, 30534, 30535, BTN_POWER, GumpButtonType.Reply, 0);
        }
        else
        {
            AddButton(355, y, 30535, 30534, BTN_POWER, GumpButtonType.Reply, 0);
        }
        y += 20;

        AddStatRow(20, y, "Spawns", $"{ControlService.GetAllSpawnCount()}", COLOR_VALUE);
        AddStatRow(190, y, "Recycled", $"{ControlService.GetRecycledCount()}", COLOR_VALUE);

        // Power
        AddLabel(power ? 360 : 358, y, power ? COLOR_ON : COLOR_OFF, $"{(power ? "ON" : "OFF")}");
        y += 35;

        // === SYSTEM TOGGLES ===
        AddSectionHeader(0, y, "@-[ Controls ]-@");
        y += 25;

        AddToggleRow(20, y, "Lock System", UOR_Core.IsLocked, BTN_TOGGLE_LOCK);
        AddToggleRow(230, y, "Debug Mode", UOR_Settings.ENABLE_DEBUG, BTN_TOGGLE_DEBUG);
        y += 30;

        AddToggleRow(20, y, "Spawn Effects", UOR_Settings.ENABLE_SPAWN_EFFECTS, BTN_TOGGLE_EFFECTS);
        AddToggleRow(230, y, "Scale Spawn", UOR_Settings.ENABLE_SCALE_SPAWN, BTN_TOGGLE_SCALE);
        y += 35;

        // === SPAWN TOGGLES ===
        AddSectionHeader(0, y, "$-[ Spawn ]-$");
        y += 25;

        AddToggleRow(20, y, "Town Spawn", UOR_Settings.ENABLE_TOWN_SPAWN, BTN_TOGGLE_TOWN);
        AddToggleRow(230, y, "Vendors Spawn", UOR_Settings.ENABLE_VENDOR_SPAWN, BTN_TOGGLE_VENDOR);
        y += 30;

        AddToggleRow(20, y, "Rift Spawn", UOR_Settings.ENABLE_RIFT_SPAWN, BTN_TOGGLE_RIFT);
        AddToggleRow(230, y, "Vendor Patron", UOR_Settings.ENABLE_VENDOR_EXTRA, BTN_TOGGLE_VENDOR_EXTRA);
        y += 30;

        AddToggleRow(20, y, "Grave Spawn", UOR_Settings.ENABLE_GRAVE_SPAWN, BTN_TOGGLE_GRAVE);
        AddToggleRow(230, y, "Vendor Sleep", UOR_Settings.ENABLE_VENDOR_NIGHT, BTN_TOGGLE_VENDOR_NIGHT);
        y += 40;

        // === LIMITS ===
        AddSectionHeader(0, y, "!-[ Limits ]-!");
        y += 25;

        AddAdjustRow(20, y, "Scale Mod", $"{UOR_Settings.SCALE_MOD:F1}x", BTN_SCALE_DOWN, BTN_SCALE_UP);
        AddAdjustRow(230, y, "Max Queue", $"{UOR_Settings.MAX_QUEUE_SIZE}", BTN_MAXQUEUE_DOWN, BTN_MAXQUEUE_UP);
        y += 22;

        AddAdjustRow(20, y, "Min Range", $"{UOR_Settings.MIN_RANGE_VAL}", BTN_MINRANGE_DOWN, BTN_MINRANGE_UP);
        AddAdjustRow(230, y, "Max Range", $"{UOR_Settings.MAX_RANGE_VAL}", BTN_MAXRANGE_DOWN, BTN_MAXRANGE_UP);
        y += 22;

        AddAdjustRow(20, y, "Max Crowd", $"{UOR_Settings.MAX_CROWD_VAL}", BTN_MAXCROWD_DOWN, BTN_MAXCROWD_UP);
        AddAdjustRow(230, y, "Max Spawn", $"{UOR_Settings.MAX_SPAWN_VAL}", BTN_MAXSPAWN_DOWN, BTN_MAXSPAWN_UP);
        y += 30;

        // === INTERVALS ===
        AddSectionHeader(0, y, "*-[ Intervals ]-*");
        y += 25;

        AddAdjustRow(20, y, "Search", $"{UOR_Settings.SEARCH_INTERVAL} ms", BTN_SEARCH_DOWN, BTN_SEARCH_UP);
        AddAdjustRow(230, y, "Validate", $"{UOR_Settings.VALIDATE_INTERVAL} sec", BTN_VALIDATE_DOWN, BTN_VALIDATE_UP);
        y += 22;

        AddAdjustRow(20, y, "Process", $"{UOR_Settings.PROCESS_INTERVAL} ms", BTN_PROCESS_DOWN, BTN_PROCESS_UP);
        AddAdjustRow(230, y, "Timed", $"{UOR_Settings.TIMED_INTERVAL} min", BTN_TIMED_DOWN, BTN_TIMED_UP);
        y += 30;

        // === CHANCES ===
        AddSectionHeader(0, y, "%-[ Chances ]-%");
        y += 25;

        AddAdjustRow(20, y, "Common", $"{UOR_Settings.CHANCE_COMMON:P0}", BTN_CHANCE_COMMON_DOWN, BTN_CHANCE_COMMON_UP);
        AddAdjustRow(230, y, "Water", $"{UOR_Settings.CHANCE_WATER:P0}", BTN_CHANCE_WATER_DOWN, BTN_CHANCE_WATER_UP);
        y += 22;

        AddAdjustRow(20, y, "Uncommon", $"{UOR_Settings.CHANCE_UNCOMMON:P0}", BTN_CHANCE_UNCOMMON_DOWN, BTN_CHANCE_UNCOMMON_UP);
        AddAdjustRow(230, y, "Weather", $"{UOR_Settings.CHANCE_WEATHER:P0}", BTN_CHANCE_WEATHER_DOWN, BTN_CHANCE_WEATHER_UP);
        y += 22;

        AddAdjustRow(20, y, "Rare", $"{UOR_Settings.CHANCE_RARE:P0}", BTN_CHANCE_RARE_DOWN, BTN_CHANCE_RARE_UP);
        AddAdjustRow(230, y, "Timed", $"{UOR_Settings.CHANCE_TIMED:P0}", BTN_CHANCE_TIMED_DOWN, BTN_CHANCE_TIMED_UP);
        y += 40;

        // === BUTTONS ===
        AddButton(30, y, 4011, 4012, BTN_OPEN, GumpButtonType.Reply, 0);
        AddLabel(70, y + 2, COLOR_ON, "Edit Spawn");

        AddButton(190, y, 4023, 4024, BTN_SAVE, GumpButtonType.Reply, 0);
        AddLabel(230, y + 2, COLOR_ON, "Save Settings");

        AddButton(355, y, 4017, 4018, BTN_CLOSE, GumpButtonType.Reply, 0);
        y += 35;

        // Version
        AddHtml(0, y, width, 20, GumpHelper.BoldCenter($"<BASEFONT COLOR=#FFAA00>Version-[{UOR_Settings.VERSION}]-2026</BASEFONT>"), false, false);
    }

    private void AddSectionHeader(int x, int y, string text)
    {
        AddHtml(x, y, 420, 20, GumpHelper.Center($"<BASEFONT COLOR=#88AAFF>{text}</BASEFONT>"), false, false);
    }

    private void AddStatRow(int x, int y, string label, string value, int valueColor)
    {
        AddLabel(x, y, COLOR_LABEL, $"{label}:");
        AddLabel(x + 70, y, valueColor, value);
    }

    private void AddToggleRow(int x, int y, string label, bool isOn, int buttonId)
    {
        AddButton(x, y, isOn ? 2154 : 2151, isOn ? 2151 : 2154, buttonId, GumpButtonType.Reply, 0);
        AddLabel(x + 35, y + 3, isOn ? COLOR_ON : COLOR_OFF, label);
    }

    private void AddAdjustRow(int x, int y, string label, string value, int btnDown, int btnUp)
    {
        AddLabel(x, y, COLOR_LABEL, $"{label}:");
        AddButton(x + 80, y, 5603, 5607, btnDown, GumpButtonType.Reply, 0);
        AddHtml(x + 97, y, 50, 20, GumpHelper.Center($"<BASEFONT COLOR=#00DDFF>{value}</BASEFONT>"), false, false);
        AddButton(x + 150, y, 5601, 5605, btnUp, GumpButtonType.Reply, 0);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_Player == null || _Player.Deleted)
        {
            return;
        }

        int buttonId = info.ButtonID;

        switch (buttonId)
        {
            case BTN_CLOSE:
                _Service.CloseGump();
                return;

            case BTN_SAVE:
                _Service.SaveSettings();
                break;

            case BTN_OPEN:
                _Service.EditSpawn();
                break;

            case BTN_POWER:
                _Service.TogglePower();
                break;

            // System Toggles
            case BTN_TOGGLE_LOCK: ControlService.ToggleLock(); break;
            case BTN_TOGGLE_DEBUG: ControlService.ToggleDebug(); break;
            case BTN_TOGGLE_EFFECTS: ControlService.ToggleEffects(); break;
            case BTN_TOGGLE_SCALE: ControlService.ToggleScaleSpawn(); break;

            // Spawn Toggles
            case BTN_TOGGLE_TOWN: ControlService.ToggleTownSpawn(); break;
            case BTN_TOGGLE_GRAVE: ControlService.ToggleGraveSpawn(); break;
            case BTN_TOGGLE_RIFT: ControlService.ToggleRiftSpawn(); break;
            case BTN_TOGGLE_VENDOR: ControlService.ToggleVendorSpawn(); break;
            case BTN_TOGGLE_VENDOR_NIGHT: ControlService.ToggleVendorNight(); break;
            case BTN_TOGGLE_VENDOR_EXTRA: ControlService.ToggleVendorExtra(); break;

            // Scale & Limits
            case BTN_SCALE_DOWN: ControlService.AdjustScaleMod(-0.1); break;
            case BTN_SCALE_UP: ControlService.AdjustScaleMod(0.1); break;
            case BTN_MAXSPAWN_DOWN: ControlService.AdjustMaxSpawn(-1); break;
            case BTN_MAXSPAWN_UP: ControlService.AdjustMaxSpawn(1); break;
            case BTN_MINRANGE_DOWN: ControlService.AdjustMinRange(-1); break;
            case BTN_MINRANGE_UP: ControlService.AdjustMinRange(1); break;
            case BTN_MAXRANGE_DOWN: ControlService.AdjustMaxRange(-5); break;
            case BTN_MAXRANGE_UP: ControlService.AdjustMaxRange(5); break;
            case BTN_MAXCROWD_DOWN: ControlService.AdjustMaxCrowd(-1); break;
            case BTN_MAXCROWD_UP: ControlService.AdjustMaxCrowd(1); break;
            case BTN_MAXQUEUE_DOWN: ControlService.AdjustMaxQueueSize(-1); break;
            case BTN_MAXQUEUE_UP: ControlService.AdjustMaxQueueSize(1); break;

            // Intervals
            case BTN_SEARCH_DOWN: ControlService.AdjustSearchInterval(-25); break;
            case BTN_SEARCH_UP: ControlService.AdjustSearchInterval(25); break;
            case BTN_PROCESS_DOWN: ControlService.AdjustProcessInterval(-25); break;
            case BTN_PROCESS_UP: ControlService.AdjustProcessInterval(25); break;
            case BTN_VALIDATE_DOWN: ControlService.AdjustValidateInterval(-1); break;
            case BTN_VALIDATE_UP: ControlService.AdjustValidateInterval(1); break;
            case BTN_TIMED_DOWN: ControlService.AdjustTimedInterval(-1); break;
            case BTN_TIMED_UP: ControlService.AdjustTimedInterval(1); break;

            // Chances
            case BTN_CHANCE_WATER_DOWN: ControlService.AdjustChanceWater(-0.01); break;
            case BTN_CHANCE_WATER_UP: ControlService.AdjustChanceWater(0.01); break;
            case BTN_CHANCE_WEATHER_DOWN: ControlService.AdjustChanceWeather(-0.01); break;
            case BTN_CHANCE_WEATHER_UP: ControlService.AdjustChanceWeather(0.01); break;
            case BTN_CHANCE_TIMED_DOWN: ControlService.AdjustChanceTimed(-0.01); break;
            case BTN_CHANCE_TIMED_UP: ControlService.AdjustChanceTimed(0.01); break;
            case BTN_CHANCE_COMMON_DOWN: ControlService.AdjustChanceCommon(-0.01); break;
            case BTN_CHANCE_COMMON_UP: ControlService.AdjustChanceCommon(0.01); break;
            case BTN_CHANCE_UNCOMMON_DOWN: ControlService.AdjustChanceUncommon(-0.01); break;
            case BTN_CHANCE_UNCOMMON_UP: ControlService.AdjustChanceUncommon(0.01); break;
            case BTN_CHANCE_RARE_DOWN: ControlService.AdjustChanceRare(-0.01); break;
            case BTN_CHANCE_RARE_UP: ControlService.AdjustChanceRare(0.01); break;
        }

        _Service.RefreshGump();
    }
}
