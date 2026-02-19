# Copilot Instructions

## Project Overview
UORespawn is a professional spawn management system for Ultima Online servers running ServUO. Built with .NET 10 MAUI Blazor Hybrid, it provides a **desktop-only** editor (Windows/macOS) for creating and managing creature spawns.

**Platform Support: Desktop Only (Windows, macOS)**
- We do NOT support mobile platforms (iOS, Android)
- We do NOT support web deployment
- Blazor Hybrid is used for its UI benefits, not cross-platform mobile/web capabilities

## Architecture

### Data Flow
- **Local Data**: `Data/UOR_DATA/` contains active spawn binary files (`.bin`) used by the editor
- **Spawn Packs**: `Data/PACKS/` stores backup/staging packs; applying a pack copies files to `UOR_DATA`
- **Server Sync**: When ServUO folder is linked, DataWatcher monitors the **server's** `Data/UOR_DATA/` folder for `.txt` file changes (bestiary, region list, spawner list) and reloads them automatically
- **Binary Serialization**: Uses `BinarySerializationService` for fast, efficient `.bin` file loading (no CSV)

### Key Services
- `ViewService` - Manages view navigation and shared state (current map, XML/Spawns toggle visibility)
- `DataWatcher` - Monitors linked ServUO server folder for file changes (Windows/macOS only)
- `SpawnPackService` - Loads, validates, imports, exports, and applies spawn packs
- `ToastService` - Shows warning toasts only (success/info toasts removed for cleaner UX)
- `BinarySerializationService` - Handles all binary file read/write operations

## Project Guidelines
- Spawn packs are stored in `Data/PACKS`; applying a pack replaces live spawn/settings files in `Data/UOR_DATA` and relies on the data watcher to sync to the server, with `PACKS` acting as a backup/staging area.
- Use Entities instead of DTO Models for data objects, following ServUO-style custom save approach.
- Toasts: Only show warnings that explain failures. Do not show success/info toasts.
- When creating spawn pack ZIPs (or server script ZIPs), zip the files directly at the root of the archive, then name the ZIP file as the desired folder name. Do NOT zip the folder itself. Correct: `DefaultPack.zip` contains `UOR_BoxSpawn.bin`, `UOR_TileSpawn.bin`, etc. at root level. Wrong: `DefaultPack.zip` contains `DefaultPack/UOR_BoxSpawn.bin` (nested folder). The app extracts ZIPs to a folder matching the ZIP name, so nesting creates unwanted double-folder structure.

## UI Features

### Map Editor Features (Box Spawn & Region Spawn Pages)
- **Mini Map**: Click to jump, green rectangle shows viewport, Reset button returns to origin
- **XML Toggle**: Shows/hides XML spawner locations (green circles with X markers)
  - Interactive: Hover highlights spawner, click shows tooltip with location and home range
  - Shared state via `ViewService.ShowXMLSpawners` across Box/Region pages
- **Spawns Toggle**: Shows/hides server spawn statistics (colored dots per player)
  - Interactive: Hover over player dot, after 500ms dwell shows tooltip with player name, location, total events
  - Individual dot highlight (not all player dots) - white border on hovered dot
  - Shared state via `ViewService.ShowServerSpawns` across Box/Region pages
- **Spawn Boxes**: Click to select, hover shows golden glow highlight

### Pack Dashboard (Spawn Packs Page)
- Pack name: Centered, golden color (goldenrod), bold
- Frequency names (Water, Weather, Timed, Common, Uncommon, Rare): Blue color (#6ea8fe)
- Stats tiles show: Box Spawns, Tile Spawns, Region Spawns, Maps, Entries, Unique Mobs

### Interactive Elements
- Info icons (`InfoIcon.razor`) - Hover for contextual help tooltips
- Back-to-top button on Instructions page
- Spawn ID search input in spawn list headers

## Code Style
- JavaScript in `wwwroot/js/map.js` handles canvas rendering and mouse interactions
- Blazor components in `Components/Controls/` with scoped CSS (`.razor.css`)
- Dwell-based tooltips: Use `setTimeout` with 500ms delay, `clearTimeout` on mouse move