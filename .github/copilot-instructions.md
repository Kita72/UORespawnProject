# Copilot Instructions

## Project Overview
UORespawn is a professional spawn management system for Ultima Online servers running ServUO. Built with .NET 10 MAUI Blazor Hybrid, it provides a **desktop-only** editor (Windows/macOS) for creating and managing creature spawns.

**Platform Support: Desktop Only (Windows, macOS)**
- We do NOT support mobile platforms (iOS, Android)
- We do NOT support web deployment
- Blazor Hybrid is used for its UI benefits, not cross-platform mobile/web capabilities

---

## Project Structure Mind Map

```
UORespawnProject/
├── UORespawnApp/                    # Main MAUI Blazor Hybrid application
│   ├── Components/
│   │   ├── Controls/                # Blazor UI components (.razor + .razor.css)
│   │   ├── Layout/                  # MainLayout.razor, NavMenu
│   │   └── Pages/                   # Page-level components
│   ├── Scripts/
│   │   ├── Constants/               # PathConstants, AppConstants
│   │   ├── Entities/                # Data entities (ServUO-style, not DTOs)
│   │   ├── Services/                # Business logic services
│   │   └── Utilities/               # Static helper utilities
│   ├── Data/                        # Runtime data folder (user accessible)
│   │   ├── UOR_DATA/                # Active spawn binary files (.bin)
│   │   ├── PACKS/                   # Spawn packs (Approved, Created, Imported)
│   │   └── MAPS/                    # Map images (Map0.bmp - Map255.bmp)
│   ├── Resources/Raw/               # Bundled resources (default reference data)
│   └── wwwroot/
│       ├── css/                     # Global styles
│       └── js/map.js                # Canvas rendering & mouse interactions
│
└── Data/SERVER/                     # Server-side scripts (copied to ServUO)
    └── UORespawnSystem/
        ├── Commands/                # In-game admin commands
        ├── Core/                    # Core spawn engine
        ├── Entities/                # Server entity definitions
        └── Mobiles/                 # Custom mobile behaviors
```

---

## Architecture

### Server Directory Structure (v2.0+)
When linked to ServUO, the server uses this folder structure under `Data/UORespawn/`:
```
UORespawn/
├── INPUT/       # Editor writes .bin files here (server reads)
├── OUTPUT/      # Server writes .txt files here (editor reads)
├── STATS/       # Heatmap/player spawn statistics
└── SYS/         # Internal tracking files
```

### Data Flow
1. **Editor → Server**: Editor saves `.bin` files to `Data/UOR_DATA/`, DataWatcher syncs to server's `INPUT/`
2. **Server → Editor**: Server auto-generates `.txt` files in `OUTPUT/` on startup (bestiary, regions, spawners, vendors)
3. **Spawn Packs**: `Data/PACKS/` organizes packs by category (Approved/Created/Imported); applying a pack copies files to `UOR_DATA/`

### Binary Files (Editor Writes)
| File | Purpose |
|------|---------|
| `UOR_BoxSpawn.bin` | Box spawn definitions |
| `UOR_TileSpawn.bin` | Tile/point spawn definitions |
| `UOR_RegionSpawn.bin` | Region-based spawn definitions |
| `UOR_VendorSpawn.bin` | Vendor spawn definitions |
| `UOR_Settings.bin` | Editor settings |

### Text Files (Server Generates - Auto on Startup)
| File | Purpose |
|------|---------|
| `UOR_Bestiary.txt` | All spawnable creature types |
| `UOR_RegionList.txt` | All region names and bounds |
| `UOR_SpawnerList.txt` | XML spawner locations (for overlay) |
| `UOR_VendorList.txt` | Vendor types available |

---

## Key Services (Scripts/Services/)

| Service | Purpose |
|---------|---------|
| `ViewService` | View navigation, shared state (current map, XML/Spawns toggles) |
| `DataWatcher` | Monitors server folder for file changes (Windows/macOS) |
| `SpawnPackService` | Load, validate, import, export, apply spawn packs |
| `BinarySerializationService` | All .bin file read/write operations |
| `ToastService` | Warning toasts only (no success/info toasts) |
| `SettingsService` | App settings persistence |

---

## Key Utilities (Scripts/Utilities/)

| Utility | Purpose |
|---------|---------|
| `MapUtility` | Map ID ↔ name conversion, available maps scanning |
| `BestiaryUtility` | Load/parse bestiary creature list |
| `RegionUtility` | Load/parse region definitions |
| `SpawnerListUtility` | Load XML spawner locations for map overlay |
| `PathConstants` | Centralized file paths (`MapsPath`, `DataPath`, etc.) |

---

## Key Entities (Scripts/Entities/)

