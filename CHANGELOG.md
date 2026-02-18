# Changelog

All notable changes to UORespawn will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
