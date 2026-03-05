# Changelog

All notable changes to UORespawn will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.1.1] - 2026-03-05

### Added

#### XML Spawner Editing
- **Edit XML Spawner** - Press ENTER while hovering over an XML spawner on the map to edit it
  - EditXmlSpawnerModal component with HomeRange and MaxCount sliders
  - Side-by-side layout: bestiary list (left) + current spawn entries (right)
  - Click bestiary creatures to add, click existing entries to remove
  - Favorites support for quick access to commonly used creatures
  - Applies to Box Spawn, Region Spawn, and Vendor Spawn map editors
- **Updated JavaScript Tooltips** - Map tooltips now show both "Press ENTER to edit" and "Press DEL to delete"

#### Documentation
- **[ShowRespawn] Command** - Added to Instructions page Debug Mode section
  - In-game command documentation with usage table
  - Shows all spawnable creatures at your location

### Changed

#### Settings Page Reorganization
- **ServerIntegrationCard** - New unified component combining all server integration options
  - Local/Remote toggle replaces separate cards
  - Remote mode includes FTP/Manual sub-selection
  - Cleaner, more organized settings layout
- **Removed FtpSettingsCard** - Functionality consolidated into ServerIntegrationCard

#### Tooltip Corrections
- **Scale Spawn** - Fixed tooltip: "Minimum scale factor for spawned creatures (0.75 = 75% size)"
- **Extra Patrons** - Renamed from "Extra Townsfolk" with corrected tooltip about additional vendor patrons
- **Town Spawn** - Fixed tooltip: "Spawn town creatures and NPCs within town boundaries"
- **Grave Spawn** - Fixed tooltip: "Spawn undead creatures near grave/cemetery areas"

