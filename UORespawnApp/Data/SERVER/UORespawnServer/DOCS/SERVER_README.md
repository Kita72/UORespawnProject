# UORespawnServer - Server Documentation

> **Version:** 2.0.1.2
> **Target:** .NET Framework 4.8  
> **Last Updated:** June 2025

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Startup Sequence](#startup-sequence)
4. [Directory Structure](#directory-structure)
5. [Data Files](#data-files)
6. [Settings Configuration](#settings-configuration)
7. [Spawn System](#spawn-system)
8. [Logging System](#logging-system)
9. [Editor Integration](#editor-integration)
10. [Spawn Packs](#spawn-packs)
11. [Command System](#command-system)
12. [In-Game Spawn Editing System](#in-game-spawn-editing-system)
13. [Vendor System](#vendor-system)
14. [Event System](#event-system)

---

## Overview

UORespawnServer is a dynamic player-centric spawn system for ServUO. Instead of static world spawners, creatures spawn around players based on their location, terrain, region, and environmental conditions.

### Key Features

- **Player-Centric Spawning** - Mobs spawn near active players, not fixed locations
- **Multi-Layer Spawn Resolution** - Box → Region → Tile priority system
- **O(1) Spatial Lookups** - SpatialGridManager provides instant box lookups
- **On-Map Spawn Management** - SpawnQueryService relocates and trims spawn on the live map
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
| `UOR_Core` | Main orchestrator - centralized startup, events, state management |
| `UOR_Settings` | Configuration loading and runtime settings |
| `UOR_DIR` | Directory and file path management |
| `UOR_Utility` | Shared utility methods |

### Services (8 total)

| Service | Purpose |
|---------|---------|
| `ProcessService` | Main spawn processing and mob creation |
| `SpawnQueryService` | On-map spawn queries for relocation and trimming |
| `TrackService` | Placeholder for future tracking features |
| `ValidateService` | Spawn validation using ISpawner on-demand queries |
| `TimedService` | Time-based spawn updates (day/night) |
| `StatsService` | Spawn statistics collection |
| `VendorService` | Vendor NPC runtime operations (reset, time updates) |
| `ControlService` | In-game settings gump interface |

### Managers (5 total)

| Manager | Purpose |
|---------|---------|
| `SpawnManager` | Loads and manages all spawn data from binary files |
| `SpatialGridManager` | O(1) spatial grid for box spawn lookups |
| `GameManager` | Generates server data lists for Editor |
| `VendorManager` | Sign and hive location management |
| `LogManager` | Session log buffering, color-level tagging, and shutdown flush |

### Spawners (4 spawn logic + 2 ISpawner singletons)

| Spawner | Purpose |
|---------|---------|
| `BoxSpawner` | Geographic box-based spawns (highest priority) |
| `RegionSpawner` | Region-based spawns (medium priority) |
| `TileSpawner` | Terrain tile-based spawns (lowest priority) |
| `VendorSpawner` | Vendor NPC spawning at sign locations |

### ISpawner Singletons (NEW in 2.0.0.8+)

| ISpawner | Purpose |
|----------|---------|
| `UOR_MobSpawner` | Tracks all UORespawn mob spawn via `creature.Spawner` field |
| `UOR_VendorSpawner` | Tracks all UORespawn vendor spawn via `creature.Spawner` field |

The ISpawner pattern provides:
- **On-demand queries** - `GetAllSpawn()` finds all owned creatures instantly
- **Automatic cleanup** - `CleanupAll()` deletes all owned spawn
- **No tracking lists** - Game's existing Mobile collection handles persistence
- **Leak-proof** - Spawn keeps ISpawner reference even when recycled

---

## Startup Sequence

### ⚠️ Centralized ServerStarted Pattern (NEW in 2.0.0.8)

UORespawn uses a **centralized startup pattern** via `EventSink.ServerStarted`. This ensures all initialization happens AFTER `World.Load()` completes, when `World.Mobiles` is fully populated.

### Why ServerStarted?

ServUO startup order:
1. `World.Load()` - Deserializes all mobiles/items
2. `ScriptCompiler.Invoke("Initialize")` - Runs all `[CallPriority]` Initialize methods
3. `EventSink.ServerStarted` - Fires when server is fully ready

**Problem with Initialize():** `Mobile.Spawner` (ISpawner) is NOT serialized by ServUO. After `World.Load()`, all creatures have `Spawner = null` even though the spawner Items exist.

**Solution:** Use `ServerStarted` to reclaim spawner references AFTER world load, then initialize vendors and services.

### Startup Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                    UORespawn STARTUP SEQUENCE                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   PHASE 1: Initialize() - Called during script initialization        │
│   ══════════════════════════════════════════════════════════════     │
│   • Logo display                                                     │
│   • UOR_Utility setup                                                │
│   • GameManager.InitializeData() - Generate OUTPUT/ files            │
│   • SpawnManager.LoadSpawns() - Load INPUT/ binary data              │
│   • Subscribe to EventSink.ServerStarted                             │
│                                                                      │
│                         ↓ (Wait for World.Load)                      │
│                                                                      │
│   PHASE 2: OnServerStarted() - Called after server fully ready       │
│   ══════════════════════════════════════════════════════════════     │
│   1. ReclaimSpawners()     - Restore bc.Spawner references           │
│   2. CleanupMobSpawn()     - Delete mob spawn (fresh world)          │
│   3. InitializeVendors()   - Check existing or spawn new             │
│   4. InitializeServices()  - Create service instances                │
│   5. InitializeEvents()    - Subscribe to game events                │
│   6. StartTimers()         - Start service timers                    │
│   7. Enable system         - IsPaused = IsLocked                     │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Console Output on Startup

```
Respawn-[Started]
*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*
|-|-|-|-|-|-|-| UORespawn |-|-|-|-|-|-|-|
|-|-|-|-|-|-|-|   ~*~*~   |-|-|-|-|-|-|-|
*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*
Respawn-[Pre-Load Setup]
Respawn-[1/2]
Respawn-[2/2]
Respawn-[Waiting for ServerStarted...]
... (ServUO startup messages) ...
Respawn-[ServerStarted - Beginning Full Init]
RECLAIM-[Mobs: 0, Vendors: 125]
Respawn-[1/5]
CLEANUP-[0 Mobs Deleted - Fresh World Ready]
Respawn-[2/5]
VENDORS-[125 persisted from save]
Respawn-[3/5]
SERVICES-[Initialized]
Respawn-[4/5]
EVENTS-[Subscribed]
Respawn-[5/5]
TIMERS-[Initialized]
Respawn-[Sequence Complete]
FLAGS-[0 Deleted]
GATES-[0 Deleted]
STARTED - Running ...
```

### Key Startup Operations

| Step | Method | Purpose |
|------|--------|---------|
| 1 | `ReclaimSpawners()` | Restore `creature.Spawner` references from serial tracking |
| 2 | `CleanupMobSpawn()` | Delete all mob spawn (fresh world for players) |
| 3 | `InitializeVendors()` | Spawn vendors only if none exist from save |
| 4 | `InitializeServices()` | Create all 8 service instances |
| 5 | `InitializeEvents()` | Subscribe to Login, Logout, Death, Save, etc. |
| 6 | `StartTimers()` | Start Process, Validate, Timed timers |

### Why Vendors Persist, Mobs Don't

| Spawn Type | On Startup | Reason |
|------------|------------|--------|
| **Mobs** | Deleted | Fresh world experience for players |
| **Vendors** | Preserved | Town NPCs should persist across restarts |

Vendors are only respawned when:
- First server run (no existing vendors)
- Manual reset via `[uor` → Reset Vendors
- `ENABLE_VENDOR_SPAWN` toggled on after being off

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
    │   └── UOR_SpawnSettings.csv    # ⚠️ CSV format settings
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
    ├── COMMANDS/       # ⚠️ NEW: Server → Editor command queue
    │   ├── settings_edits.txt   # Settings change commands
    │   ├── box_edits.txt        # Box spawn commands
    │   ├── region_edits.txt     # Region spawn commands
    │   ├── tile_edits.txt       # Tile spawn commands
    │   └── vendor_edits.txt     # Vendor spawn commands
    │
    ├── STATS/          # Statistics data
    │
    └── SYS/            # System files
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
| `MAX_RECYCLE_TYPE` | 20 | Max spawn allowed per creature type |
| `MAX_RECYCLE_TOTAL` | *dynamic* | Calculated as `BestiaryCount × MAX_RECYCLE_TYPE` (can be overridden) |
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

### Settings Validation (NEW in 2.0.1.2)

All settings are automatically validated on load to prevent invalid values from causing issues. Invalid values are clamped to valid ranges and logged.

| Setting | Valid Range | Notes |
|---------|-------------|-------|
| `SCALE_MOD` | 0.1 - 3.0 | Scale modifier |
| `SEARCH_INTERVAL` | 50 - 2000 ms | Per-player search rate |
| `PROCESS_INTERVAL` | 50 - 2000 ms | Global processing rate |
| `VALIDATE_INTERVAL` | 1 - 60 sec | Must be >= 1 |
| `TIMED_INTERVAL` | 1 - 60 min | Must be >= 1 |
| `MAX_RECYCLE_TYPE` | 1 - 100 | Per-type spawn limit, must be >= 1 |
| `MAX_RECYCLE_TOTAL` | 1 - 100000 | Total spawn limit (if override set) |
| `MAX_SPAWN_CHECKS` | 1 - 10 | Spawn point search attempts |
| `MAX_QUEUE_SIZE` | 1 - 10 | Queue slots per player |
| `MAX_STAT_SIZE` | 100 - 10000 | Statistics buffer size |
| `MAX_SPAWN` | 5 - 75 | Area spawn limit |
| `MIN_RANGE` | 5 - 125 | Auto-clamped to <= MAX_RANGE |
| `MAX_RANGE` | 5 - 250 | Spawn distance limit |
| `MAX_CROWD` | 1 - 10 | Crowd multiplier |
| `CHANCE_*` | 0.0 - 1.0 | All chance values clamped to valid probability |

**Example Log Output:**
```
SETTINGS-[Clamped VALIDATE_INTERVAL to 1]
SETTINGS-[Clamped CHANCE_RARE to 1]
SETTINGS-[Clamped MIN_RANGE to MAX_RANGE (80)]
```

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
│ Version      : 2.0.0.9                                       │
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

## Command System

### ⚠️ NEW: Command-Based Edit Synchronization

The command system provides **unidirectional data flow** with a **command queue** for server → editor communication. Instead of both server and editor reading/writing the same settings file, edits are logged as commands.

### Why Commands?

| Old Approach (Problems) | New Approach (Commands) |
|------------------------|-------------------------|
| Both read/write same file | One-way data flow with command queue |
| Race conditions possible | Server logs commands, editor consumes |
| Hard to track what changed | Each change is an explicit command |
| Settings could conflict | Editor is source of truth, server proposes changes |

### Directory Structure

```
Data/
└── UORespawn/
    └── COMMANDS/           # ⚠️ NEW: Command queue folder
        ├── settings_edits.txt    # Settings change commands
        ├── box_edits.txt         # Box spawn edit commands
        ├── region_edits.txt      # Region spawn edit commands
        ├── tile_edits.txt        # Tile spawn edit commands
        └── vendor_edits.txt      # Vendor spawn edit commands
```

### Command File Format

Each line in a command file is a pipe-delimited command:

```
Action|Target|Section|Trigger|SpawnName|ExtraData
```

#### Field Definitions

| Field | Values | Description |
|-------|--------|-------------|
| `Action` | `Add`, `Remove`, `Update` | What to do |
| `Target` | `Settings`, `Box`, `Region`, `Tile`, `Vendor` | What type to modify |
| `Section` | `None`, `Common`, `Uncommon`, `Rare`, `Water`, `Weather`, `Timed` | Spawn list to modify |
| `Trigger` | `None`, `Weather`, `Timed` | Trigger condition |
| `SpawnName` | string | Creature name (spawn) or setting key (settings) |
| `ExtraData` | string | Setting value or location identifier |

### Command Examples

#### Settings Commands

```
# Update a setting value
Update|Settings|None|None|SEARCH_INTERVAL|125
Update|Settings|None|None|ENABLE_DEBUG|True
Update|Settings|None|None|CHANCE_RARE|0.05
```

#### Spawn Commands

```
# Add Orc to Box #5 on Felucca (MapId=0) Common list
Add|Box|Common|None|Orc|0,5

# Remove Dragon from Region "Britain" on Felucca Rare list
Remove|Region|Rare|None|Dragon|0,Britain

# Add RatmanArcher to Tile "grass" on Trammel (MapId=1) Uncommon list
Add|Tile|Uncommon|None|RatmanArcher|1,grass

# Add Blacksmith to Vendor at coordinates on Felucca
Add|Vendor|None|None|Blacksmith|0,1434,1699,0
```

### ExtraData Format by Target

| Target | ExtraData Format | Example |
|--------|------------------|---------|
| `Settings` | `{value}` | `125`, `True`, `0.05` |
| `Box` | `{MapId},{BoxId}` | `0,5` (Felucca, Box #5) |
| `Region` | `{MapId},{RegionName}` | `0,Britain` |
| `Tile` | `{MapId},{TileName}` | `1,grass` |
| `Vendor` | `{MapId},{X},{Y},{Z}` | `0,1434,1699,0` |

### Command Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                    COMMAND FLOW DIAGRAM                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   IN-GAME EDIT FLOW (Server → Editor)                                │
│   ════════════════════════════════════                               │
│                                                                      │
│   1. Admin opens Control Gump in game                                │
│   2. Adjusts settings (intervals, limits, toggles)                   │
│   3. Clicks "Save" button                                            │
│   4. ControlService.SaveSettings() is called                         │
│   5. Settings logged as commands to COMMANDS/settings_edits.txt      │
│   6. Server continues running with updated in-memory settings        │
│                                                                      │
│                         ↓                                            │
│                                                                      │
│   EDITOR CONSUMPTION (Editor processes server commands)              │
│   ═════════════════════════════════════════════════════              │
│                                                                      │
│   1. Editor launches                                                 │
│   2. Checks COMMANDS/ folder for edit files                          │
│   3. For each edit file found:                                       │
│      a. Read and parse commands                                      │
│      b. Apply changes to spawn pack data                             │
│      c. DELETE the consumed edit file                                │
│   4. Save updated spawn pack files                                   │
│   5. Push updated .bin/.csv files to server INPUT/ folder            │
│                                                                      │
│                         ↓                                            │
│                                                                      │
│   SERVER RESTART SAFETY                                              │
│   ════════════════════════                                           │
│                                                                      │
│   If server restarts BEFORE editor consumes commands:                │
│   1. Server starts → LoadSpawns()                                    │
│   2. Loads settings/spawn data from binary files                     │
│   3. Calls CheckAndApplyPendingCommands()                            │
│   4. Reads any remaining command files                               │
│   5. Applies commands to in-memory data                              │
│   6. Deletes consumed command files                                  │
│   7. Server runs with updated settings                               │
│                                                                      │
│   ⚠️ Commands persist until consumed by EITHER server OR editor!     │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Server-Side Components

| Component | File | Purpose |
|-----------|------|---------|
| `CommandTypes.cs` | `Enums/` | Defines `CommandAction`, `CommandTarget`, `SpawnSection`, `SpawnTrigger` |
| `EditCommand.cs` | `Models/` | Command model with serialization/parsing |
| `CommandManager.cs` | `Managers/` | Read/Write/Consume command files |
| `GameManager.cs` | `Managers/` | `CheckAndApplyPendingCommands()` on startup |
| `SpawnManager.cs` | `Managers/` | `ApplySpawnCommand()` for spawn edits |
| `ControlService.cs` | `Services/` | `SaveSettings()` logs commands |

### Fresh Install Behavior

When NO data files exist (fresh install):

| Scenario | Behavior |
|----------|----------|
| No settings file | Uses hardcoded defaults in `UOR_Settings` |
| No spawn binary files | Empty spawn dictionaries, logs warning |
| No command files | Skips command processing |
| Unknown spawn type (debug) | Uses `PlaceHolder` mob |
| Unknown spawn type (prod) | Uses `WanderingHealer` fallback |

### Editor Integration Requirements

**⚠️ EDITOR AI: READ THIS SECTION**

The Editor must implement command consumption:

#### On Launch

```
1. Check if COMMANDS/ folder exists
2. For each edit file (settings_edits.txt, box_edits.txt, etc.):
   a. Read file line by line
   b. Parse each line as: Action|Target|Section|Trigger|SpawnName|ExtraData
   c. Apply command to spawn pack data in memory
   d. DELETE the file after successful processing
3. Continue with normal Editor startup
```

#### Command Processing Pseudocode

```csharp
// For each command file
foreach (var file in Directory.GetFiles(COMMANDS_DIR, "*_edits.txt"))
{
    var lines = File.ReadAllLines(file);

    foreach (var line in lines)
    {
        // Skip comments and empty lines
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            continue;

        var parts = line.Split('|');
        // parts[0] = Action (Add/Remove/Update)
        // parts[1] = Target (Settings/Box/Region/Tile/Vendor)
        // parts[2] = Section (None/Common/Uncommon/Rare/Water/Weather/Timed)
        // parts[3] = Trigger (None/Weather/Timed)
        // parts[4] = SpawnName (creature name or setting key)
        // parts[5] = ExtraData (setting value or location identifier)

        ApplyCommand(parts);
    }

    // CRITICAL: Delete file after consumption
    File.Delete(file);
}
```

#### Settings Command Application

```csharp
// For settings commands (Target == "Settings")
// SpawnName = setting key, ExtraData = setting value
switch (command.SpawnName.ToUpper())
{
    case "SEARCH_INTERVAL":
        Settings.SearchInterval = int.Parse(command.ExtraData);
        break;
    case "ENABLE_DEBUG":
        Settings.EnableDebug = bool.Parse(command.ExtraData);
        break;
    // ... etc for all settings
}
```

#### Spawn Command Application

```csharp
// For spawn commands (Target == Box/Region/Tile)
// Parse ExtraData to find the entity, then modify the appropriate list

if (command.Action == "Add")
{
    GetSpawnList(entity, command.Section).Add(command.SpawnName);
}
else if (command.Action == "Remove")
{
    GetSpawnList(entity, command.Section).Remove(command.SpawnName);
}
```

### Important Notes for Editor AI

1. **Always delete command files after processing** - This prevents duplicate application
2. **Commands are append-only** - New commands append to existing file
3. **Process all commands before saving** - Apply all pending changes, then save updated packs
4. **Validate commands** - Skip invalid commands and log warnings
5. **Settings keys are case-insensitive** - `ENABLE_DEBUG` == `enable_debug`
6. **ExtraData parsing varies by target** - See ExtraData Format table above

### Version History (Commands)

| Version | Changes |
|---------|---------|
| 2.0.0.7 | Added COMMANDS/ folder and command file system |
| 2.0.0.7 | ControlService.SaveSettings() now logs commands |
| 2.0.0.7 | GameManager.CheckAndApplyPendingCommands() on startup |
| 2.0.0.7 | SpawnManager.ApplySpawnCommand() for runtime edits |

---

## Server Events

The system hooks into these ServUO events:

| Event | Action |
|-------|--------|
| `EventSink.ServerStarted` | **Primary init point** - Reclaim, cleanup, vendors, services, timers |
| `EventSink.TameCreature` | Log taming (ISpawner handles release) |
| `EventSink.CreatureDeath` | Log death (ISpawner handles release) |
| `EventSink.MobileDeleted` | ISpawner handles cleanup automatically |
| `EventSink.BeforeWorldSave` | Pause system during save |
| `EventSink.AfterWorldSave` | Resume system, save stats/vendors |
| `EventSink.Login` | Add player to respawner list |
| `EventSink.Logout` | Remove player from respawner list |
| `EventSink.Shutdown` | Flush logs, stop timers |
| `EventSink.Crashed` | Emergency log flush |

### Event Subscription Safety

Events are subscribed only once via `_EventsSubscribed` flag. The `UnsubscribeEvents()` method is called during `SHUTDOWN()` to prevent double handlers if the system is restarted.

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
| 2.0.1.2 | **NEW:** Settings validation with range clamping for all values |
| 2.0.1.2 | **FIX:** ValidateService IsCalling loop now processes each player once |
| 2.0.1.1 | **REFACTOR:** RecycleService replaced with SpawnQueryService (on-map relocation) |
| 2.0.1.1 | **NEW:** Dynamic `MAX_RECYCLE_TOTAL` calculation (`BestiaryCount × MAX_RECYCLE_TYPE`) |
| 2.0.1.1 | **FIX:** Spawn limits now only trim excess, never block new spawns |
| 2.0.1.1 | **PERF:** TrimExcess deletes furthest spawn first (minimizes player impact) |
| 2.0.1.0 | **REFACTOR:** Major code review and cleanup pass |
| 2.0.0.9 | **POLISH:** Code review and comment cleanup pass |
| 2.0.0.9 | **FIX:** Spawner name no longer shows on creature hover (debug mode only) |
| 2.0.0.9 | **FIX:** Queued location check now properly detects point inside rectangle |
| 2.0.0.9 | **FIX:** VendorService uses reverse iteration for safe deletion |
| 2.0.0.9 | **REFACTOR:** SpawnEditService/Gump converted from ArrayList to List<string> |
| 2.0.0.9 | **DOCS:** Added XML documentation to TimedService, RespawnerEntity, SpawnEntity, SpawnManager, and all Timer classes |
| 2.0.0.9 | **DOCS:** Fixed outdated TrackService reference in UOR_Spawner comment |
| 2.0.0.8 | **BREAKING:** Replaced tracking lists with ISpawner pattern |
| 2.0.0.8 | **NEW:** `UOR_MobSpawner` and `UOR_VendorSpawner` singletons |
| 2.0.0.8 | **NEW:** Centralized startup to `EventSink.ServerStarted` |
| 2.0.0.8 | **NEW:** `UOR_Core.OnServerStarted()` - single entry point after world load |
| 2.0.0.8 | **NEW:** Explicit startup ordering: Reclaim → Cleanup → Vendors → Services → Events → Timers |
| 2.0.0.8 | **FIX:** Vendors no longer recreated every restart (check after reclaim) |
| 2.0.0.8 | **FIX:** Mobile.Spawner properly reclaimed after world load |
| 2.0.0.8 | **SIMPLIFIED:** TrackService - startup logic moved to UOR_Core |
| 2.0.0.8 | **SIMPLIFIED:** VendorService - startup logic moved to UOR_Core |
| 2.0.0.8 | **NEW:** On-demand spawn queries via `creature.Spawner` field |
| 2.0.0.8 | **NEW:** `RespawnVendorsAtLocation()` for immediate vendor swap |
| 2.0.0.8 | **REMOVED:** `VENDOR_MARKER` (Home.Z=999) - Use ISpawner instead |
| 2.0.0.8 | **REMOVED:** `UOR_VendorSerials.bin` - ISpawner persists with Mobile |
| 2.0.0.8 | **REMOVED:** `_AllSpawns` tracking list - Query ISpawner on-demand |
| 2.0.0.8 | **REMOVED:** `SpawnedVendors` list on VendorEntity - Query on-demand |
| 2.0.0.8 | **FIX:** RecycleService maintains ISpawner leash (no Release) |
| 2.0.0.8 | VendorEditService.SaveChanges() applies changes and respawns |
| 2.0.0.7 | Settings changed from binary to CSV format |
| 2.0.0.7 | Logging unified through UOR_Utility.SendMsg |
| 2.0.0.7 | ControlGump layout fixes and improvements | 
| 2.0.0.7 | Added SpatialGridManager for O(1) lookups |
| 2.0.0.7 | Added Dictionary lookups for Region/Tile |
| 2.0.0.7 | **NEW:** In-game spawn editing (SpawnEditGump/Service) |
| 2.0.0.7 | **NEW:** Vendor spawn editing (VendorEditGump/Service) |
| 2.0.0.7 | **NEW:** Event subscription safety (prevents double handlers) |
| 2.0.0.7 | **FIX:** Vendor display names use actual sign text, not SignType enum |
| 2.0.0.7 | **FIX:** VendorManager.LoadAllHives() now counts generated hives |

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
| Logs edits → COMMANDS/ | Reads COMMANDS/ → Updates packs |
| Owns runtime state | Owns source of truth |
| Regenerates on startup | Refreshes on launch |

### Command System Summary

```
┌────────────────────────────────────────────────────────────────┐
│                    DATA FLOW SUMMARY                            │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│   OUTPUT/          One-way: Server → Editor (game data)         │
│   INPUT/           One-way: Editor → Server (spawn data)        │
│   COMMANDS/        One-way: Server → Editor (edit requests)     │
│                                                                 │
│   Server is CONSUMER of INPUT/                                  │
│   Server is PRODUCER of OUTPUT/ and COMMANDS/                   │
│                                                                 │
│   Editor is CONSUMER of OUTPUT/ and COMMANDS/                   │
│   Editor is PRODUCER of INPUT/                                  │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

---

## Contact

For issues or questions about the server-side code, check `UOR_DebugLog.txt` first, then refer to this documentation.

---

## In-Game Spawn Editing System

### Overview

The server includes an **in-game spawn editing system** that allows admins to view and edit spawn data directly in the game client. This system generates commands that the Editor consumes to update spawn pack files.

### Accessing the Spawn Editor

1. Use the command `[uor` to open the Control Gump
2. Click the "Edit Spawn" button
3. Target any location (land, static, or item)
4. Spawn editor gump(s) will open for matching spawn data

### Target Resolution

When targeting a location, the system checks for spawn data in this priority:

| Target Type | What Opens |
|-------------|-----------|
| **Land Tile** | Box → Region → Tile spawn editors |
| **Static (Beehive)** | Vendor editor for beekeeper spawn |
| **Item (Sign)** | Vendor editor for shop vendor spawn |

### SpawnEditGump (Box/Region/Tile)

The spawn edit gump displays all 6 spawn sections in tabs:

| Tab | Section | Description |
|-----|---------|-------------|
| 1 | Water | Spawns when player is on water |
| 2 | Weather | Spawns during weather events |
| 3 | Timed | Spawns at night (timed events) |
| 4 | Common | Always spawns (100% chance) |
| 5 | Uncommon | Rare spawns (~10% chance) |
| 6 | Rare | Very rare spawns (~1% chance) |

#### SpawnEditGump Features

- **View spawn lists** - See all creatures in each section
- **Add spawns** - Type creature name, click Add
- **Remove spawns** - Click X next to creature name
- **Tab navigation** - Switch between 6 spawn sections
- **Auto-command logging** - All changes logged to COMMANDS/ folder

### VendorEditGump

The vendor edit gump is a simplified single-list editor for vendor spawns:

| Feature | Description |
|---------|-------------|
| **Location display** | Shows actual sign name from world (e.g., "Bank Of Skara Brae") |
| **Vendor list** | Single list of vendor types to spawn |
| **Add vendors** | Type vendor name (e.g., "Banker"), click Add |
| **Remove vendors** | Click X next to vendor name |

### Services Architecture

| Service | File | Purpose |
|---------|------|---------|
| `SpawnEditService` | `Services/SpawnEditService.cs` | Handles Box/Region/Tile editing |
| `VendorEditService` | `Services/VendorEditService.cs` | Handles vendor spawn editing |
| `SpawnEditGump` | `Gumps/SpawnEditGump.cs` | 6-tab spawn editor UI |
| `VendorEditGump` | `Gumps/VendorEditGump.cs` | Single-list vendor editor UI |

### Command Generation

When edits are made via gumps, commands are written to:

| Edit Type | Command File |
|-----------|-------------|
| Box spawn | `COMMANDS/box_edits.txt` |
| Region spawn | `COMMANDS/region_edits.txt` |
| Tile spawn | `COMMANDS/tile_edits.txt` |
| Vendor spawn | `COMMANDS/vendor_edits.txt` |

---

## Vendor System

### Overview

The vendor system manages NPC vendor spawning at shop signs and beehive locations. Vendors are tracked via the **ISpawner pattern** using `UOR_VendorSpawner`.

### Key Differences from Regular Spawns

| Regular Spawns | Vendor Spawns |
|----------------|---------------|
| Spawn/despawn dynamically | Spawn once, persist |
| Tracked by `UOR_MobSpawner` | Tracked by `UOR_VendorSpawner` |
| Recycled when far from players | Only deleted on system off/reset |
| No special persistence | ISpawner field persists with Mobile |

### ISpawner Pattern (NEW in 2.0.0.8+)

All spawn tracking now uses the ServUO `ISpawner` interface:

```csharp
// Claiming a vendor (in VendorSpawner.cs)
UOR_VendorSpawner.Instance.Claim(vendor, location);

// On-demand query for all vendors
var allVendors = UOR_VendorSpawner.GetAllSpawn();

// Cleanup all vendors
int deleted = UOR_VendorSpawner.CleanupAll();
```

**Benefits:**
- No tracking lists needed - query `World.Mobiles` on-demand
- No serial persistence files - ISpawner field saves with Mobile
- No marker systems - check `creature.Spawner == UOR_VendorSpawner.Instance`
- Immediate vendor swap on edit - `RespawnVendorsAtLocation()` method

### Vendor Entity (Config Only)

Each `VendorEntity` is now **config-only** - no runtime serial tracking:

```csharp
internal class VendorEntity
{
    // Config data from editor
    internal List<string> VendorList { get; }  // Types to spawn
    internal Point3D Location { get; }          // Spawn point
    internal bool IsSign { get; }               // Sign vs beehive

    // NO runtime tracking - ISpawner handles it
}
```

### Vendor Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│                    VENDOR LIFECYCLE (ISpawner)                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   SERVER STARTUP                                                 │
│   ══════════════                                                 │
│   1. SpawnManager.LoadVendorSpawnData()                          │
│      → Loads VendorEntity definitions from UOR_VendorSpawn.bin   │
│      → Each entity has VendorList (type names to spawn)          │
│                                                                  │
│   2. VendorService.InitializeSpawn()                             │
│      → Checks UOR_VendorSpawner.GetCount() for existing vendors  │
│      → If vendors exist (ISpawner tracked), skip spawning        │
│      → If no vendors, spawn from config and Claim() via ISpawner │
│                                                                  │
│   WORLD SAVE                                                     │
│   ══════════════                                                 │
│   1. VendorService.Save()                                        │
│      → Just logs count - ISpawner field saves with Mobile        │
│      → No separate serial persistence needed!                    │
│                                                                  │
│   SYSTEM OFF / RESET                                             │
│   ══════════════════                                             │
│   1. VendorService.DeleteAllVendors()                            │
│      → UOR_VendorSpawner.CleanupAll() - deletes all owned spawn  │
│      → Single ISpawner query finds and deletes all vendors       │
│                                                                  │
│   IN-GAME VENDOR EDIT                                            │
│   ═══════════════════                                            │
│   1. VendorEditService.SaveChanges()                             │
│      → Applies changes to in-memory VendorEntity                 │
│      → Calls RespawnVendorsAtLocation() for immediate swap       │
│      → ISpawner finds/deletes old vendors, spawns new ones       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### ISpawner Queries (Replaces Markers/Tracking Lists)

The ISpawner pattern eliminates the need for markers and tracking lists:

```csharp
// Find all UORespawn vendors in the world
var allVendors = UOR_VendorSpawner.GetAllSpawn();

// Find vendors near a specific location
foreach (var vendor in allVendors)
{
    if (vendor.Map == map && vendor.GetDistanceToSqrt(location) <= range)
    {
        // This is a vendor at this location
    }
}

// Delete all vendors
int deleted = UOR_VendorSpawner.CleanupAll();
```

**No longer needed:**
- ~~`VENDOR_MARKER` (Home.Z=999)~~ - Check `creature.Spawner` instead
- ~~`UOR_VendorSerials.bin`~~ - ISpawner field persists with Mobile
- ~~`SpawnedVendors` list on VendorEntity~~ - Query on-demand

### VendorSpawn.bin Format (Editor Creates)

```
int32   FileVersion
string  VersionString
int32   MapCount
[per map]:
  int32   MapId
  string  MapName
  int32   VendorCount
  [per vendor]:
    bool    IsSign          // true = shop sign, false = beehive
    int32   SignType        // SignType enum value
    int32   SignFacing      // SignFacing enum (North/West)
    int32   X
    int32   Y
    int32   Z
    int32   VendorListCount
    [per vendor type]:
      string  VendorTypeName  // e.g., "Banker", "Blacksmith"
```

### Location Coordinate Systems

**⚠️ IMPORTANT:** `VendorEntity.Location` stores the **inside location** (spawn point), not the original sign/hive position!

| Source | Location Stored | Purpose |
|--------|-----------------|---------|
| Sign world position | Original sign item location | Where user clicks |
| VendorEntity.Location | Inside location (offset) | Where vendors spawn |
| Editor creates | Original sign position | Input data |
| Server transforms | Inside location | Runtime storage |

#### Offset Calculations

**Signs (IsSign = true):**
```csharp
// North facing: Y - 2
insideLocation = new Point3D(signX, signY - 2, signZ);

// West facing: X - 2
insideLocation = new Point3D(signX - 2, signY, signZ);
```

**Beehives (IsSign = false):**
```csharp
// Always: X + 1, Y + 1
insideLocation = new Point3D(hiveX + 1, hiveY + 1, hiveZ);
```

#### Reverse Offset (For Editor Lookup)

When targeting a sign to edit, the server reverses the offset:

```csharp
// Sign (North): Y + 2
originalLocation = new Point3D(entityX, entityY + 2, entityZ);

// Sign (West): X + 2
originalLocation = new Point3D(entityX + 2, entityY, entityZ);

// Beehive: X - 1, Y - 1
originalLocation = new Point3D(entityX - 1, entityY - 1, entityZ);
```

### Vendor Display Names

The in-game vendor editor shows the **actual sign name** from the targeted item, not the stored `SignType` enum:

```csharp
// When user targets a sign:
string signName = baseSign.Name ?? baseSign.ItemData.Name;
// Shows: "Bank Of Skara Brae (North)"

// NOT the SignType enum:
// Would show: "Library (North)" ← WRONG if binary data has wrong enum
```

### Vendor Command Format

**File:** `COMMANDS/vendor_edits.txt`

```
# Add a vendor type to a location
Add|Vendor|None|None|Blacksmith|0,1434,1699,0

# Remove a vendor type from a location
Remove|Vendor|None|None|Blacksmith|0,1434,1699,0

# ExtraData format: MapId,X,Y,Z (original sign/hive position)
```

### Editor Requirements for Vendors

#### Reading VendorSpawn.bin

```csharp
// Read vendor entity
bool isSign = reader.ReadBoolean();
SignType signType = (SignType)reader.ReadInt32();
SignFacing signFacing = (SignFacing)reader.ReadInt32();
int x = reader.ReadInt32();
int y = reader.ReadInt32();
int z = reader.ReadInt32();

// Read vendor type list
int vendorCount = reader.ReadInt32();
for (int i = 0; i < vendorCount; i++)
{
    string vendorName = reader.ReadString();
    // Add to entity's vendor list
}
```

#### Writing VendorSpawn.bin

```csharp
// Write vendor entity
writer.Write(entity.IsSign);
writer.Write((int)entity.SignType);
writer.Write((int)entity.SignFacing);
writer.Write(entity.Location.X);  // Original sign/hive position!
writer.Write(entity.Location.Y);
writer.Write(entity.Location.Z);

// Write vendor type list
writer.Write(entity.VendorList.Count);
foreach (var vendorName in entity.VendorList)
{
    writer.Write(vendorName);
}
```

### Beehive Vendors

Beehives are a special case of vendor locations:

| Property | Sign | Beehive |
|----------|------|---------|
| `IsSign` | `true` | `false` |
| `SignType` | Varies | `MetalPost` (ignored) |
| `SignFacing` | `North`/`West` | `North` (ignored) |
| Offset | -2 on facing axis | +1 on X and Y |
| Typical vendors | Shop-specific | `Beekeeper` |

### VendorManager Data Files

These files contain **raw location data** for the Editor to populate its vendor location picker:

| File | Format | Description |
|------|--------|-------------|
| `UOR_SignData.txt` | `MapId:SignType:Facing:X:Y:Z` | All shop signs |
| `UOR_HiveData.txt` | `MapId:X:Y:Z` | All beehive locations |

**⚠️ These are OUTPUT files (server generates, editor reads).** They do NOT contain spawn lists - just locations. The Editor uses these to show available vendor locations, then creates `VendorSpawn.bin` with the spawn lists.

---

## Event System

### Event Subscription Safety

The system prevents double event subscription when toggling power:

```csharp
private static bool _EventsSubscribed = false;

private static void InitializeEvents()
{
    if (_EventsSubscribed) return; // Prevent double subscription

    EventSink.TameCreature += EventSink_TameCreature;
    // ... other events

    _EventsSubscribed = true;
}

private static void UnsubscribeEvents()
{
    if (!_EventsSubscribed) return;

    EventSink.TameCreature -= EventSink_TameCreature;
    // ... other events

    _EventsSubscribed = false;
}
```

### SHUTDOWN Cleanup

When the system is turned off via Control Gump:

```csharp
internal static void SHUTDOWN()
{
    IsLocked = true;

    UnsubscribeEvents();                // Prevent double subscription on restart
    _VendorService.DeleteAllVendors();  // Clean up all vendor spawn via ISpawner
    _RecycleService.ClearRecycled();    // Clear recycled pool

    int deleted = UOR_Utility.ClearAllSpawns();  // Clean up all mob spawn via ISpawner
}
```

**ISpawner-based cleanup:**
- `DeleteAllVendors()` → Uses `UOR_VendorSpawner.CleanupAll()` internally
- `ClearAllSpawns()` → Uses `UOR_MobSpawner.CleanupAll()` internally
- No tracking lists to save or reset

---
