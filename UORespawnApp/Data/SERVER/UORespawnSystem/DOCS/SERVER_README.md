# UORespawnServer - Server Documentation

> **Version:** 2.0.0.7  
> **Target:** .NET Framework 4.8  
> **Last Updated:** Feb 2026

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Directory Structure](#directory-structure)
4. [Data Files](#data-files)
5. [Settings Configuration](#settings-configuration)
6. [Spawn System](#spawn-system)
7. [Logging System](#logging-system)
8. [Editor Integration](#editor-integration)
9. [Spawn Packs](#spawn-packs)

---

## Overview

UORespawnServer is a dynamic player-centric spawn system for ServUO. Instead of static world spawners, creatures spawn around players based on their location, terrain, region, and environmental conditions.

### Key Features

- **Player-Centric Spawning** - Mobs spawn near active players, not fixed locations
- **Multi-Layer Spawn Resolution** - Box → Region → Tile priority system
- **O(1) Spatial Lookups** - SpatialGridManager provides instant box lookups
- **Mob Recycling** - RecycleService pools mobs for performance
- **Weather/Time Spawns** - Conditional spawns based on environment
- **Vendor System** - Dynamic NPC vendor spawning at sign locations
- **Unified Logging Pipeline** - `UOR_Utility.SendMsg` drives console + file logging by color
- **Auto-Sync with Editor** - Server generates data, Editor consumes it automatically

### 🔄 Server ↔ Editor Data Marriage

The server and editor work in a **synchronized partnership**:

```
┌─────────────────────────────────────────────────────────────────────┐
│                     AUTO-SYNC DATA FLOW                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   SERVER (Auto-Generates on Every Startup)                          │
│   ├── Scans all BaseCreature types → UOR_BestiaryList.txt           │
│   ├── Scans all Map regions → UOR_RegionList.txt                    │
│   ├── Scans all terrain tiles → UOR_TileList.txt                    │
│   ├── Scans all Maps → UOR_MapList.txt                              │
│   ├── Scans all ISpawner/XmlSpawner → UOR_SpawnerList.txt           │
│   ├── Scans all Sign items → UOR_SignData.txt                       │
│   └── Scans all Beehive statics → UOR_HiveData.txt                  │
│                              ↓                                      │
│                       OUTPUT/ folder                                │
│                              ↓                                      │
│   EDITOR (Reads on Launch)                                          │
│   ├── Populates Bestiary dropdown (all spawnable creatures)         │
│   ├── Populates Region list (all named regions per map)             │
│   ├── Populates Tile list (all terrain types)                       │
│   ├── Populates Map selector (all available maps)                   │
│   ├── Shows existing spawner locations (reference)                  │
│   └── Shows vendor sign/hive locations                              │
│                              ↓                                      │
│   User edits spawn data in Editor                                   │
│                              ↓                                      │
│   EDITOR (Saves)                                                    │
│   └── Writes spawn definitions → INPUT/*.bin files                  │
│                              ↓                                      │
│   SERVER (Loads on Startup or Data Change)                          │
│   └── Reads INPUT/*.bin → Active spawn system                       │
│                                                                     │
│                        [UOR_Settings.csv]                           │
│                   <Two Way Reading & Writing>                       │
│                               ↓                                     │
│         -Server - Starts ── Read UOR_Settings.csv                   │
│         -Editor - Starts ── Read UOR_Settings.csv                   │
│                               ↓                                     │
│         -Server - Game Gump ── Edit UOR_Settings                    │
│         -Editor - Settings Page ── Edit UOR_Settings                │
│                               ↓                                     │
│         -Server - Game Command ── Save UOR_Settings.csv             │
│         -Editor - Closes ── Save UOR_Settings.csv                   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**This means:**
- ✅ Add a new creature to the game → Server auto-includes it in bestiary next startup
- ✅ Add a new region to regions.xml → Server auto-includes it in region list
- ✅ Add a new XmlSpawner → Server auto-includes it in spawner list
- ✅ No manual file editing required for game data
- ✅ Editor always has current server state when launched

---

## Architecture

### Core Components

| Component | Purpose |
|-----------|---------|
| `UOR_Core` | Main orchestrator - initialization, events, state management |
| `UOR_Settings` | Configuration loading and runtime settings |
| `UOR_DIR` | Directory and file path management |
| `UOR_Utility` | Shared utility methods |

### Services (8 total)

| Service | Purpose |
|---------|---------|
| `ProcessService` | Main spawn processing and mob creation |
| `RecycleService` | Mob pooling and recycling for performance |
| `TrackService` | Spawn location tracking and persistence |
| `ValidateService` | Spawn validation and cleanup |
| `TimedService` | Time-based spawn updates (day/night) |
| `StatsService` | Spawn statistics collection |
| `VendorService` | Vendor NPC lifecycle management |
| `ControlService` | In-game settings gump interface |

### Managers (5 total)

| Manager | Purpose |
|---------|---------|
| `SpawnManager` | Loads and manages all spawn data from binary files |
| `SpatialGridManager` | O(1) spatial grid for box spawn lookups |
| `GameManager` | Generates server data lists for Editor |
| `VendorManager` | Sign and hive location management |
| `LogManager` | Session log buffering, color-level tagging, and shutdown flush |

### Spawners (4 total)

| Spawner | Purpose |
|---------|---------|
| `BoxSpawner` | Geographic box-based spawns (highest priority) |
| `RegionSpawner` | Region-based spawns (medium priority) |
| `TileSpawner` | Terrain tile-based spawns (lowest priority) |
| `VendorSpawner` | Vendor NPC spawning at sign locations |

### Timers (4 total)

| Timer | Interval | Purpose |
|-------|----------|---------|
| `ProcessTimer` | 250ms | Process spawn queue |
| `SearchTimer` | 75ms | Search for spawn locations per player |
| `ValidateTimer` | 5s | Validate existing spawns |
| `TimedTimer` | 1min | Update time-based spawns |

---

## Directory Structure

```
Data/
└── UORespawn/
    ├── INPUT/          # Editor → Server (binary spawn data)
    │   ├── UOR_BoxSpawn.bin
    │   ├── UOR_RegionSpawn.bin
    │   ├── UOR_TileSpawn.bin
    │   ├── UOR_VendorSpawn.bin
    │   └── UOR_SpawnSettings.csv    # ⚠️ NEW: CSV format settings
    │
    ├── OUTPUT/         # Server → Editor (generated lists)
    │   ├── UOR_MapList.txt
    │   ├── UOR_BestiaryList.txt
    │   ├── UOR_RegionList.txt
    │   ├── UOR_TileList.txt
    │   ├── UOR_SpawnerList.txt
    │   ├── UOR_VendorList.txt
    │   ├── UOR_SignData.txt
    │   └── UOR_HiveData.txt
    │
    ├── STATS/          # Statistics data
    │
    └── SYS/            # System files
        ├── UOR_TrackSpawn.txt
        ├── UOR_VendorSpawn.txt
        └── UOR_DebugLog.txt
```

---

## Data Files

### Binary Files (Editor Creates → Server Loads)

| File | Format | Description |
|------|--------|-------------|
| `UOR_BoxSpawn.bin` | Binary | Geographic spawn boxes with priority |
| `UOR_RegionSpawn.bin` | Binary | Region-based spawn definitions |
| `UOR_TileSpawn.bin` | Binary | Terrain tile spawn definitions |
| `UOR_VendorSpawn.bin` | Binary | Vendor spawn locations |

#### Binary Format Structure

All binary files follow this header format:
```
int32   FileVersion
string  VersionString
int32   MapCount
[per map]:
  int32   MapId
  string  MapName
  int32   EntityCount
  [per entity]: ... entity-specific data
```

### Text Files (Server Auto-Generates → Editor Reads)

**⚠️ These files are regenerated every server startup!**

The server scans the game world and creates fresh data files that the Editor uses to populate its UI. This ensures the Editor always has the latest game state.

| File | Auto-Generated From | Editor Uses For |
|------|---------------------|-----------------|
| `UOR_MapList.txt` | `Map.Maps[]` array | Map dropdown selector |
| `UOR_BestiaryList.txt` | All `BaseCreature` types via reflection | Creature/mob selector |
| `UOR_RegionList.txt` | `Region.Regions` collection | Region selector per map |
| `UOR_TileList.txt` | `TileData.LandTable` | Terrain type selector |
| `UOR_SpawnerList.txt` | All `ISpawner` and `XmlSpawner` items | Reference view of existing spawners |
| `UOR_SignData.txt` | All `Sign` items in world | Vendor location picker |
| `UOR_HiveData.txt` | All beehive static items | Beekeeper vendor locations |

#### Auto-Sync Benefits

1. **New Creatures** - Add a new `BaseCreature` class to the server, restart, and it automatically appears in the Editor's bestiary list
2. **New Regions** - Add regions to `regions.xml`, restart server, and they appear in Editor's region list
3. **New Spawners** - Place an XmlSpawner in-game, restart, and it shows in Editor's spawner reference
4. **New Signs** - Add shop signs to the world, restart, and they become available as vendor locations
5. **No Manual Sync** - The Editor never needs manual updates for game data

---

## Settings Configuration

### ⚠️ IMPORTANT: Settings Format Change

**The settings file has changed from binary (.bin) to CSV (.csv) format!**

**File:** `Data/UORespawn/INPUT/UOR_SpawnSettings.csv`

### CSV Format

```csv
# UORespawn Settings File
# Format: SettingName,Value
# Lines starting with # are comments

# Scale Modifier
SCALE_MOD,1.0

# System Intervals (milliseconds/seconds/minutes as noted)
SEARCH_INTERVAL,75
PROCESS_INTERVAL,250
VALIDATE_INTERVAL,5
TIMED_INTERVAL,1

# System Limits
MAX_RECYCLE_TYPE,20
MAX_RECYCLE_TOTAL,50000
MAX_SPAWN_CHECKS,3
MAX_QUEUE_SIZE,5
MAX_STAT_SIZE,10000

# Spawn Limits
MAX_SPAWN,25
MIN_RANGE,20
MAX_RANGE,80
MAX_CROWD,3

# Spawn Chances (0.0 to 1.0)
CHANCE_WATER,0.25
CHANCE_WEATHER,0.01
CHANCE_TIMED,0.01
CHANCE_COMMON,1.0
CHANCE_UNCOMMON,0.1
CHANCE_RARE,0.01

# Spawn Toggles (True/False)
ENABLE_SCALE_SPAWN,False
ENABLE_RIFT_SPAWN,False
ENABLE_TOWN_SPAWN,True
ENABLE_GRAVE_SPAWN,True

# Vendor Toggles (True/False)
ENABLE_VENDOR_SPAWN,False
ENABLE_VENDOR_NIGHT,False
ENABLE_VENDOR_EXTRA,False

# Effects Toggle
ENABLE_SPAWN_EFFECTS,True

# Debug Toggle
ENABLE_DEBUG,False
```

### Settings Reference

#### System Intervals

| Setting | Default | Unit | Description |
|---------|---------|------|-------------|
| `SEARCH_INTERVAL` | 75 | ms | How often to search for spawn locations per player |
| `PROCESS_INTERVAL` | 250 | ms | How often to process the spawn queue |
| `VALIDATE_INTERVAL` | 5 | sec | How often to validate existing spawns |
| `TIMED_INTERVAL` | 1 | min | How often to check time-based spawns |

#### System Limits

| Setting | Default | Description |
|---------|---------|-------------|
| `MAX_RECYCLE_TYPE` | 20 | Max mobs cached per type in recycle pool |
| `MAX_RECYCLE_TOTAL` | 50000 | Max total mobs in recycle pool |
| `MAX_SPAWN_CHECKS` | 3 | Max attempts to find valid spawn point |
| `MAX_QUEUE_SIZE` | 5 | Max locations queued per player |
| `MAX_STAT_SIZE` | 10000 | Max statistics entries |

#### Spawn Limits

| Setting | Default | Description |
|---------|---------|-------------|
| `MAX_SPAWN` | 25 | Max mobs in spawn range |
| `MIN_RANGE` | 20 | Minimum spawn distance from player |
| `MAX_RANGE` | 80 | Maximum spawn distance from player |
| `MAX_CROWD` | 3 | Crowd multiplier |

#### Spawn Chances

| Setting | Default | Description |
|---------|---------|-------------|
| `CHANCE_WATER` | 0.25 | Probability for water spawns |
| `CHANCE_WEATHER` | 0.01 | Probability for weather spawns |
| `CHANCE_TIMED` | 0.01 | Probability for timed spawns |
| `CHANCE_COMMON` | 1.0 | Probability for common spawns |
| `CHANCE_UNCOMMON` | 0.1 | Probability for uncommon spawns |
| `CHANCE_RARE` | 0.01 | Probability for rare spawns |

#### Feature Toggles

| Setting | Default | Description |
|---------|---------|-------------|
| `ENABLE_SCALE_SPAWN` | False | Enable spawn scaling with SCALE_MOD |
| `ENABLE_RIFT_SPAWN` | False | Enable rift spawn events |
| `ENABLE_TOWN_SPAWN` | True | Allow spawns in town regions |
| `ENABLE_GRAVE_SPAWN` | True | Enable grave spawn effects |
| `ENABLE_VENDOR_SPAWN` | False | Enable vendor NPC spawning |
| `ENABLE_VENDOR_NIGHT` | False | Vendors affected by day/night |
| `ENABLE_VENDOR_EXTRA` | False | Spawn extra TownNPCs with vendors |
| `ENABLE_SPAWN_EFFECTS` | True | Show spawn visual effects |
| `ENABLE_DEBUG` | False | Enable verbose debug logging |

---

## Spawn System

### Spawn Resolution Priority

When determining what to spawn at a location, the system checks in this order:

1. **BoxSpawner** (Highest Priority)
   - Uses SpatialGridManager for O(1) lookup
   - Geographic rectangles with explicit spawn lists
   - Higher priority boxes override lower ones

2. **RegionSpawner** (Medium Priority)
   - Dictionary lookup by Region handle
   - Falls back if no box covers location

3. **TileSpawner** (Lowest Priority)
   - Dictionary lookup by tile name
   - Used when no box or region match

### Spawn Categories

Each spawn entity (Box/Region/Tile) contains 6 spawn lists:

| List | Trigger | Chance Setting |
|------|---------|----------------|
| `WaterList` | Player on water tile | `CHANCE_WATER` |
| `WeatherList` | Active weather | `CHANCE_WEATHER` |
| `TimedList` | Night time | `CHANCE_TIMED` |
| `CommonList` | Always | `CHANCE_COMMON` |
| `UnCommonList` | Random | `CHANCE_UNCOMMON` |
| `RareList` | Random | `CHANCE_RARE` |

### Spawn Flow

```
Player Login
    ↓
SearchTimer (75ms per player)
    ↓
Queue spawn location (max 5 per player)
    ↓
ProcessTimer (250ms global)
    ↓
BoxSpawner → RegionSpawner → TileSpawner
    ↓
RecycleService (get from pool or create new)
    ↓
Spawn mob at location
    ↓
ValidateTimer (5s) - cleanup invalid spawns
```

---

## Logging System

### Overview

UORespawn uses a unified logging system built around `UOR_Utility.SendMsg()`. This single entry point handles both console output and session logging, with behavior controlled by `ConsoleColor` and the `ENABLE_DEBUG` setting.

### How It Works

```csharp
// All logging goes through SendMsg
UOR_Utility.SendMsg(ConsoleColor.Green, "SERVICES-[Initialized]");
UOR_Utility.SendMsg(ConsoleColor.Yellow, "PROCESS-[Started]");
UOR_Utility.SendMsg(ConsoleColor.Red, "REGION SPAWN: Exception occurred");
```

**Console Output Rules:**
- **System Colors** → Always shown on console (regardless of debug setting)
- **Error Colors (`Red`, `DarkRed`)** → Always shown on console
- **Debug Detail Colors (`Yellow`, `DarkYellow`)** → Shown only when `ENABLE_DEBUG=True`

**Log File Rules:**
- **System** and **Error** messages are always logged
- **Debug Detail** messages are logged only when `ENABLE_DEBUG=True`
- Messages are tagged by color: INFO (system), DEBG (yellow), ERROR (red)

### System Colors (Always Visible)

These colors represent important system messages that always display on the console:

| Color | Typical Use |
|-------|-------------|
| `Magenta` / `DarkMagenta` | System status (STARTED, STOPPED) |
| `Blue` / `DarkBlue` | Branding/Logo |
| `Green` / `DarkGreen` | Success messages (Initialized, Loaded) |
| `Cyan` / `DarkCyan` | Info messages (counts, stats) |

### Message Colors

| Color | Level | When Shown | Description |
|-------|-------|------------|-------------|
| System colors | INFO | Always | Normal operations |
| `Yellow` / `DarkYellow` | DEBG | Debug only | Detailed troubleshooting output |
| `Red` / `DarkRed` | ERROR | Always | Failures and exceptions |

### Message Format Convention

Messages follow a consistent format: `COMPONENT-[Details]`

```
SERVICES-[Initialized]
PROCESS-[250 Created]
TILE SPAWN-[Found 'Rabbit' on tile 'grass' (hits: 42/50)]
REGION SPAWN-[Found 'Orc' in region 'Yew' (hits: 15/20)]
```

### Log Output

**File:** `Data/UORespawn/SYS/UOR_DebugLog.txt`

The log file is written on server shutdown/crash and includes:
- Session info (start/end times, duration, end reason)
- Log summary (counts by level: INFO, DEBUG, ERROR)
- Settings snapshot (current configuration)
- All log entries from the session

### Sample Log Output

```
╔══════════════════════════════════════════════════════════════╗
║                    UORespawn Session Log                      
╚══════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────┐
│ SESSION INFO                                                 
├─────────────────────────────────────────────────────────────┤
│ Version      : 2.0.0.7                                       │
│ Session Start: 2026-02-27 16:31:11                          
│ Session End  : 2026-02-27 16:52:43                          
│ Duration     : 00:21:32                                     
│ End Reason   : Server Shutdown                              
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ LOG SUMMARY                                                  
├─────────────────────────────────────────────────────────────┤
│ INFO: 67      DEBUG: 17     ERROR: 0                        
│ Total Entries: 84                                           
└─────────────────────────────────────────────────────────────┘

INFO:SERVICES-[Initialized]
INFO:PROCESS-[250 Created]
DEBG:SEARCH-[PlayerA: 50 locations pushed, queue: 5]
ERROR:REGION SPAWN: Exception occurred
```

### Debug Mode

When `ENABLE_DEBUG=True`:
- All messages (including yellow/red) appear on console
- All messages are recorded to session log
- Spawner hit/miss ratios are logged
- Detailed state changes are tracked

When `ENABLE_DEBUG=False`:
- System and error messages appear on console
- System and error messages are still recorded to session log
- Minimal console output for production use

---

## Editor Integration

### 🔄 The Server-Editor Marriage

The server and editor are designed as a **married pair** - each has a specific role:

| Server's Job | Editor's Job |
|--------------|--------------|
| Generate game data (OUTPUT/) | Read game data to populate UI |
| Load spawn definitions (INPUT/) | Create/edit spawn definitions |
| Run the spawn system | Provide user-friendly editing |
| Auto-sync on every startup | Auto-read on every launch |

### Editor Must Read Server Data on Launch

**⚠️ CRITICAL:** The Editor should **always read server-generated files on launch** to:

1. **Load `OUTPUT/*.txt` files** - Populate bestiary, regions, tiles, maps, etc.
2. **Load `INPUT/UOR_SpawnSettings.csv`** - Get current server settings
3. **Display current server state** - Never show stale data

### Why Auto-Generate?

The server auto-generates `OUTPUT/` files because:

- **Game changes constantly** - New creatures, regions, spawners added
- **No manual sync needed** - Just restart server to update
- **Editor always current** - Launch editor, get latest data
- **Single source of truth** - Server IS the game state

### Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                        EDITOR                                │
├─────────────────────────────────────────────────────────────┤
│  ON LAUNCH:                                                 │
│  1. Read OUTPUT/UOR_BestiaryList.txt → Creature dropdown    │
│  2. Read OUTPUT/UOR_RegionList.txt → Region dropdown        │
│  3. Read OUTPUT/UOR_TileList.txt → Tile dropdown            │
│  4. Read OUTPUT/UOR_MapList.txt → Map dropdown              │
│  5. Read OUTPUT/UOR_SignData.txt → Vendor locations         │
│  6. Read INPUT/UOR_SpawnSettings.csv → Settings panel       │
│                                                             │
│  ON SAVE:                                                   │
│  1. Write INPUT/UOR_BoxSpawn.bin                            │
│  2. Write INPUT/UOR_RegionSpawn.bin                         │
│  3. Write INPUT/UOR_TileSpawn.bin                           │
│  4. Write INPUT/UOR_VendorSpawn.bin                         │
│  5. Write INPUT/UOR_SpawnSettings.csv (if changed)          │
└─────────────────────────────────────────────────────────────┘
                              ↕
┌─────────────────────────────────────────────────────────────┐
│                        SERVER                                │
├─────────────────────────────────────────────────────────────┤
│  ON STARTUP:                                                │
│  1. Generate OUTPUT/UOR_BestiaryList.txt (scan creatures)   │
│  2. Generate OUTPUT/UOR_RegionList.txt (scan regions)       │
│  3. Generate OUTPUT/UOR_TileList.txt (scan tiles)           │
│  4. Generate OUTPUT/UOR_MapList.txt (scan maps)             │
│  5. Generate OUTPUT/UOR_SignData.txt (scan signs)           │
│  6. Load INPUT/*.bin (spawn definitions from Editor)        │
│  7. Load INPUT/UOR_SpawnSettings.csv (configuration)        │
│                                                             │
│  RUNTIME:                                                   │
│  - Watch for INPUT/ changes → Hot-reload if changed         │
└─────────────────────────────────────────────────────────────┘
```

### Files Editor Reads (Auto-Generated by Server)

| File | Generated From | Populates |
|------|----------------|-----------|
| `OUTPUT/UOR_MapList.txt` | Map.Maps[] | Map dropdown |
| `OUTPUT/UOR_BestiaryList.txt` | All BaseCreature types | Creature selector |
| `OUTPUT/UOR_RegionList.txt` | Region.Regions | Region selector |
| `OUTPUT/UOR_TileList.txt` | TileData.LandTable | Tile selector |
| `OUTPUT/UOR_SignData.txt` | World Sign items | Vendor location picker |
| `OUTPUT/UOR_HiveData.txt` | World beehive statics | Beekeeper locations |
| `INPUT/UOR_SpawnSettings.csv` | Current configuration | Settings panel |

### Files Editor Writes (Server Loads)

| File | Purpose |
|------|---------|
| `INPUT/UOR_BoxSpawn.bin` | Box spawn definitions |
| `INPUT/UOR_RegionSpawn.bin` | Region spawn definitions |
| `INPUT/UOR_TileSpawn.bin` | Tile spawn definitions |
| `INPUT/UOR_VendorSpawn.bin` | Vendor spawn definitions |
| `INPUT/UOR_SpawnSettings.csv` | Updated settings |

---

## Spawn Packs

### Overview

Spawn packs are pre-configured sets of spawn and settings files that can be applied to quickly change the server's spawn configuration.

### Pack Location

```
Data/
└── PACKS/              # Spawn pack storage
    ├── DefaultPack/
    │   ├── UOR_BoxSpawn.bin
    │   ├── UOR_RegionSpawn.bin
    │   ├── UOR_TileSpawn.bin
    │   ├── UOR_VendorSpawn.bin
    │   └── UOR_SpawnSettings.csv
    │
    └── CustomPack/
        └── ...
```

### Applying a Pack

When applying a spawn pack:

1. Copy all `.bin` files from pack to the live data folder (`Data/UORespawn/INPUT/` on server, `Data/UOR_DATA/` in editor workflow)
2. Copy `UOR_SpawnSettings.csv` from pack to the same live data folder
3. Server data watcher detects changes and reloads
4. `PACKS/` serves as backup/staging area

### Editor Pack Integration

When creating/saving a spawn pack in Editor:

1. Export current spawn data to pack folder
2. **Include current `UOR_SpawnSettings.csv`** in pack
3. Pack represents complete server state

When loading a spawn pack in Editor:

1. Read pack's `UOR_SpawnSettings.csv`
2. Update Editor UI to match pack settings
3. Load pack's spawn data for editing

---

## Server Events

The system hooks into these ServUO events:

| Event | Action |
|-------|--------|
| `EventSink.WorldLoad` | Initialize system, load spawns |
| `EventSink.WorldSave` | Save tracked spawns, flush logs |
| `EventSink.Login` | Add player to respawner list |
| `EventSink.Logout` | Remove player from respawner list |
| `EventSink.PlayerDeath` | Track death for statistics |
| `EventSink.Shutdown` | Cleanup, flush final log |
| `EventSink.Crashed` | Emergency log flush |

---

## Troubleshooting

### Common Issues

1. **No spawns appearing**
   - Check `UOR_DebugLog.txt` for errors
   - Verify binary files exist in `INPUT/`
   - Enable `ENABLE_DEBUG=True` for verbose logging

2. **Settings not loading**
   - Verify `UOR_SpawnSettings.csv` exists
   - Check CSV format (no extra spaces, valid values)
   - Look for parsing errors in log

3. **Editor data out of sync**
   - Run server once to generate `OUTPUT/` files
   - Editor should read these on launch
   - If creature/region missing, restart server to regenerate

4. **New creature not in Editor**
   - Restart server to regenerate `UOR_BestiaryList.txt`
   - Ensure creature inherits from `BaseCreature`
   - Check creature has parameterless constructor

5. **New region not in Editor**
   - Restart server to regenerate `UOR_RegionList.txt`
   - Verify region is in `regions.xml` with valid name

6. **Performance issues**
   - Reduce `MAX_SPAWN` and `MAX_RANGE`
   - Increase `PROCESS_INTERVAL`
   - Check recycle pool limits

7. **"Tile Doesn't Exist" warnings**
   - Expected for unconfigured tiles (e.g., `wooden floor`, `stone floor`)
   - Indoor/structural tiles intentionally have no spawn data
   - Add tile to configuration in Editor if spawns are desired

---

## Version History

| Version | Changes |
|---------|---------|
| 2.0.0.7 | Settings changed from binary to CSV format |
| 2.0.0.7 | Logging unified through UOR_Utility.SendMsg |
| 2.0.0.7 | ControlGump layout fixes and improvements | 
| 2.0.0.7 | Added SpatialGridManager for O(1) lookups |
| 2.0.0.7 | Added Dictionary lookups for Region/Tile |

---

## Quick Reference

### For Server Admins

```
Add new creature → Restart server → Auto-appears in Editor bestiary
Add new region   → Restart server → Auto-appears in Editor regions
Add new spawner  → Restart server → Auto-appears in Editor reference
Edit settings    → Edit UOR_SpawnSettings.csv → Restart server
```

### For Editor Users

```
On Launch:
  - Read OUTPUT/*.txt for dropdowns (auto-generated by server)
  - Read INPUT/UOR_SpawnSettings.csv for settings

On Save:
  - Write INPUT/*.bin for spawn data
  - Write INPUT/UOR_SpawnSettings.csv if settings changed

Never manually edit OUTPUT/ files - they are regenerated every server start!
```

### The Marriage Summary

| Server Does | Editor Does |
|-------------|-------------|
| Scans game → Generates OUTPUT/ | Reads OUTPUT/ → Populates UI |
| Loads INPUT/ → Runs spawns | Saves INPUT/ → Defines spawns |
| Owns game truth | Owns user editing |
| Regenerates on startup | Refreshes on launch |

---

## Contact

For issues or questions about the server-side code, check `UOR_DebugLog.txt` first, then refer to this documentation.