### Fixed
- **Light Theme Button Contrast** - Fixed `btn-outline-light` buttons invisible in light mode
  - Spawn Pack page Import button now visible in both themes
  - Modal buttons properly styled for light/dark themes
  - Uses dark text (#495057) and medium border (#6c757d) in light mode

### Technical
- `EditXmlSpawnerModal.razor` component with full CRUD support for XML spawner editing
- `ServerIntegrationCard.razor` consolidates Local Link, FTP Sync, and Manual Export options
- CSS light theme overrides in `SpawnPacksComponent.razor.css` for button visibility
- Build verified with .NET 10 - no warnings or errors

## [2.0.1.0] - 2026-03-04

### Added

#### Dependency Injection Architecture
- **SpawnDataService** - Centralized spawn data management via DI
  - Thread-safe spawn dictionaries with proper locking
  - Singleton service for consistent state across components
  - Methods: AddBoxSpawn, ClearBoxSpawns, InitializeBoxSpawns, etc.
- **SessionService** - DI-injectable session management
  - Wraps Session with proper lifecycle management
  - MapImageCacheService integration for base64 caching
  - Thread-safe map image retrieval
- **MapImageCacheService** - Efficient map image caching
  - Lazy loading with LRU eviction (max 6 maps)
  - Thread-safe concurrent dictionary storage
  - Base64 data URL generation for Blazor rendering

#### Thread Safety Improvements
- **Session Lock** - Added .NET 9+ Lock type for thread-safe operations
  - Protected MapId, LinkedServerPath, and ServerMode properties
  - Safe concurrent access from multiple components
- **DataWatcher Lock** - Thread-safe file monitoring
  - Protected _isRunning state with proper locking
  - Safe start/stop operations from any thread
- **SpawnDataService Lock** - Protected spawn dictionary operations
  - All CRUD operations properly synchronized

#### File Operations
- **FileUtility** - Robust file operations with retry logic
  - WriteAllTextWithRetry: 3 attempts with 100ms delays
  - WriteAllBytesWithRetry: Handles transient file locks
  - Proper exception handling and logging

#### Constants & Configuration
- **ViewportConstants** - Centralized viewport dimensions
  - DEFAULT_WIDTH = 800, DEFAULT_HEIGHT = 600
  - Consistent sizing across all map components

#### UI Components
- **FrequencyButtonsComponent** - Reusable frequency selector
  - Extracted from TileSpawnComponent for reuse
  - All 6 frequency types: Water, Weather, Timed, Common, Uncommon, Rare
  - Proper styling with theme support

#### Visual Improvements
- **Trigger Dashboard** - Weather/Time indicators on TileSpawnComponent
  - Visual icons (cloud, clock) showing active triggers
  - Appears when IsWeather or IsTimed is enabled
  - Subtle styling that doesn't distract from main content
- **NavMenu Icon Updates** - Improved spawn type icons
  - Box Spawns: bi-bounding-box (was bi-box)
  - Tile Spawns: bi-grid-3x3-gap (was bi-geo-alt)

### Changed

#### Architecture Refactoring
- **Utility.cs Delegation Pattern** - Static class now delegates to DI services
  - BoxSpawns, TileSpawns, RegionSpawns, VendorSpawns → SpawnDataService
  - SESSION → SessionService
  - Backward compatible: existing code works unchanged
  - Migration path: new code should inject services directly

#### Navigation & Layout
- **Map Selector Moved** - From NavMenu to individual page headers
  - Box, Region, and Vendor spawn pages now have map selector in header
  - Cleaner NavMenu without map-specific controls
  - Consistent placement across spawn editor pages
- **NavMenu Reordered** - Logical grouping of features
  - Order: Spawn Packs → Spawn Section → Settings → Instructions → Theme
  - Spawn Packs at top for quick access
  - Settings/Instructions grouped together

#### Styling
- **Tile Name Color** - Blue (#6ea8fe) in frequency card header only
  - Matches frequency theme for consistency
  - Proper scoping to avoid affecting other elements
- **NavMenu Spacing** - Fixed gap between active pack and nav items
  - Added margin-bottom to active-pack-header

### Fixed
- **CSS Warning** - Removed invalid `crisp-edges` value from image-rendering
  - Changed to standard `-webkit-optimize-contrast` and `pixelated`
- **Frequency Enum Accessibility** - Changed from internal to public
  - Required for component parameter binding

### Technical
- Registered new services in MauiProgram.cs as singletons
- MainPage.xaml.cs calls Utility.SetServices() during startup
- Added map-header-select styling in app.css
- Build verified with .NET 10 - no warnings or errors

#### XML Spawner Management
- **Add XML Spawner** - Create new XML spawners directly from the map editor
  - Right-click context menu option on Box Spawn and Region Spawn pages
  - Side-by-side modal layout: bestiary list (left) + selected spawns (right)
  - HomeRange slider (10-250 tiles) for spawn radius configuration
  - MaxCount slider (1-100) for maximum creature count
  - Click-to-add from bestiary with instant visual feedback
  - Favorites support in bestiary for quick access
- **Delete XML Spawner** - Remove XML spawners via map editor
  - Right-click context menu option when clicking on existing spawner
  - Confirmation modal showing spawner details before deletion
  - Server command integration for immediate removal

#### Light/Dark Theme Polish
- **MiniMapComponent** - Full light theme support
  - Header adapts to theme (light gray in light mode)
  - Body background theme-aware
  - SVG icons switch between white (dark) and dark (light) fills
  - Map icon uses appropriate goldenrod shade per theme
- **SpawnPacksComponent** - Complete light theme support
  - Stat tiles, submission cards, dashboard footer
  - Modal dialogs with proper theming
  - Toggle buttons (Approved/Imported) with theme variants
  - Pack description areas with proper backgrounds
- **VendorSpawnComponent** - Full theme support
  - Spawn item lists with proper colors
  - Location items and focused states
  - Minimap body and dark header variants
  - Search input styling per theme
- **BoxSpawn & RegionSpawn Lists** - Light theme support
  - Spawn item backgrounds and borders
  - Hover and selected states
  - Text muted colors
  - Search inputs with theme-aware styling

### Changed

#### Navigation & Branding
- **Logo Navigation** - Clicking the logo now navigates to Home (replaces Home button)
- **Home Button Removed** - Freed up nav slot for future feature
  - Settings is now the first navigation item
  - Logo serves as Home navigation
- **Brand Area Polish** - Improved light theme appearance
  - Uses neutral dark gray (#3a3a3a) instead of near-black
  - Logo maximized to 5.5rem height (from 80px)
  - Reduced padding for better logo prominence
  - Perfectly centered within brand element

#### Theme Consistency
- **Centralized Theme Utilities** - Added to app.css
  - Light theme text-muted, heading, and text-secondary colors
  - Badge adjustments for proper contrast
- **Minimap Styling** - Unified across all spawn editor pages
  - Dark header styling now theme-aware
  - Consistent icon treatment across pages

### Fixed
- **Minimap stuck in dark mode** - All three spawn pages now properly theme
- **Spawn lists not theming** - Box and Region spawn lists now respond to theme changes
- **Logo off-center** - Removed padding that caused horizontal offset
- **Brand area too dark in light mode** - Now uses softer gray for better harmony

### Technical
- `AddXmlSpawnerModal.razor` with HomeRange/MaxCount sliders and side-by-side layout
- `DeleteXmlSpawnerModal.razor` for spawner removal confirmation
- `XmlSpawnerCommandService` for server communication
- Light theme CSS sections added to:
  - `MiniMapComponent.razor.css`
  - `SpawnPacksComponent.razor.css`
  - `VendorSpawnComponent.razor.css`
  - `BoxSpawnComponent.razor.css`
  - `RegionSpawnComponent.razor.css`
- Removed unused `.bi-house-door-fill-nav-menu` icon definitions
- Build verified with .NET 10 - no warnings or errors

## [2.0.0.9] - 2026-03-03

### Added

#### FTP Remote Server Integration
- **FTP Sync Feature** - New third option for server integration alongside "Link Local" and "Manual"
  - Upload spawn data (.bin files) to remote servers via FTP
  - Download reference data (.txt files) from remote servers
  - Full sync option (download then upload)
  - Real-time progress reporting with per-file status
- **Auto-Detect Path** - Automatically searches common server paths for `Data/UORespawn/` folder
  - Searches home directory, `/home`, `/var`, `/srv`, and common ServUO paths
  - One-click path discovery for easier setup
- **Cancellation Support** - All FTP operations can be cancelled mid-transfer
  - Graceful handling with partial completion reporting
  - Cancel button in sync modal

#### File-Based Account System (Security)
- **Local App Accounts** - Create accounts without passwords (just friendly names)
- **User-Controlled Storage** - FTP credentials stored in user's chosen folder, not in app
- **SRP Security Model** - App stores only folder paths; delete folder = data gone
- **No Cloud/Server** - Zero external account storage, full user control

#### Architecture Improvements
- **ErrorHandler Utility** - Centralized error handling with:
  - `Handle()` / `HandleSilent()` - Consistent logging patterns
  - `TryExecute()` / `TryExecuteAsync()` - Safe execution wrappers
  - `GetFriendlyMessage()` - User-friendly exception messages
- **ConfigurationValidator** - Startup validation that:
  - Creates missing required folders automatically
  - Validates server link if configured
  - Provides diagnostic summary for troubleshooting
  - Logs warnings for configuration issues

#### UI/UX Improvements
- **Scroll-to-Top Button** - Added golden glowing button to Settings page (matches Instructions)
- **Page Scroll Reset** - Settings and Instructions pages now scroll to top on navigation
- **FTP Settings Card** - New card in Settings with account setup and credential management
- **FTP Sync Modal** - Progress modal with file-by-file status and cancel button

### Changed
- **Instructions Page** - Added comprehensive FTP documentation in Section 9 (Server Integration)
  - Step-by-step account setup guide
  - FTP credential configuration
  - Auto-detect path feature explanation
  - Pull/Push sync button documentation
- **Settings Page Layout** - FTP card matches centered styling of other settings cards
- **MauiProgram Startup** - Now uses ConfigurationValidator and ErrorHandler

### Fixed
- **StackOverflowException** - Fixed recursive event loop in FtpSettingsCard credential change handling
- **FTP Card Styling** - Fixed centering to match other Settings cards
- **UpdateChecker HttpClient** - Headers now set once in static constructor (not on every call)
- **DataWatcher Namespace** - Corrected from root `UORespawnApp` to `UORespawnApp.Scripts.Services`

### Technical
- `FtpSyncService` with `CancellationToken` support on all operations
- `FtpConnectionService` wraps FluentFTP with cancellation and progress
- `FtpCredentialService` manages credentials in user folders
- `AccountService` with file-based account persistence
- `UserAccount` entity with JSON serialization
- `FtpCredentials` entity with validation
- Removed unused `using` statements from `SpawnPackEntities.cs`
- Removed unnecessary `partial` keyword from `DataWatcher.cs`
- Build verified with .NET 10 - no warnings or errors

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