| Entity | Purpose |
|--------|---------|
| `BoxSpawn` | Box spawn definition with frequency entries |
| `TileSpawn` | Point/tile spawn definition |
| `RegionSpawn` | Region-based spawn definition |
| `VendorSpawn` | Vendor spawn definition |
| `SpawnEntry` | Individual creature in a spawn (name, min, max) |
| `FrequencyData` | Spawn timing (Water, Weather, Timed, Common, Uncommon, Rare) |
| `XMLSpawnPoint` | XML spawner location for map overlay |
| `SpawnPackStats` | Pack statistics (counts, maps, entries) |

---

## Map System

### Map ID Architecture
- **IDs 0-5**: Default UO maps with known names and dimensions
- **IDs 6-255**: Custom maps supported (generic "Map N" naming)
- **Storage**: `Data/maps/Map{id}.bmp` files
- **Validation**: Default maps (0-5) must match exact dimensions; custom maps are flexible

### Default Map Reference
| ID | Name | Dimensions |
|----|------|------------|
| 0 | Felucca | 7168 × 4096 |
| 1 | Trammel | 7168 × 4096 |
| 2 | Ilshenar | 2304 × 1600 |
| 3 | Malas | 2560 × 2048 |
| 4 | Tokuno | 1448 × 1448 |
| 5 | Ter Mur | 1280 × 4096 |

### Key Principle
- **Data files use numeric map IDs only** (never names)
- **UI displays friendly names** for default 6, "Map N" for custom
- All code should be fluid with any map count (0-255)

---

## UI Components (Components/Controls/)

### Spawn Editors
| Component | Purpose |
|-----------|---------|
| `BoxSpawnComponent.razor` | Box spawn editor with map canvas |
| `TileSpawnComponent.razor` | Tile/point spawn editor |
| `RegionSpawnComponent.razor` | Region-based spawn editor |
| `VendorSpawnComponent.razor` | Vendor spawn editor |

### Supporting Components
| Component | Purpose |
|-----------|---------|
| `SpawnPacksComponent.razor` | Pack dashboard with stats tiles |
| `SettingsComponent.razor` | App settings, server linking, map management |
| `InstructionsComponent.razor` | User documentation and commands |
| `InfoIcon.razor` | Hover tooltip helper icons |

### Map Editor Features
- **Mini Map**: Click to jump, green viewport rectangle, Reset button
- **XML Toggle**: Green circles showing XML spawner locations
- **Spawns Toggle**: Colored dots showing player spawn statistics
- **Spawn Boxes**: Click to select, golden glow on hover

---

## Spawn Frequencies

Spawns use a frequency system for timing:
| Frequency | Color | Purpose |
|-----------|-------|---------|
| Water | Blue (#6ea8fe) | Water-based spawns |
| Weather | Blue (#6ea8fe) | Weather-triggered spawns |
| Timed | Blue (#6ea8fe) | Time-of-day spawns |
| Common | Blue (#6ea8fe) | Frequent spawns |
| Uncommon | Blue (#6ea8fe) | Moderate spawns |
| Rare | Blue (#6ea8fe) | Infrequent spawns |

---

## Project Guidelines

### Data Patterns
- Use **Entities** (not DTO Models) following ServUO-style custom save approach
- Spawn packs in `Data/PACKS/` are backup/staging; applying copies to `Data/UOR_DATA/`
- DataWatcher syncs changes to linked server automatically

### UI/UX Patterns
- **Toasts**: Only show warnings that explain failures (no success/info toasts)
- **Tooltips**: Use 500ms dwell delay (`setTimeout`/`clearTimeout`)
- **Pack names**: Centered, golden color (goldenrod), bold
- **Frequency names**: Blue color (#6ea8fe)

### ZIP Packaging Rule
When creating spawn pack ZIPs:
- ✅ **Correct**: ZIP files directly at root level, name ZIP as folder name
- ❌ **Wrong**: ZIP containing nested folder structure
- Example: `DefaultPack.zip` contains `UOR_BoxSpawn.bin` at root (not `DefaultPack/UOR_BoxSpawn.bin`)

---

## Server Commands (In-Game)

### Admin Commands (Requires AccessLevel.Administrator)
| Command | Purpose |
|---------|---------|
| `[UORespawn` | Opens main spawn management gump |
| `[ReloadSpawns` | Reloads all spawn data from files |
| `[RespawnAll` | Forces all spawners to respawn |
| `[ClearSpawns` | Removes all spawned creatures |
| `[SpawnStats` | Shows spawn statistics |

### File Generation
Server **auto-generates** all reference files on startup - no manual commands needed:
- Bestiary, Region list, Spawner list, Vendor list, Sign list, Hive list

---

## Code Style

### C# Conventions
- .NET 10 with C# 14.0 features
- Collection expressions: `List<T> items = [];`
- Pattern matching and switch expressions
- File-scoped namespaces

### Blazor Conventions
- Scoped CSS files (`.razor.css`) for component styles
- JavaScript interop via `wwwroot/js/map.js`
- Canvas rendering for map displays

### Naming Conventions
- Services: `{Name}Service.cs`
- Utilities: `{Name}Utility.cs`
- Entities: `{Name}.cs` (in Entities folder)
- Components: `{Name}Component.razor`