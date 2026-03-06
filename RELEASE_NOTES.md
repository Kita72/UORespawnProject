# UORespawn v2.0.1.2 Release Notes

**Release Date:** March 6, 2026  
**Platform:** Windows, macOS  
**Framework:** .NET 10 MAUI with Blazor

---

## 🎉 What's New

### 🏆 Official App Badge
Show your support! UORespawn now includes an official badge you can display on your server website or documentation.

- **Easy Download** - Click the badge in the navigation menu to save it
- **Windows** - Opens folder picker to choose save location
- **macOS** - Saves directly to Downloads folder
- **Full Quality** - Uncompressed PNG for best display

### 🎮 New In-Game Commands
Two new commands for quick spawn management without opening gumps:

| Command | Description | Access |
|---------|-------------|--------|
| `[UORAdd` | Add creatures to spawn at your location | GameMaster+ |
| `[UORDrop` | Remove creatures from spawn at your location | GameMaster+ |

---

## 📝 Documentation Updates

### Section 12: In-Game Editing (Complete Rewrite)
The instructions for in-game editing have been completely rewritten to accurately document the actual workflow:

1. Open Control Panel with `[UORespawn`
2. Click **Edit Spawn** button to get target cursor
3. Target ground, signs, beehives, or items
4. Edit spawns directly in the gump interface
5. Save changes

### Updated Command Documentation
- **[ShowRespawn]** - Now works as a toggle (call once to enable call-outs, again to disable)
- Settings defaults synced with server values

---

## 🗑️ Removed

### MaxRecycleTotal Setting
This setting has been removed from the editor. The server now automatically calculates the optimal recycle total based on active player count, providing better performance without manual tuning.

---

## 🔧 Technical Changes

- Badge container positioned in nav-footer for consistent bottom-aligned layout
- Windows folder picker integration for badge saving
- Cleaned up incorrect documentation references

---

## 📦 Installation

### Fresh Install
1. Download the release ZIP for your platform
2. Extract to your desired location
3. Run `UORespawnApp.exe` (Windows) or `UORespawnApp.app` (macOS)

### Upgrade from v2.0.1.1
1. Close the existing UORespawn Editor
2. Replace the application files with the new version
3. Your spawn data and settings are preserved in the Data folder

---

## 🔗 Links

- **GitHub:** [UORespawnProject](https://github.com/Kita72/UORespawnProject)
- **Documentation:** See Instructions page in the app
- **Support:** Open an issue on GitHub

---

## 🙏 Thank You

Thank you for using UORespawn! Your feedback helps make this tool better for the entire ServUO community.

*Happy spawning!* 🎮
