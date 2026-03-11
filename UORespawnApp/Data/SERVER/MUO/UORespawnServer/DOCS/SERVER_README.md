# UORespawnServer — Server Documentation

> **Version:** 2.0.1.4
> **Platform:** ModernUO
> **Runtime:** C# 14 / .NET 10
> **Last Updated:** March 10, 2026

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Startup Sequence](#startup-sequence)
4. [Directory Structure](#directory-structure)
5. [Data Files](#data-files)
6. [Settings Configuration](#settings-configuration)
7. [Spawn System](#spawn-system)
8. [Logging System](#logging-system)
9. [Editor Integration](#editor-integration)
10. [Command System](#command-system)
11. [In-Game Spawn Editing](#in-game-spawn-editing)
12. [Vendor System](#vendor-system)
13. [Custom Mobiles & Items](#custom-mobiles--items)
14. [Event System](#event-system)
15. [Runtime Power Toggle](#runtime-power-toggle)
16. [Troubleshooting](#troubleshooting)
17. [Changelog](#changelog)
18. [Contact & Links](#contact--links)

---

## Overview

UORespawnServer is a dynamic, player-centric spawn system for ModernUO. Instead of static world spawners, creatures spawn around active players based on their location, terrain, region, time of day, and weather conditions.

### Key Features

- **Player-Centric Spawning** — Mobs spawn near active players, not at fixed points
- **Multi-Layer Spawn Resolution** — Box → Region → Tile priority cascade
- **O(1) Spatial Lookups** — `SpatialGridManager` provides instant geographic box lookups
- **On-Map Spawn Relocation** — `SpawnQueryService` relocates and trims live spawn
- **Weather & Time Spawns** — Conditional spawns based on active weather and day/night cycle
- **Vendor System** — Dynamic NPC vendor spawning at shop sign and beehive locations
- **ISpawner Singleton Pattern** — All spawn tracked via `creature.Spawner` field; no tracking lists
- **Unified Logging** — `UOR_Utility.SendMsg` drives all console and session log output
- **Server ↔ Editor Sync** — Server generates data files on startup; Editor reads and writes them
- **Runtime Power Toggle** — System can be turned on or off live via the Control Gump

### Server ↔ Editor Data Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                        AUTO-SYNC DATA FLOW                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   SERVER (Auto-Generates on Every Startup)                          │
│   ├── Scans all BaseCreature types  → UOR_BestiaryList.txt          │
│   ├── Scans all Map regions         → UOR_RegionList.txt            │
│   ├── Scans all terrain tiles       → UOR_TileList.txt              │
│   ├── Scans all Maps                → UOR_MapList.txt               │
│   ├── Scans all ISpawner items      → UOR_SpawnerList.txt           │
│   ├── Scans all Sign items          → UOR_SignData.txt              │
│   └── Scans beehive statics         → UOR_HiveData.txt             │
│                              ↓                                      │
│                         OUTPUT/ folder                              │
│                              ↓                                      │
│   EDITOR (Reads on Launch)                                          │
│   ├── Populates creature dropdown (bestiary)                        │
│   ├── Populates region list per map                                 │
│   ├── Populates tile list                                           │
│   ├── Populates map selector                                        │
│   ├── Shows existing spawner locations                              │
│   └── Shows vendor sign/hive locations                              │
│                              ↓                                      │
│   User edits spawn data in Editor                                   │
│                              ↓                                      │
│   EDITOR (Saves)                                                    │
│   └── Writes spawn definitions → INPUT/*.bin files                  │
│                              ↓                                      │
│   SERVER (Loads on Startup or Command Apply)                        │
│   └── Reads INPUT/*.bin → Active spawn system                       │
│                                                                     │
│                        [UOR_Settings.csv]                           │
│                   Two-Way Reading and Writing                       │
│        Server reads on startup ← → Editor reads on launch          │
│        Control Gump saves commands → Editor consumes changes        │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

**Results:**
- Add a new creature class → Restart server → It appears in the Editor bestiary automatically
- Add a new region to `regions.xml` → Restart server → It appears in the Editor region list
- No manual data file maintenance required

---

## Architecture

### Core Files

| File | Purpose |
|------|---------|
| `UOR_Core` | Central orchestrator — startup, events, state, public API |
| `UOR_Settings` | All configuration values and CSV loading |
| `UOR_DIR` | All file and directory paths in one place |
| `UOR_Utility` | Shared helper methods (tile, region, weather, spawn creation) |

### Services (7)

Instantiated in `InitializeServices()` after world load.

| Service | Purpose |
|---------|---------|
| `ProcessService` | Spawns or relocates mobs from the per-player queue |
| `SpawnQueryService` | On-map queries for relocation candidates; single-pass type validation and trimming via `ValidateAndTrim()` |
| `ValidateService` | Periodically validates existing spawn, removes invalid entries |
| `TimedService` | Triggers day/night spawn updates and vendor night cycle |
| `StatsService` | Collects and saves per-player spawn statistics |
| `VendorService` | Vendor runtime operations (reset, night toggle, delete all) |
| `ControlService` | Opens and manages the in-game Control Gump |

UI-only services (not in `InitializeServices`, instantiated on demand):

| Service | Purpose |
|---------|---------|
| `SpawnEditService` | Business logic for the in-game spawn list editor |
| `VendorEditService` | Business logic for the in-game vendor list editor |

### Managers (7)

Static classes; most initialize during `GameManager.InitializeData()` or `SpawnManager.LoadSpawns()`.

| Manager | Purpose |
|---------|---------|
| `GameManager` | Generates OUTPUT/ data files for the Editor on every startup |
| `SpawnManager` | Loads and holds all spawn data from INPUT/ binary files |
| `SpatialGridManager` | O(1) spatial grid for geographic box spawn lookups |
| `VendorManager` | Scans and caches sign and beehive locations from the world |
| `CommandManager` | Reads, writes, and consumes spawn edit command files |
| `XMLManager` | Processes native spawner commands from `UOR_XmlCommands.txt` |
| `LogManager` | Buffers session log entries; flushes to disk on shutdown |

### Spawners

| Class | Purpose |
|-------|---------|
| `UOR_Spawner` | Abstract base — implements `ISpawner`, persists spawned serials, reclaims on load |
| `UOR_MobSpawner` | Singleton ISpawner for all UORespawn mob spawn |
| `UOR_VendorSpawner` | Singleton ISpawner for all UORespawn vendor spawn |
| `BoxSpawner` | Geographic rectangle-based spawns (highest priority) |
| `RegionSpawner` | Region-based spawns (medium priority) |
| `TileSpawner` | Terrain tile-based spawns (lowest priority) |
| `VendorSpawner` | Spawns vendor NPCs at sign and beehive locations |

### Timers (4)

| Timer | Owner | Interval | Purpose |
|-------|-------|----------|---------|
| `SearchTimer` | Per-player (`RespawnerEntity`) | 125 ms | Finds and queues valid spawn locations per player |
| `ProcessTimer` | Global (`ProcessService`) | 250 ms | Processes queued locations — spawns or relocates mobs |
| `ValidateTimer` | Global (`ValidateService`) | 5 sec | Validates existing spawn, removes dead references |
| `TimedTimer` | Global (`TimedService`) | 1 min | Checks day/night transition, triggers timed spawn updates |

### Entities & Models

| Class | Purpose |
|-------|---------|
| `RespawnerEntity` | Tracks one logged-in player — holds the search queue and `SearchTimer` |
| `SpawnEntity` | A single queued spawn location — resolved tile, region, weather, frequency, and mob name |
| `BoxEntity` | A geographic spawn rectangle with 6 spawn lists |
| `RegionEntity` | A named region's spawn configuration |
| `TileEntity` | A terrain tile name's spawn configuration |
| `VendorEntity` | A sign or beehive location's vendor type list |
| `LocationEntity` | A lightweight player location snapshot used during search |
| `StatEntity` | One spawn statistics record |
| `EditCommand` | A parsed spawn or settings edit command |

---

## Startup Sequence

### Why ServerStarted?

ModernUO's startup order:

```
1. World.Load()                         — Deserializes all Mobiles and Items
2. ScriptCompiler.Invoke("Initialize")  — Calls all public static Initialize() methods
3. EventSink.ServerStarted              — Fires when server is fully ready
```

`Mobile.Spawner` (the `ISpawner` field) is **not serialized** by ModernUO. After `World.Load()`, all creatures have `Spawner = null` even though the `UOR_Spawner` items exist and hold the serial lists. `UOR_Spawner` saves its own serial list via `[SerializableField]` and reclaims ownership in `OnServerStarted()`.

### Startup Phases

```
┌─────────────────────────────────────────────────────────────────────┐
│                    UORespawn STARTUP SEQUENCE                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   Initialize() — ScriptCompiler.Invoke("Initialize")                 │
│   ══════════════════════════════════════════════════                 │
│   • Display logo                                                     │
│   • UOR_Utility.InitializeUtility()                                  │
│   • GameManager.InitializeData()    — Write OUTPUT/ files            │
│   • SpawnManager.LoadSpawns()       — Load INPUT/ binary data        │
│   • Subscribe EventSink.ServerStarted                                │
│                                                                      │
│                  ↓ (World.Load completes)                            │
│                                                                      │
│   OnServerStarted() — World.Mobiles fully populated                  │
│   ══════════════════════════════════════════════════                 │
│   PHASE 1: ReclaimSpawners()        — Restore Mobile.Spawner refs    │
│   PHASE 2: CleanupMobSpawn()        — Delete mobs from last session  │
│   PHASE 3: InitializeVendors()      — Persist or spawn vendors       │
│   PHASE 4: XMLManager.ProcessCommands() — Apply spawner commands     │
│   PHASE 5: InitializeServices()     — Create 7 service instances     │
│   PHASE 6: InitializeEvents()       — Subscribe to game events       │
│   PHASE 7: StartTimers()            — Start all 3 global timers      │
│   • IsPaused = IsLocked             — Enable system                  │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Console Output on a Clean Start

```
Respawn-[Started]
*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*
|-|-|-|-|-|-|-| UORespawn |-|-|-|-|-|-|-|
...
Respawn-[Pre-Load Setup]
Respawn-[1/2]
Respawn-[2/2]
Respawn-[Waiting for ServerStarted...]
... (ModernUO startup output) ...
Respawn-[ServerStarted - Beginning Full Init]
VENDORS-[125 Reclaimed]
Fresh World Ready!
VENDORS-[125 persisted from save]
SERVICES-[Initialized]
EVENTS-[Subscribed]
TIMERS-[Initialized]
Respawn-[Sequence Complete]
STARTED - Running ...
```

### Why Mobs Reset But Vendors Don't

| Spawn Type | On Startup | Reason |
|------------|------------|--------|
| **Mobs** | Deleted | Players get a fresh, balanced world each session |
| **Vendors** | Preserved | Town NPCs should feel persistent to players |

Vendors are re-spawned only when:
- First server run (no existing vendors in world)
- `ENABLE_VENDOR_SPAWN` toggled on after being off
- Manual reset via the Control Gump → Reset Vendors

---

## Directory Structure

```
Data/
└── UORespawn/
    ├── INPUT/              # Editor → Server (spawn definitions)
    │   ├── UOR_BoxSpawn.bin
    │   ├── UOR_RegionSpawn.bin
    │   ├── UOR_TileSpawn.bin
    │   ├── UOR_VendorSpawn.bin
    │   ├── UOR_SpawnSettings.csv
    │   └── UOR_XmlCommands.txt     # Native spawner commands
    │
    ├── OUTPUT/             # Server → Editor (auto-generated game data)
    │   ├── UOR_MapList.txt
    │   ├── UOR_BestiaryList.txt
    │   ├── UOR_RegionList.txt
    │   ├── UOR_TileList.txt
    │   ├── UOR_SpawnerList.txt
    │   ├── UOR_VendorList.txt
    │   ├── UOR_SignData.txt
    │   └── UOR_HiveData.txt
    │
    ├── COMMANDS/           # Server ↔ Editor edit command queue
    │   ├── settings_edits.txt
    │   ├── box_edits.txt
    │   ├── region_edits.txt
    │   ├── tile_edits.txt
    │   └── vendor_edits.txt
    │
    ├── STATS/              # Per-session spawn statistics
    │
    └── SYS/                # System files
        └── UOR_DebugLog.txt
```

---

## Data Files

### Binary Files (Editor Creates → Server Loads)

All binary files share the same header structure:

```
int32   FileVersion
string  VersionString
int32   MapCount
[per map]:
  int32   MapId
  string  MapName
  int32   EntityCount
  [per entity]: ... entity-specific fields
```

| File | Description |
|------|-------------|
| `UOR_BoxSpawn.bin` | Geographic spawn rectangles with 6 spawn lists each |
| `UOR_RegionSpawn.bin` | Region-keyed spawn definitions |
| `UOR_TileSpawn.bin` | Tile-name-keyed spawn definitions |
| `UOR_VendorSpawn.bin` | Vendor type lists per sign/hive location |

### Text Files (Server Auto-Generates → Editor Reads)

> **These files are overwritten on every server startup.**

| File | Source | Editor Uses |
|------|--------|-------------|
| `UOR_MapList.txt` | `Map.Maps[]` | Map dropdown |
| `UOR_BestiaryList.txt` | All `BaseCreature` types via reflection | Creature selector |
| `UOR_RegionList.txt` | `Region.Regions` | Region selector per map |
| `UOR_TileList.txt` | `TileData.LandTable` | Terrain type selector |
| `UOR_SpawnerList.txt` | All `ISpawner` items in world | Reference view |
| `UOR_SignData.txt` | All `Sign` items in world | Vendor location picker |
| `UOR_HiveData.txt` | Beehive statics | Beekeeper location picker |

### XML Spawner Command File

`INPUT/UOR_XmlCommands.txt` is processed by `XMLManager` during Phase 4 of `OnServerStarted()`. Each line is a pipe-delimited command:

```
DELETE|<serial>
ADD|<type>|<x>|<y>|<z>|<mapId>|...
EDIT|<serial>|<field>|<value>
```

Processed commands are cleared from the file after application.

---

## Settings Configuration

**File:** `Data/UORespawn/INPUT/UOR_SpawnSettings.csv`

Format: `SettingName,Value` — lines starting with `#` are comments. Keys are case-insensitive.

### Full Settings Reference

```csv
# Scale Modifier
SCALE_MOD,1.0

# System Intervals
SEARCH_INTERVAL,125        # ms  — per-player location search rate
PROCESS_INTERVAL,250       # ms  — global spawn queue processing rate
VALIDATE_INTERVAL,5        # sec — existing spawn validation rate
TIMED_INTERVAL,1           # min — day/night transition check rate

# System Limits
MAX_RECYCLE_TYPE,20        # per-type spawn cap (relocation and trim limit)
MAX_SPAWN_CHECKS,3         # max attempts to find a valid spawn point
MAX_QUEUE_SIZE,5           # max locations queued per player
MAX_STAT_SIZE,10000        # max stat entries before rollover

# Spawn Limits (scaled by SCALE_MOD when ENABLE_SCALE_SPAWN=True)
MAX_SPAWN,25               # max mobs within MAX_RANGE of any player
MIN_RANGE,30               # minimum spawn distance from player
MAX_RANGE,80               # maximum spawn distance from player
MAX_CROWD,3                # crowd multiplier

# Spawn Chances (0.0–1.0)
CHANCE_WATER,0.05
CHANCE_WEATHER,0.01
CHANCE_TIMED,0.01
CHANCE_COMMON,1.0
CHANCE_UNCOMMON,0.1
CHANCE_RARE,0.01

# Feature Toggles
ENABLE_SCALE_SPAWN,False   # apply SCALE_MOD to spawn limits
ENABLE_RIFT_SPAWN,False    # enable rift wisp event spawns
ENABLE_TOWN_SPAWN,True     # allow spawns in town regions
ENABLE_GRAVE_SPAWN,True    # enable grave effect spawns

# Vendor Toggles
ENABLE_VENDOR_SPAWN,False  # enable vendor NPC system
ENABLE_VENDOR_NIGHT,False  # apply night schedule to vendors
ENABLE_VENDOR_EXTRA,False  # spawn extra TownNPCs alongside vendors

# Other Toggles
ENABLE_SPAWN_EFFECTS,True  # visual spawn effect NPCs
ENABLE_DEBUG,False         # verbose console + log output
```

### Settings Validation

All settings are validated on load. Invalid values are clamped and logged:

```
SETTINGS-[Clamped VALIDATE_INTERVAL to 1]
SETTINGS-[Clamped MIN_RANGE to MAX_RANGE (80)]
```

| Setting | Valid Range |
|---------|-------------|
| `SCALE_MOD` | 0.1 – 3.0 |
| `SEARCH_INTERVAL` | 50 – 2000 ms |
| `PROCESS_INTERVAL` | 50 – 2000 ms |
| `VALIDATE_INTERVAL` | 1 – 60 sec |
| `TIMED_INTERVAL` | 1 – 60 min |
| `MAX_RECYCLE_TYPE` | 1 – 100 |
| `MAX_SPAWN_CHECKS` | 1 – 10 |
| `MAX_QUEUE_SIZE` | 1 – 10 |
| `MAX_STAT_SIZE` | 100 – 10000 |
| `MAX_SPAWN` | 5 – 75 |
| `MIN_RANGE` | 5 – `MAX_RANGE` |
| `MAX_RANGE` | 5 – 250 |
| `MAX_CROWD` | 1 – 10 |
| `CHANCE_*` | 0.0 – 1.0 |

---

## Spawn System

### Spawn Resolution Priority

When resolving what to spawn at a player's location, the system checks in this order:

1. **BoxSpawner** — `SpatialGridManager` provides O(1) lookup into geographic rectangles
2. **RegionSpawner** — Dictionary lookup by `Region` handle
3. **TileSpawner** — Dictionary lookup by tile name string

For dungeon and town regions, `RegionSpawner` is checked first (skipping box lookup).

### Spawn Categories (6 per entity)

Each Box, Region, and Tile entity contains 6 spawn lists:

| List | Trigger Condition | Chance Setting |
|------|-------------------|----------------|
| `WaterList` | Player standing on a water tile | `CHANCE_WATER` |
| `WeatherList` | Active weather in the spawn box | `CHANCE_WEATHER` |
| `TimedList` | Night time in-game | `CHANCE_TIMED` |
| `CommonList` | Default (no special condition) | `CHANCE_COMMON` |
| `UncommonList` | Random dice roll | `CHANCE_UNCOMMON` |
| `RareList` | Random dice roll | `CHANCE_RARE` |

### Spawn Flow

```
Player connects
    ↓
RespawnerEntity created → SearchTimer starts (125ms per player)
    ↓
SearchTimer: find valid location → push SpawnEntity to queue
    ↓
ProcessTimer (250ms global): pop SpawnEntity → check MAX_SPAWN
    ↓
SpawnEntity resolves: tile → region → time → weather → frequency → mob name
    ↓
UOR_Core.GetRelocatable(): find existing mob to relocate, or create new
    ↓
Mob placed at location — creature.Spawner = UOR_MobSpawner.Instance
    ↓
ValidateTimer (5s): scan and remove invalid spawn entries
```

### ISpawner Pattern

`UOR_MobSpawner` and `UOR_VendorSpawner` are both singletons that extend `UOR_Spawner`, which:

- Saves a `List<uint>` of spawned serials via `[SerializableField]`; a parallel `HashSet<uint>` provides O(1) duplicate checks in `Claim()` and is rebuilt from the list on every load
- Reclaims all `Mobile.Spawner` references on `OnServerStarted()` → `ReclaimAll()`
- Provides `GetAllSpawn()` for on-demand queries against `World.Mobiles`
- Provides `CountCanSwim()` — allocation-free swimmer count; exposed as `UOR_MobSpawner.CountSwimmers()`
- Provides `CleanupAll()` for complete teardown

Benefits:
- No external tracking lists to maintain
- No serial persistence files beyond the spawner item itself
- `creature.Spawner == UOR_MobSpawner.Instance` is the single ownership check

---

## Logging System

### Overview

All output goes through `UOR_Utility.SendMsg(ConsoleColor, string)`. Color determines log level and console visibility.

### Message Levels

| Colors | Level | Console | Log File |
|--------|-------|---------|----------|
| `Magenta`, `DarkMagenta`, `Blue`, `DarkBlue`, `Green`, `DarkGreen`, `Cyan`, `DarkCyan` | INFO | Always | Always |
| `Yellow`, `DarkYellow` | DEBG | Only if `ENABLE_DEBUG=True` | Only if `ENABLE_DEBUG=True` |
| `Red`, `DarkRed` | ERROR | Always | Always |

### Log File

**Location:** `Data/UORespawn/SYS/UOR_DebugLog.txt`

Written on server shutdown or crash. Contains:
- Session start/end times and duration
- End reason (Shutdown or Crash)
- Entry counts by level (INFO / DEBG / ERROR)
- Settings snapshot at session end
- All buffered log entries in order

### Message Format Convention

```
COMPONENT-[Detail or count]

Examples:
SERVICES-[Initialized]
PROCESS-[Spawned 3 for 2 players (total: 75)]
TILE SPAWN-[Found 'Rabbit' on tile 'grass']
REGION SPAWN-[Found 'Orc' in region 'Yew']
```

---

## Editor Integration

### Roles

| Server | Editor |
|--------|--------|
| Generates OUTPUT/ game data | Reads OUTPUT/ to populate UI dropdowns |
| Loads INPUT/ spawn definitions | Writes INPUT/ spawn definitions |
| Logs edits to COMMANDS/ | Reads and deletes COMMANDS/ files |
| Owns runtime state | Owns spawn pack source of truth |

### On Launch (Editor)

1. Read `OUTPUT/UOR_BestiaryList.txt` → creature dropdown
2. Read `OUTPUT/UOR_RegionList.txt` → region dropdown
3. Read `OUTPUT/UOR_TileList.txt` → tile dropdown
4. Read `OUTPUT/UOR_MapList.txt` → map selector
5. Read `OUTPUT/UOR_SignData.txt` → vendor location picker
6. Read `OUTPUT/UOR_HiveData.txt` → beekeeper location picker
7. Read `INPUT/UOR_SpawnSettings.csv` → settings panel
8. Check `COMMANDS/` → consume any pending server-side edit commands

### On Save (Editor)

1. Write `INPUT/UOR_BoxSpawn.bin`
2. Write `INPUT/UOR_RegionSpawn.bin`
3. Write `INPUT/UOR_TileSpawn.bin`
4. Write `INPUT/UOR_VendorSpawn.bin`
5. Write `INPUT/UOR_SpawnSettings.csv` (if settings changed)

> **Never manually edit OUTPUT/ files — they are overwritten on every server start.**

---

## Command System

### Purpose

The COMMANDS/ folder provides a one-way queue from server → editor for edits made in-game via the Control Gump or the spawn edit gumps.

### Data Flow

```
┌──────────────────────────────────────────────────────────────────┐
│                      DIRECTORY ROLES                              │
├──────────────────────────────────────────────────────────────────┤
│  OUTPUT/    One-way: Server → Editor   (auto-generated game data) │
│  INPUT/     One-way: Editor → Server   (spawn definitions)        │
│  COMMANDS/  One-way: Server → Editor   (in-game edit requests)    │
└──────────────────────────────────────────────────────────────────┘
```

### Command File Format

Each line in a `COMMANDS/` file is pipe-delimited:

```
Action|Target|Section|Trigger|SpawnName|ExtraData
```

| Field | Values | Description |
|-------|--------|-------------|
| `Action` | `Add`, `Remove`, `Update` | Operation type |
| `Target` | `Settings`, `Box`, `Region`, `Tile`, `Vendor` | Data type |
| `Section` | `None`, `Common`, `Uncommon`, `Rare`, `Water`, `Weather`, `Timed` | Spawn list |
| `Trigger` | `None`, `Weather`, `Timed` | Optional trigger condition |
| `SpawnName` | string | Creature name or settings key |
| `ExtraData` | string | Setting value or location identifier |

### ExtraData Format by Target

| Target | ExtraData | Example |
|--------|-----------|---------|
| `Settings` | `{value}` | `125`, `True`, `0.05` |
| `Box` | `{MapId},{BoxId}` | `0,5` |
| `Region` | `{MapId},{RegionName}` | `0,Britain` |
| `Tile` | `{MapId},{TileName}` | `1,grass` |
| `Vendor` | `{MapId},{X},{Y},{Z}` | `0,1434,1699,0` |

### Examples

```
# Settings
Update|Settings|None|None|SEARCH_INTERVAL|125
Update|Settings|None|None|ENABLE_DEBUG|True

# Spawn lists
Add|Box|Common|None|Orc|0,5
Remove|Region|Rare|None|Dragon|0,Britain
Add|Tile|Uncommon|None|RatmanArcher|1,grass
Add|Vendor|None|None|Blacksmith|0,1434,1699,0
```

### Restart Safety

If the server restarts before the Editor processes a command file, `GameManager.CheckAndApplyPendingCommands()` is called during startup. It reads any remaining files, applies changes to in-memory spawn data, and deletes the processed files. Commands are consumed exactly once — by whichever process (server or editor) reads them first.

### Fresh Install Behavior

| Scenario | Behavior |
|----------|----------|
| No settings CSV | Uses hardcoded defaults in `UOR_Settings` |
| No spawn binary files | Empty spawn dictionaries, warning logged |
| No command files | Command step is silently skipped |
| Unresolved spawn name (debug on) | Uses `PlaceHolder` creature |
| Unresolved spawn name (debug off) | Falls back to `WanderingHealer` |

---

## In-Game Spawn Editing

### Accessing the Spawn Editor

1. Use `[UOR` or `[UORespawn` to open the Control Gump (Administrator)
2. Click **Edit Spawn**
3. Target any land tile, sign, or beehive
4. The appropriate edit gump opens

### Target Resolution

| Target | Gump Opened |
|--------|-------------|
| Land tile | `SpawnEditGump` for matching Box, Region, and/or Tile entities |
| Sign item | `VendorEditGump` for the matching vendor location |
| Beehive static | `VendorEditGump` for the matching beehive location |

### SpawnEditGump — 6 Tabs

| Tab | Section |
|-----|---------|
| 1 | Water spawns |
| 2 | Weather spawns |
| 3 | Timed (night) spawns |
| 4 | Common spawns |
| 5 | Uncommon spawns |
| 6 | Rare spawns |

All add/remove actions are immediately written as commands to the appropriate `COMMANDS/*.txt` file.

### VendorEditGump

Shows the actual sign name from the world item (e.g., `"Bank Of Skara Brae"`). Displays a single list of vendor type names. Add and remove entries directly.

---

## Vendor System

### Overview

Vendors are NPCs spawned at sign and beehive locations configured in the Editor. They persist across server restarts and are tracked via `UOR_VendorSpawner`.

### ISpawner Ownership

```csharp
// All vendor spawn uses the ISpawner singleton
UOR_VendorSpawner.Instance.Claim(vendor, location);

// On-demand query
var allVendors = UOR_VendorSpawner.GetAllSpawn();

// Full teardown
int deleted = UOR_VendorSpawner.CleanupAll();
```

No external serial lists, no marker values (e.g., `Home.Z=999`). Ownership is determined entirely by `creature.Spawner == UOR_VendorSpawner.Instance`.

### Vendor Lifecycle

```
Server Startup
  → ReclaimAll()         — Mobile.Spawner re-set from saved serial list
  → InitializeVendors()  — if GetCount() > 0: skip; else: spawn from config
  → VendorService.Save() — logs count only; ISpawner field saves with Mobile

System Off / Reset
  → VendorSpawner.CleanupAll() — deletes all owned vendors

In-Game Edit (VendorEditService)
  → SaveChanges()           — applies to VendorEntity in memory
  → RespawnVendorsAtLocation() — deletes old, spawns new at same location
```

### Location Coordinate Notes

`VendorEntity.Location` stores the **inside spawn point**, not the sign/hive world position.

**Signs:**
```
North facing: spawn at (X, Y - 2, Z)
West facing:  spawn at (X - 2, Y, Z)
```

**Beehives:**
```
spawn at (X + 1, Y + 1, Z)
```

The reverse offsets are applied when targeting a sign or hive in-game to match the stored entity.

### VendorSpawn.bin Format

```
bool    IsSign
int32   SignType        (SignType enum)
int32   SignFacing      (SignFacing enum: North / West)
int32   X
int32   Y
int32   Z
int32   VendorListCount
[per vendor]:
  string  VendorTypeName
```

---

## Custom Mobiles & Items

### World NPCs

| Class | Description |
|-------|-------------|
| `WorldNPC` | Base class for all custom UORespawn NPCs |
| `TownNPC` | Generic town wanderer — spawned alongside vendors when `ENABLE_VENDOR_EXTRA=True` |
| `AmbushNPC` | Hidden paragon melee creature; triggers a group ambush when stepped on |
| `RiftMob` | Spell-absorbing wisp (`a rift wisp`) — spawns a `RiftGate` when it takes enough spell damage |
| `SpecialSnowMob` | Singleton boss (`the frost warden`) — paragon, cold-resist, snow-tile-only; only one exists in the world at a time; enforced by `_Active` static field |
| `PlaceHolder` | Debug-only mob used when spawn name resolution fails and `ENABLE_DEBUG=True` |

### Effect NPCs

Invisible, temporary NPCs spawned at a location to play a visual effect then self-delete. Controlled by `ENABLE_SPAWN_EFFECTS`.

| Class | Effect |
|-------|--------|
| `ConfettiEffectNPC` | Confetti particle burst |
| `ElectricEffectNPC` | Lightning bolt |
| `ExplosionEffectNPC` | Explosion |
| `FireEffectNPC` | Fire column |
| `GlowEffectNPC` | Glow aura |
| `MagicEffectNPC` | Magic sparkle |
| `MistEffectNPC` | Ground mist |
| `PoisonEffectNPC` | Poison cloud |
| `SmokeEffectNPC` | Smoke puff |
| `WaveEffectNPC` | Wave shimmer |
| `WindEffectNPC` | Wind swirl |

### Custom Items

| Class | Description |
|-------|-------------|
| `DebugFlag` | Placed at failed spawn locations when `ENABLE_DEBUG=True`. Self-deletes on server restart via `[AfterDeserialization]` → `Delete()`. Increments `TotalFlagsCleaned` in `UOR_Core` for startup log. |
| `RiftGate` | Portal opened by `RiftMob`. Has a 30-second self-delete timer. Self-deletes on restart via `[AfterDeserialization]` → `Delete()`. Increments `TotalGatesCleaned` in `UOR_Core`. |

### ModernUO Serialization

All custom mobiles and items use ModernUO's source-generated serialization:

```csharp
[SerializationGenerator(0, false)]
internal partial class RiftMob : BaseCreature { ... }

[SerializationGenerator(0, false)]
internal partial class DebugFlag : Item { ... }
```

Fields that must survive a world save use `[SerializableField(index)]`.

---

## Event System

### Subscribed Events

| Event | Handler Action |
|-------|----------------|
| `EventSink.WorldSave` | `IsPaused = true` — pause spawn during save |
| `EventSink.WorldSavePostSnapshot` | `IsPaused = IsLocked` — resume; save stats and vendor state |
| `EventSink.Connected` | Add player to `_RespawnerList` → start `SearchTimer` |
| `EventSink.Logout` | Remove player from `_RespawnerList`; pause if empty |
| `EventSink.Shutdown` | Flush log to disk, stop timers |
| `EventSink.ServerCrashed` | Emergency log flush, stop timers |

### Subscription Safety

```csharp
private static bool _EventsSubscribed = false;

private static void InitializeEvents()
{
    if (_EventsSubscribed) return;
    // ... subscribe
    _EventsSubscribed = true;
}

private static void UnsubscribeEvents()
{
    if (!_EventsSubscribed) return;
    // ... unsubscribe
    _EventsSubscribed = false;
}
```

`UnsubscribeEvents()` is called during `SHUTDOWN()` so the system can be toggled on/off at runtime without double-subscribing.

---

## Runtime Power Toggle

The system can be shut down and restarted live without a server restart using the Control Gump.

### State Fields

| Field | Type | Meaning |
|-------|------|---------|
| `IsPaused` | `bool` | Spawn is halted. Set to `true` during world saves, when all players log out, or while system is locked. Clears automatically when players connect. |
| `IsLocked` | `bool` | Admin lock from Control Gump. Blocks state changes. Toggled by `ToggleLock()`. |

### SHUTDOWN()

```
1. IsLocked = true, IsPaused = true
2. StopTimers()
3. UnsubscribeEvents()
4. VendorService.DeleteAllVendors()     — UOR_VendorSpawner.CleanupAll()
5. UOR_Utility.ClearAllSpawns()         — UOR_MobSpawner.CleanupAll()
```

### STARTUP()

```
1. InitializeVendors()   — respawn if none exist
2. InitializeEvents()    — re-subscribe (safe if already subscribed)
3. StartTimers()         — restart all 3 global timers
4. IsLocked = false, IsPaused = false
```

Staff can also add themselves to the spawn system manually:

| Command | Access | Action |
|---------|--------|--------|
| `[UOR` / `[UORespawn` | Administrator | Open Control Gump |
| `[UORAdd` | Counselor | Add yourself to respawner list |
| `[UORDrop` | Counselor | Remove yourself from respawner list |
| `[ShowRespawn` | Administrator | Visualize spawn around you |

---

## Troubleshooting

**No spawns appearing**
- Check `SYS/UOR_DebugLog.txt` for errors
- Verify binary files exist in `INPUT/`
- Set `ENABLE_DEBUG=True` for verbose output
- Confirm at least one player is logged in (system pauses on empty server)

**Settings not loading**
- Verify `UOR_SpawnSettings.csv` exists in `INPUT/`
- Check CSV format: `SettingName,Value` — no spaces around the comma
- Look for clamping messages in the startup log

**Editor data is stale**
- Restart the server — `OUTPUT/` files regenerate every startup
- Never hand-edit files in `OUTPUT/`

**New creature not appearing in Editor**
- Restart server to regenerate `UOR_BestiaryList.txt`
- Ensure the class inherits from `BaseCreature`
- Ensure it has a `[Constructible]` parameterless constructor

**New region not appearing in Editor**
- Restart server to regenerate `UOR_RegionList.txt`
- Verify the region has a non-null name in `regions.xml`

**Vendors respawning every restart**
- Verify `ENABLE_VENDOR_SPAWN=True` in settings
- Confirm `UOR_VendorSpawner` item exists in the world (it is created on first run and must persist)
- Check startup log for "Reclaimed" vs "spawned" vendor messages

**DebugFlag / RiftGate not cleaning up**
- These self-delete via `[AfterDeserialization]` on every restart — this is expected behaviour
- The startup log reports counts: `FLAGS-[N Deleted]`, `GATES-[N Deleted]`

**"Tile Doesn't Exist" warnings**
- Expected for interior tiles (stone floor, wooden floor, etc.)
- Add the tile to spawn configuration in the Editor if desired

---

## Changelog

### 2.0.1.4 — March 10, 2026

#### Removed

- **`MAX_RECYCLE_TOTAL`** removed entirely. Spawn volume is now auto-limited by the per-type count setting (`MAX_RECYCLE_TYPE`, default 20), which scales naturally with the number of unique creature types registered on the server. No global total cap is applied.

#### Performance

- **`UOR_Spawner` — O(1) serial ownership via `HashSet` shadow** — A non-serialized `HashSet<uint> _serialsSet` is maintained alongside the serializable `List<uint> _spawnedSerials`. `Claim()` uses `_serialsSet.Add()` (O(1)) instead of `List.Contains()` (O(n)). The `HashSet` is rebuilt from the list on every load via `[AfterDeserialization]`. No serialization format change; no version bump required.
- **`UOR_MobSpawner.CountSwimmers()`** — New allocation-free method counts active swimmer spawn by iterating `_spawnedSerials` directly. Replaces the `GetAllSpawn().Count(bc => bc.CanSwim)` call in `IsWaterLimit()` that allocated a full spawn list on every water-tile check.
- **`SpawnQueryService.ValidateAndTrim()`** — Replaces the prior N+1 query pattern (one `GetAllSpawn()` call per unique spawn type per validate cycle) with a single pass that groups all spawn by type and trims excess in-place.
- **`GetRespawners()`** — Return type changed from `List<RespawnerEntity>` to `IReadOnlyCollection<RespawnerEntity>` backed directly by `_RespawnerList.Values`. Eliminates a `ToList()` allocation on every 250 ms `ProcessTimer` tick.
- **`GameManager.GenTileList()`** — Duplicate tile name deduplication changed from `List.Contains` (O(n) per item) to `HashSet<string>.Add` (O(1) per item), reducing overall deduplication from O(n²) to O(n).

#### Code Health

- Removed 6 dead `SpawnQueryService` methods (`GetTypeCount`, `GetExcessSpawn`, `TrimExcess`, `GetTotalSpawnCount`, `IsAtTotalLimit`, `GetAllSpawnTypes`) — all superseded by `ValidateAndTrim`.
- `ValidateService.Validate()` simplified to a single `queryService.ValidateAndTrim(MAX_RECYCLE_TYPE)` call.
- `ProcessTimer` and `ControlService` for-loops converted to `foreach` (consistent with `IReadOnlyCollection<T>` which has no indexer).
- `SearchTimer` — Removed redundant `else` branch after unconditional `return`.
- `ValidateService` constructor visibility corrected from `public` to `internal`.
- `ControlService` — `.ToLower()` replaced with `.ToLowerInvariant()` for culture-safe key normalization.
- `XMLManager` — `.ToUpper()` replaced with `.ToUpperInvariant()`.
- `ProcessService` — Town extra spawn now selects spawn point via `GetBestSpawnPoint` instead of `GetSpawnPoint`.
- `UOR_Settings.ApplySettingCommand` — Now dispatches on the normalized key instead of the raw input key.

---

## Contact & Links

The UORespawnServer package is developed

- **ServUO Forums** — https://www.servuo.com (project thread)
- **ModernUO GitHub** — https://github.com/modernuo/ModernUO (server platform)

For server-side issues, check `SYS/UOR_DebugLog.txt` first. It contains a full session trace with settings snapshot and error log.

---

*Generated from source — reflects actual codebase state as of version 2.0.1.4.*
