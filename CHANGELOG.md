# Changelog

All notable changes to UORespawn will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0.8] - 2026-03-02

### Added

#### In-Game Spawn Editing
- **[EditBox** command - Target a creature to edit its parent box spawn directly in-game
- **[EditRegion** command - Target a creature to edit its parent region spawn in-game
- **[EditTile** command - Target a creature to edit its parent tile spawn in-game
- **[EditVendor** command - Target a vendor to edit its vendor spawn in-game
- Commands write to `COMMANDS/` folder; editor detects and opens the appropriate spawn panel
- Two-way editing: changes in editor sync back to server automatically

#### Server Auto-Install & Update
- **Automatic Installation** - Server scripts auto-install to `Scripts/Custom/UORespawnServer/` when linking
- **Auto-Update Detection** - Editor detects version mismatches and prompts to update server files
- **Legacy Cleanup** - Automatically removes old `UORespawn*/Respawn*` folders during install/update
- No more manual file copying required

#### COMMANDS Folder
- New `Data/UORespawn/COMMANDS/` directory for in-game edit command queue
- Server writes command files; editor reads and opens appropriate spawn panel
- Files auto-cleaned after processing

### Changed
- **Settings Property** - `ServUODataFolder` renamed to `ServerFolder` for clarity
- **Server Folder Structure** - Now uses `Scripts/Custom/UORespawnServer/` (namespace match)
- **Export Path Display** - Settings component now shows correct relative path
- **Instructions Page** - Added comprehensive Server Integration and In-Game Editing sections

### Fixed
- Server folder naming now matches `UORespawnServer` namespace exactly
- Improved error handling during server setup operations

### Technical
- `ServerSetupUtility` handles auto-install, update detection, and legacy cleanup
- `CommandService` processes in-game edit commands via COMMANDS folder
- Version consistency enforced between editor (2.0.0.8) and server (2.0.0.8)

## [2.0.0.7] - 2026-03-01

### Added
- **ISpawnEntity Interface** - New shared interface for consistent spawn entity handling
  - Implemented by `BoxSpawnEntity`, `TileSpawnEntity`, and `RegionSpawnEntity`
  - Defines the 6-list spawn structure (Water, Weather, Timed, Common, Uncommon, Rare)
  - Includes `WeatherSpawn` and `TimedSpawn` trigger properties
  - Enables future generic spawn handling and validation
- **Settings Validation** - Comprehensive bounds checking on all numeric settings
  - `Math.Clamp()` validation in property setters prevents invalid values
  - Validation constants define allowed ranges (e.g., `MinRangeValue`, `MaxMobsValue`)
  - `ValidateRanges()` helper ensures MinRange ≤ MaxRange
  - `GetSettingsSummary()` for debugging/logging current settings
- **Point2D Helper Struct** - Custom lightweight struct for spawn statistics
  - Replaces `Microsoft.Maui.Graphics.Point` in `MapDisplayUtility`
  - Eliminates MAUI-specific warnings in stats processing

### Changed
- **Stats File Format** - Now uses numeric map ID instead of map name
  - Format: `Time|Player|MapID|PX|PY|SX|SY|CreatureName`
  - Example: `12:42 AM|Wilson|1|1359|1844|1360|1645|Bird`
  - `MapDisplayUtility` updated to parse map ID directly with `int.Parse()`
- **Instructions Page** - Third-pass comprehensive review and updates
  - Renamed "Admin GUI" → "Control Dashboard" throughout
  - Fixed spawn system count: "three" → "four" (added Vendor Spawns)
  - Changed "World Spawns" → "Tile Spawns" for consistency
  - Removed outdated command references (`[SpawnReload`, `[SpawnAdmin`, `[PushRespawnStats`, `[DebugRespawn`)
  - Clarified linked server auto-sync workflow
  - Simplified Section 9 (Testing Workflow) from ~200 to ~80 lines
- **Server Scripts Folder** - Renamed `UORespawnSystem` → `UORespawnServer`
  - Updated folder: `Data/SERVER/UORespawnServer/`
  - Updated zip: `UORespawnServer.zip`
  - All references updated in codebase and documentation
- **WebView Margin** - Fixed overlap with nav menu (250px → 260px)

### Fixed
- **CancellationTokenSource Disposal** - Proper disposal pattern in `DataWatcher.OnChanged()`
  - Now calls `Dispose()` before reassigning to prevent resource leaks
- **Empty Duplicate File** - Removed empty `Scripts/Utilities/ViewService.cs`
  - Real implementation lives in `Scripts/Services/ViewService.cs`

### Technical
- All spawn entities now implement `ISpawnEntity` interface
- Settings validation constants: `MinRangeValue=1`, `MaxRangeValue=500`, `MinMobsValue=1`, etc.
- Chance values clamped to 0.0-1.0 range
- Interval values clamped to reasonable bounds (10ms-60000ms for intervals)
- Complete XML documentation on `VendorEntity` properties and methods
- Build verified with .NET 10 - no warnings or errors

## [2.0.0.6] - 2026-02-20

### Added
- **Vendor Spawn Editor** - Full-featured page for configuring vendors at shop signs and hives
  - Browse sign types (Bakery, Blacksmith, Tailor, etc.) organized as expandable cards
  - Per-location vendor assignment with map visualization
  - Bulk "Add to All Locations" feature for quick setup
  - Vendor markers on map with green glow for configured locations
  - Gold highlight for selected/focused location
  - Badge system showing configured/total locations per sign type
- **Bestiary Favorites** - Star creatures to add them to a quick-access favorites list
  - Toggle between "All" and "Favorites" view in Settings bestiary browser
  - Favorites persist across sessions and are saved with settings
  - Starred items appear with yellow star icon
- **Vendor Favorites** - Star vendors for quick access in vendor spawn modal
  - Same functionality as Bestiary Favorites for vendor types
- **Region map index numbers** - Each region displays its list index for quick identification
  - Numbers dynamically sized based on region area
  - Color changes based on selection state (gold selected, cyan hover, white normal)
- **Region search by index** - Search input in region list header to jump to region by number
- **Pack Dashboard enhanced stats** - Shows both entry counts and location counts
  - Main value shows spawn entries (creatures/vendors)
  - Sublabel shows location count (boxes, tiles, regions, signs)
- **Dirty flag system** - Intelligent pack sync only when actual changes are made
  - Prevents false "Modified" detection on Save All

### Removed
- **Reset/Backup System** - Completely removed in favor of simpler pack management
  - Removed `PACKS/Backup/` folder and all backup-related code
  - Removed Reset button from approved pack cards
  - Approved packs can now be edited like any other pack (just not deleted)
  - Users can re-download the app to restore default pack if needed

### Changed
- **Vendor Spawn system** fully integrated with editor workflow
  - Sign and hive data auto-loaded from server OUTPUT files
  - Vendor spawn binary saved alongside other spawn types
- **SpawnCategoryModal** updated to support favorites filtering
  - Shows favorites section at top when available
  - Favorites filtered to only show items in current source list
- **Pack handling** improved with SRP (Single Responsibility Principle)
  - All pack types (Approved, Created, Imported) use unified sync logic
  - `ActivePackDataPath` works for any loaded pack type
  - Dirty flag tracks actual data changes, not just saves
- **Approved Packs** - Now editable but not deletable (no reset functionality)
  - DefaultPack lives directly in `PACKS/Approved/` folder
  - Both folder and ZIP included for flexibility
- **Pack Folder Structure** simplified to three categories:
  - `PACKS/Approved/` - Bundled packs (editable, not deletable)
  - `PACKS/Created/` - User-created packs
  - `PACKS/Imported/` - User-imported packs
- Documentation updated throughout Instructions page for new features
- Deterministic binary serialization for consistent pack comparison

### Fixed
- **DefaultPack not loading on fresh install** - Fixed .csproj to copy all DefaultPack files
  - Previously only `pack.json` was being copied to output
  - Now copies `DefaultPack.zip` and entire `DefaultPack/` folder contents
- **Created folder not tracked in Git** - Fixed .gitignore pattern for .gitkeep files
- Pack sync only triggers when spawn data is actually modified
- Vendor spawn page correctly loads sign/hive data for selected map

### Technical
- `IsSpawnDataDirty` flag in `PathConstants` tracks actual data modifications
- Dirty flag set at edit points (create/delete spawns, modal saves, trigger changes)
- `SyncFileToActivePack` respects dirty flag to prevent false modifications
- Sorted dictionary serialization (`OrderBy`) for deterministic output
- `VendorSpawnComponent.razor` with full map integration and marker rendering
- Favorites stored in `Settings.BestiaryFavorites` and `Settings.VendorFavorites` lists
- Removed `IsModified` property from `SpawnPackInfo` entity
- Removed backup/reset methods from `SpawnPackService`
- Updated `UORespawnApp.csproj` - Fixed DefaultPack file copying rules

## [2.0.0.5] - 2026-02-19

### Added
- Vendor Spawn support with server-side integration
- Vendor Spawn toggle in Settings page
- Debug Log functionality with per-session logging
- Region Cleaner utility for spawn pack maintenance
- Reload support for Vendors in ServUO

### Changed
- Settings page polished and reorganized
- Server and Client version synchronization updated
- Default Spawn Pack cleaned and updated with missing spawns
- Bumped Settings version for new Vendor Spawn configuration

### Fixed
- Regions List loading corrected
- Region Spawn issues resolved
- JSON warning messages from MAUI when handling Spawn Packs
- Compatibility with .NET 10 MAUI update
- Default Pack Regions functionality
- Spawn Pack loading reliability (hot fix)

### Technical
- Server-side refactoring for Vendor Spawns
- Improved Debug Log session management

## [2.0.0.4] - 2026-02-17

### Added
- Three-category spawn pack system: Approved, Created, and Imported packs
- "Create New Pack" functionality with modal dialog
- Auto-apply behavior when creating new packs (pack is immediately loaded after creation)
- Default pack auto-extraction from Backup ZIP on first launch

### Changed
- Unified terminology throughout app: "Box Spawn" (not Map Spawn), "Tile Spawn" (not World Spawn)
- Instructions page comprehensively verified and updated for all 12 sections
- Removed outdated Animals/Creatures concept (now uses 6-frequency system throughout)
- Spawn Packs page reorganized with category tabs (Approved, Created, Imported)
- Delete protection: Approved packs cannot be deleted; Created/Imported can only be deleted when not loaded

### Fixed
- Instructions terminology now matches actual UI (Box/Tile/Region spawn pages)
- Info icon descriptions corrected for spawn type priorities
- Max Mobs and Max Crowd descriptions clarified in Instructions
- Priority color descriptions (1=highest/red, 5=lowest/green) corrected

## [2.0.0.3] - 2025-02-16

### Added
- Interactive XML spawner overlay - hover to highlight, click for tooltip with location and home range
- Interactive server spawn statistics - dwell-based tooltips showing player name, location, and total spawn events
- WASD/Arrow key map panning with smooth diagonal movement support
- Zoom toggle button (1x actual size / 2x zoomed view)
- Spawn Packs system for sharing and importing spawn configurations
  - Pack Dashboard with statistics (Box Spawns, Tile Spawns, Region Spawns, Maps, Entries, Unique Mobs)
  - Import/Export spawn packs as zip files
  - Submit packs for official approval
  - Apply packs with single click
- Pack Dashboard visual styling (golden centered pack name, blue frequency names)
- Spawn ID search input in spawn list headers
- Info icons throughout UI for contextual help
- Back-to-top button on Instructions page

### Changed
- XML spawners now display as green circles with X markers (previously boxes)
- Server spawn dots highlight individually on hover (previously all player dots)
- Instructions page updated with complete interactive feature documentation
- Toasts now only show warnings (removed success/info toasts for cleaner UX)
- Copilot instructions updated with comprehensive project documentation

### Fixed
- Spawn type terminology corrected in Instructions (Box/Region/Tile spawn pages)

## [2.0.0.2] - 2025-01-15

### Added
- Complete rewrite from WinForms to .NET MAUI with Blazor
- Visual Box Spawn editor with pan and zoom
- Interactive mini-map with click-to-navigate
- Tile Spawn system for tile-based spawning
- Region Spawn system for named server regions
- Comprehensive settings panel
- Dark theme UI
- Built-in instructions page
- Auto-sync with ServUO Data folder (Windows/macOS)
- XML spawner location overlay
- Server spawn heatmap visualization
- Priority-based spawn layering
- Timed spawn support (day/night cycles)
- Custom map image upload with backup/restore
- Bestiary management with search
- Special NPCs (TownNPC, WorldNPC, AmbushNPC, Effect NPCs)
- Cross-platform support (Windows, macOS)
- Platform detection for server integration
- Auto-save on all changes
- Customizable spawn box appearance
- Aspect ratio-aware mini map with letterboxing

### Changed
- Modernized UI with Bootstrap 5
- Improved file handling with error checking
- Enhanced spawn data validation
- Better empty string filtering

### Fixed
- Mini map aspect ratio distortion
- Mini map click precision
- Data watcher warnings
- Platform compatibility checks

### Technical
- Built with .NET 10
- MAUI Blazor Hybrid architecture
- JavaScript interop for map rendering
- File system watcher with debouncing
- Responsive Bootstrap UI
- Proper async/await patterns

## [Previous Versions]

### [0.x] - WinForms Era
- Original WinForms application
- Basic spawn editing
- XML spawner support
- Map visualization

---

For detailed changes between versions, see the [GitHub Releases](https://github.com/Kita72/UORespawnProject/releases) page.
