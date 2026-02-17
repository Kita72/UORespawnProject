# UORespawn v2.0 - Professional Spawn Management System

## 🎯 Overview

**UORespawn** is a high-performance, production-ready spawn system for ServUO servers featuring:
- **Player-centric spawning** - Dynamic mob generation around active players
- **Intelligent recycling** - Up to 60% mob reuse for optimal performance
- **Binary data loading** - Fast, efficient spawn data from editor-generated files
- **Comprehensive metrics** - Real-time performance monitoring and analysis
- **Professional admin GUI** - Modern in-game control panel with live statistics
- **Edge case tracking** - Automatic cleanup of orphaned spawns on server restart

**Status:** ✅ Production-Ready | **Version:** 2.0 | **Target Framework:** .NET Framework 4.8

---

## ⚡ Quick Start

### Essential Commands

```bash
[SpawnAdmin           # Open professional admin GUI (recommended)
[SpawnStatus          # Quick system health check
[SpawnMetrics         # View detailed performance stats
[SpawnReload          # Reload spawn data from binary files
```

### First-Time Setup

1. **Generate spawn data** - Use the **UORespawn Editor** to create binary files:
   - `UOR_SpawnSettings.bin` - System configuration
   - `UOR_BoxSpawn.bin` - Box-based spawn regions
   - `UOR_TileSpawn.bin` - Tile-type spawn definitions
   - `UOR_RegionSpawn.bin` - Named region spawn assignments

2. **Place binary files** - Copy to `Data/UOR_DATA/` folder

3. **Start server** - UORespawn auto-loads on startup

4. **Verify operation** - Use `[SpawnAdmin` or `[SpawnStatus`

5. **Monitor performance** - Check `[SpawnMetrics` after 10-15 minutes

---

## 📖 System Architecture

### Core Components

| **Component** | **Purpose** | **Key Features** |
|--------------|-------------|------------------|
| **UORespawnCore** | Central orchestration | Pause/Resume, Player tracking, Queue management |
| **SpawnTimer** | 50ms tick processing | Batch processing (5 players/tick), Queue spawning |
| **SpawnFactory** | Spawn selection logic | Box→Region→Tile→Debug hierarchy |
| **UORespawnDataBase** | Binary data loading | Settings, Box, Tile, Region data with validation |
| **UORespawnSettings** | Configuration manager | Dynamic (file) + Static (hardcoded) settings |
| **UORespawnUtility** | Helper functions | Location finding, spawn creation, validation |
| **UORespawnTracker** | Edge case cleanup | Auto-delete orphaned spawns on server restart |

### Service Architecture

| **Service** | **Timer** | **Purpose** |
|------------|-----------|-------------|
| **SpawnMetricsService** | Event-driven | Performance tracking, statistics, reports |
| **SpawnRecycleService** | On-demand | Mob recycling pool (up to 50,000 mobs) |
| **SpawnDistanceService** | 1 second | Distance calculations for cleanup |
| **SpawnCleanupService** | 10 seconds | Cleanup cycle (recycle or delete) |
| **SpawnDebugService** | On-demand | In-memory debug log with file flushing |

### Spawn Workers

| **Worker** | **Priority** | **Function** |
|-----------|-------------|-------------|
| **BoxSpawner** | 1st (Highest) | Priority-based rectangular spawn regions |
| **RegionSpawner** | 2nd | Named region spawn assignments (e.g., "Britain", "Dungeon Despise") |
| **TileSpawner** | 3rd | Tile-type spawn definitions (e.g., Grass, Snow, Swamp) |
| **Debug Fallback** | 4th (Lowest) | PlaceHolder spawns for staff when ENABLE_DEBUG=true |

---

## 🔄 Spawn Processing Flow

### Every 50ms (Main Timer)
```
1. Process BATCH_SIZE (5) players from player list
2. For each player:
   ├─ Check if system is PAUSED → skip
   ├─ Check queue size (skip if ≥5 queued)
   ├─ Apply SCALE_SPAWN multiplier (if enabled)
   ├─ Check spawn count vs MAX_MOBS → skip if at limit
   ├─ Find valid spawn location (MAX_SPAWN_CHECKS attempts)
   ├─ Determine spawn type (Box→Region→Tile→Debug)
   └─ Add to player's spawn queue
3. Process 1 spawn from each player's queue:
   ├─ Try RECYCLE pool first (30-60% hit rate)
   ├─ If no recycle: Create new mob
   ├─ Restore health/mana/stamina (if recycled)
   ├─ MoveToWorld + visual effect
   ├─ Add to spawned list
   ├─ Track serial (for edge case cleanup)
   ├─ Record metrics
   └─ Apply aggression logic (Felucca vs Trammel rules)
```

### Every 1 Second (Distance Service)
```
1. Calculate distance for all spawned mobs
2. Mark mobs as "too far" if beyond MAX_RANGE * 1.5
```

### Every 10 Seconds (Cleanup Service)
```
1. Process all "too far" mobs:
   ├─ Try add to RECYCLE pool (if under limits)
   ├─ If pool full: Delete mob
   └─ Remove from spawned list
2. Record cleanup metrics (time, count)
```

### On Server Shutdown/Crash
```
1. Flush debug log to file
2. Clear all spawned mobs
3. Clear recycle pool
4. Write tracked serials to file (for next startup)
```

### On Server Startup
```
1. Read tracked serials file (UOR_TrackSpawn.txt)
2. Delete all tracked mobs (edge case cleanup)
3. Delete tracking file
4. Load binary spawn data
5. Initialize services
6. Start timer
```

---

## 📊 Binary Data System

### File Structure

UORespawn uses **binary serialization** for fast data loading. All files are created by the **UORespawn Editor** (separate tool).

#### 1. UOR_SpawnSettings.bin
```
SettingsModel:
├─ Version: "2.0.0.1"
├─ MaxMobs: 15
├─ MinRange: 10
├─ MaxRange: 50
├─ MaxCrowd: 1
├─ ChanceWater: 0.5
├─ ChanceWeather: 0.1
├─ ChanceTimed: 0.1
├─ ChanceCommon: 1.0
├─ ChanceUncommon: 0.5
├─ ChanceRare: 0.1
├─ ScaleSpawn: false
├─ EnableRiftSpawn: false
└─ EnableDebug: false
```

#### 2. UOR_BoxSpawn.bin
```
BoxContainer:
├─ Version: "2.0"
└─ BoxData: List<MapBoxData>
    ├─ MapId: 0 (Felucca)
    ├─ MapName: "Felucca"
    └─ BoxSpawns: List<BoxModel>
        ├─ Id: 1
        ├─ SpawnPriority: 1
        ├─ X, Y, Width, Height (Rectangle2D components)
        ├─ WeatherSpawn: None/Rain/Snow/Storm/Blizzard
        ├─ TimedSpawn: None/Witching_Hour/.../Late_at_Night
        └─ 6 spawn lists: Water/Weather/Timed/Common/Uncommon/Rare
```

#### 3. UOR_TileSpawn.bin
```
TileContainer:
├─ Version: "2.0"
└─ TileData: List<MapTileData>
    └─ TileSpawns: List<TileModel>
        ├─ Name: "Grass" / "Snow" / "Swamp" / etc.
        ├─ MapId: 0
        ├─ WeatherSpawn, TimedSpawn
        └─ 6 spawn lists
```

#### 4. UOR_RegionSpawn.bin
```
RegionContainer:
├─ Version: "2.0"
└─ RegionData: List<MapRegionData>
    └─ RegionSpawns: List<RegionModel>
        ├─ Name: "Britain" / "Dungeon Despise" / etc.
        ├─ MapId: 0
        ├─ WeatherSpawn, TimedSpawn
        └─ 6 spawn lists
```

### Region Name Lookup

**Important:** UORespawn only uses **named regions**. The Region.Name must match exactly (case-insensitive).

**Generate region list:**
```bash
[GenRegionList    # Creates UOR_RegionNames.txt with all named regions
```

**How it works:**
1. Editor loads region names from `UOR_RegionNames.txt`
2. Admin assigns spawn lists to specific region names
3. Editor saves region name + spawn lists to `UOR_RegionSpawn.bin`
4. Server loads binary file and looks up regions using `map.Regions.TryGetValue(name)`

---

## ⚙️ Configuration

### Dynamic Settings (Loaded from UOR_SpawnSettings.bin)

| **Setting** | **Default** | **Description** |
|------------|-------------|-----------------|
| `MAX_MOBS` | 15 | Maximum spawns per player |
| `MIN_RANGE` | 10 | Minimum spawn distance (tiles) |
| `MAX_RANGE` | 50 | Maximum spawn radius (tiles) |
| `MAX_CROWD` | 1 | Max mobs at spawn point before "crowded" |
| `CHANCE_WATER` | 0.5 (50%) | Water spawn chance |
| `CHANCE_WEATHER` | 0.1 (10%) | Weather spawn chance |
| `CHANCE_TIMED` | 0.1 (10%) | Time-based spawn chance |
| `CHANCE_COMMON` | 1.0 (100%) | Common spawn chance |
| `CHANCE_UNCOMMON` | 0.5 (50%) | Uncommon spawn chance |
| `CHANCE_RARE` | 0.1 (10%) | Rare spawn chance |
| `ENABLE_SCALE_SPAWN` | false | Player proximity scaling (0.1 multiplier per nearby player) |
| `ENABLE_RIFT_SPAWN` | false | Special rift mob spawns during weather |
| `ENABLE_DEBUG` | false | Staff spawn PlaceHolder mobs |

### Static Settings (Hardcoded in UORespawnSettings.cs)

| **Setting** | **Value** | **Description** |
|------------|-----------|-----------------|
| `INTERVAL` | 50ms | Main timer tick rate |
| `BATCH_SIZE` | 5 | Players processed per tick |
| `DISTANCE_INTERVAL` | 1000ms | Distance service timer |
| `CLEANUP_INTERVAL` | 10 seconds | Cleanup service timer |
| `MAX_RECYCLE_TYPE` | 20 | Max recycled mobs per type |
| `MAX_RECYCLE_TOTAL` | 50,000 | Total recycle pool limit |
| `MAX_SPAWN_CHECKS` | 5 | Location search attempts |
| `MAX_QUEUE_SIZE` | 5 | Max queued spawns per player |

### Tuning Recommendations

**High Population (50+ players):**
```csharp
// Edit UOR_SpawnSettings.bin in editor:
MAX_MOBS = 12           // Reduce per-player spawns
MIN_RANGE = 12          // Spread spawns further
MAX_RANGE = 60          // Larger search area
ENABLE_SCALE_SPAWN = true  // Dynamic scaling

// Static settings in UORespawnSettings.cs:
BATCH_SIZE = 10         // More players per tick
CLEANUP_INTERVAL = 15   // Less frequent cleanup
```

**Low Population (1-20 players):**
```csharp
// Edit UOR_SpawnSettings.bin in editor:
MAX_MOBS = 20           // More spawns per player
MAX_RANGE = 40          // Closer spawn radius

// Static settings in UORespawnSettings.cs:
BATCH_SIZE = 3          // Fewer players per tick (fine-grained)
```

---

## 🎮 Admin GUI Guide

### Opening the Admin Panel
```bash
[SpawnAdmin    # GameMaster+ access
```

### GUI Features

#### System Status Section (Real-time Display)
- **Status Indicator**: ■ RUNNING (green) or ■ PAUSED (red)
- **Active Spawns**: Current mob count in world
- **Active Players**: Players tracked by spawn system
- **Recycle Pool**: Mobs available for reuse
- **Recycle Rate**: Percentage of spawns recycled (30-60% = optimal)
- **Cleanup Time**: Last cleanup cycle duration (target: <5ms)

#### System Control (Administrator Only)
- **Pause/Resume**: Toggle spawning (cleanup continues while paused)
- **Reload Spawn Data**: Reload all 4 binary files without server restart

#### Monitoring & Metrics (GameMaster+)
- **Quick Status Report**: Chat message with key statistics
- **Full Metrics Report**: Console output with detailed breakdown
- **Player Metrics**: Per-player spawn statistics (Administrator only)

#### Advanced Options (Administrator Only)
- **Reset Metrics**: Clear all statistics (start fresh)
- **Debug Toggle**: Enable/disable debug mode (shows ON/OFF status)
- **Clear Spawns**: Delete all active spawns (requires confirmation)
- **Clear Pool**: Delete all recycled mobs (instant)

---

## 🎯 Command Reference

### Admin Commands (GUI Recommended)

| **Command** | **Access** | **Description** |
|------------|------------|-----------------|
| `[SpawnAdmin` | GameMaster | Open professional admin control panel (recommended interface) |
| `[SpawnStatus` | GameMaster | Quick health check with key statistics |
| `[SpawnMetrics` | Administrator | Full metrics report to console |
| `[SpawnPause` | Administrator | Pause spawning (cleanup continues) |
| `[SpawnResume` | Administrator | Resume spawning |
| `[SpawnReload` | Administrator | Reload all binary data files |

### Utility Commands

| **Command** | **Access** | **Description** |
|------------|------------|-----------------|
| `[DebugRespawn` | Administrator | Toggle debug mode ON/OFF |
| `[PushRespawn` | Administrator | Force immediate spawn update for all players |
| `[ClearRespawn` | Administrator | Clear all spawns (requires confirmation) |
| `[TrackRespawn` | Administrator | Show current spawn statistics |
| `[SpawnRecycleStats` | GameMaster | Show recycle pool statistics |
| `[ClearRecycle` | Administrator | Clear recycle pool (delete all recycled mobs) |

### Data Generation Commands (For Editor Setup)

| **Command** | **Access** | **Description** |
|------------|------------|-----------------|
| `[GenRegionList` | Administrator | Generate list of all named regions → `UOR_RegionNames.txt` |
| `[GenRespawnList` | Administrator | Generate list of all mob types → `UOR_MobList.txt` |
| `[GenSpawnerList` | Administrator | Generate spawner statistics → `UOR_SpawnerList.txt` |
| `[PushRespawnStats` | Administrator | Save spawn statistics → `UOR_STATS/` folder |

---

## 📈 Performance Targets

### Recycle Rate (Efficiency)
- ✅ **Excellent**: ≥40% - Optimal reuse, minimal memory pressure
- ✔️ **Good**: 30-40% - Healthy performance, good efficiency
- ⚠️ **Fair**: 20-30% - Acceptable but could improve
- ❌ **Poor**: <20% - Too many deletions, investigate spawn patterns

### Cleanup Time (Performance)
- ✅ **Excellent**: ≤5ms - No performance impact
- ✔️ **Good**: 5-10ms - Minor impact, acceptable
- ⚠️ **Fair**: 10-20ms - Noticeable impact, consider tuning
- ❌ **Slow**: >20ms - Performance issue, reduce spawns or increase interval

### Active Spawns Per Player
- **Target**: 12-15 spawns per active player
- **Minimum**: 8-10 (feels empty)
- **Maximum**: 18-20 (can cause crowding)
- **Scaling**: Use `ENABLE_SCALE_SPAWN` for dynamic adjustment

### Spawn Rate (Steady State)
- **Normal**: 0.5-1.5 spawns/second per player
- **Peak**: 2-3 spawns/second (player moving through new areas)
- **Low**: <0.3 spawns/second (check settings or data)

---

## 🔧 Troubleshooting

### No Spawns Appearing

**Symptoms:** Players report no mobs spawning, world feels empty

**Diagnostic Steps:**
1. Check system status:
   ```bash
   [SpawnAdmin    # Open GUI and check status indicator
   [SpawnStatus   # Console output
   ```

2. Verify system is running:
   - GUI shows "■ RUNNING" in green?
   - If "■ PAUSED" in red, use `[SpawnResume`

3. Check binary files exist:
   ```
   Data/UOR_DATA/
   ├─ UOR_SpawnSettings.bin  (required)
   ├─ UOR_BoxSpawn.bin       (optional but recommended)
   ├─ UOR_TileSpawn.bin      (optional but recommended)
   └─ UOR_RegionSpawn.bin    (optional but recommended)
   ```

4. Check spawn data loaded:
   - GUI shows "Active Players" > 0?
   - Console shows "Spawn Loaded..." message on startup?

5. Reload if necessary:
   ```bash
   [SpawnReload    # Reloads all 4 binary files
   ```

6. Enable debug mode temporarily:
   ```bash
   [DebugRespawn    # Toggle ON
   ```
   - Staff players should spawn PlaceHolder mobs (visible spawns)

**Common Causes:**
- ❌ Binary files missing or in wrong folder
- ❌ System paused
- ❌ No players in spawn range
- ❌ All spawn lists empty in binary data
- ❌ ENABLE_DEBUG=false and no mob types configured

---

### Low Recycle Rate (<20%)

**Symptoms:** Metrics show <20% recycle rate, lots of "NEW SPAWN" console messages

**Diagnostic Steps:**
1. Check current rate:
   ```bash
   [SpawnMetrics    # Look for "Recycle Rate" percentage
   ```

2. Check pool size:
   ```bash
   [SpawnRecycleStats    # Shows pool breakdown by mob type
   ```

3. Check pool limits:
   - Open `UORespawnSettings.cs`
   - Current: `MAX_RECYCLE_TOTAL = 50000` (very high, good)
   - Current: `MAX_RECYCLE_TYPE = 20` per mob type

**Common Causes:**
- ❌ Mobs dying too quickly (PvP shard, fast kill rate)
- ❌ Spawn types too diverse (100+ different mob types)
- ❌ MAX_RECYCLE_TYPE too low (increase to 30-50 for diverse spawns)
- ❌ Cleanup interval too short (mobs deleted before reuse)

**Solutions:**
1. Increase per-type limit:
   ```csharp
   // UORespawnSettings.cs
   MAX_RECYCLE_TYPE = 30;    // Up from 20
   ```

2. Increase cleanup interval:
   ```csharp
   // UORespawnSettings.cs
   CLEANUP_INTERVAL = 15;    // Up from 10 seconds
   ```

3. Check spawn distribution:
   ```bash
   [GenRespawnList    # Shows all mob types being spawned
   ```
   - If 200+ types, recycle pool fragments (expected)

---

### Slow Cleanup Performance (>20ms)

**Symptoms:** Metrics show cleanup time >20ms, potential server lag

**Diagnostic Steps:**
1. Check cleanup metrics:
   ```bash
   [SpawnMetrics    # Look for "Last Cleanup Time" and "Avg Cleanup Time"
   ```

2. Check active spawns:
   ```bash
   [SpawnStatus    # Look for "Active Spawns" count
   ```

3. Calculate spawns per player:
   - Active Spawns ÷ Active Players = Spawns Per Player
   - Target: 12-15 spawns/player
   - If >20 spawns/player, system overloaded

**Common Causes:**
- ❌ Too many active spawns (>1000 on low-end hardware)
- ❌ MAX_MOBS set too high
- ❌ Too many players being processed
- ❌ Hardware limitations (slow CPU)

**Solutions:**
1. Reduce per-player spawns:
   ```csharp
   // Edit in UORespawn Editor: UOR_SpawnSettings.bin
   MAX_MOBS = 10    // Down from 15
   ```

2. Increase cleanup interval:
   ```csharp
   // UORespawnSettings.cs
   CLEANUP_INTERVAL = 15    // Up from 10 seconds
   ```

3. Reduce batch size (less aggressive):
   ```csharp
   // UORespawnSettings.cs
   BATCH_SIZE = 3    // Down from 5
   ```

4. Hardware upgrade:
   - Consider dedicated server
   - More CPU cores
   - Faster single-thread performance

---

### Region Spawns Not Working

**Symptoms:** Region-based spawns not appearing, tile/box spawns work fine

**Diagnostic Steps:**
1. Verify region names:
   ```bash
   [GenRegionList    # Generates Data/UOR_DATA/UOR_RegionNames.txt
   ```
   - Check if your region names match exactly (case-insensitive)

2. Check binary file:
   - Verify `UOR_RegionSpawn.bin` exists in `Data/UOR_DATA/`

3. Enable debug and test:
   ```bash
   [DebugRespawn    # Enable debug mode
   ```
   - Walk around region
   - Staff should spawn PlaceHolders if region data valid

**Common Causes:**
- ❌ Region name typo in editor (e.g., "Brit" vs "Britain")
- ❌ Region name null (some ServUO regions don't have names)
- ❌ RegionSpawn.bin file not loaded or corrupted
- ❌ Wrong map ID in binary file

**Solutions:**
1. Regenerate region list:
   ```bash
   [GenRegionList    # Creates fresh list
   ```

2. Reload editor with new list

3. Verify region names in editor match server exactly

4. Test with simple region first (e.g., "Britain" on Trammel)

---

## 📊 Performance Metrics Example

### Console Output ([SpawnMetrics])

```
═══════════════════════════════════════════════════════════════
                UORespawn Performance Metrics
═══════════════════════════════════════════════════════════════

SPAWN STATISTICS
─────────────────────────────────────────────────────────────
Total Spawns:              1,234    (Since last reset)
Total New Created:           678    (55.0% new)
Total Recycled:              556    (45.0% recycled) ✔️ Good
Total Deleted:               892

Active Spawns:               147    (Currently in world)
Recycle Pool:                 89    (Available for reuse)
Active Players:               10    (Tracked by system)

PERFORMANCE
─────────────────────────────────────────────────────────────
Recycle Rate:              45.0%    ✅ Excellent (≥40%)
Avg Cleanup Time:          2.3 ms   ✅ Excellent (≤5ms)
Last Cleanup Time:         1.8 ms
Cleanup Cycles:              342

QUEUE STATISTICS
─────────────────────────────────────────────────────────────
Total Queued:              1,456
Peak Queue Size:               8
Current Queue Size:            3

SESSION INFO
─────────────────────────────────────────────────────────────
Session Start:          12:34:56 PM
Session Duration:       1h 23m 45s
Last Reset:             12:34:56 PM
═══════════════════════════════════════════════════════════════
```

### Admin GUI Display

```
╔═══════════════════════════════════════════════════════╗
║           UORespawn Admin Control Panel               ║
║        Professional Spawn Management System           ║
╠═══════════════════════════════════════════════════════╣
║ ═══ System Status ═══                                 ║
║ ■ RUNNING     Active: 147        Players: 10          ║
║ Pool: 89      Rate: 45.0%        Cleanup: 2.3ms       ║
╠════════════════════════╦══════════════════════════════╣
║ ═══ System Control ═══ ║ ═══ Monitoring & Metrics ═══ ║
║ □ Pause System         ║ □ Quick Status Report        ║
║ □ Reload Spawn Data    ║ □ Full Metrics Report        ║
║ Administrator Access   ║ □ Player Metrics             ║
╠════════════════════════╩══════════════════════════════╣
║ ═══ Advanced Options ═══                              ║
║ □ Reset   □ Debug (OFF)  □ Clear Spawns  □ Clear Pool ║
║      ⚠ Administrator Only - Use with Caution ⚠     ║
╠═══════════════════════════════════════════════════════╣
║ Close        UORespawn v2.0 Production        Refresh ║
╚═══════════════════════════════════════════════════════╝
```

---

## 🏆 Optimization Achievements

### Architecture Improvements
- ✅ **Service-Based Design** - Separated concerns (Metrics, Recycle, Cleanup, Debug, Distance)
- ✅ **Binary Serialization** - Fast load times (JSON/CSV eliminated)
- ✅ **Spatial Queries** - O(n) distance checks using Map.GetMobilesInRange()
- ✅ **Batch Processing** - Configurable player throughput (5 players/50ms)
- ✅ **Queue System** - Decoupled spawn decision from spawn execution

### Memory Optimizations
- ✅ **Intelligent Recycling** - 30-60% reuse rate, reduced GC pressure
- ✅ **Pool Limits** - 50,000 total / 20 per type prevents memory bloat
- ✅ **Distance Service** - Pre-calculates distances once per second
- ✅ **Type Caching** - Reflection results cached (Dictionary<string, Type>)

### Performance Optimizations
- ✅ **Cleanup Throttling** - 10-second cycle reduces CPU spikes
- ✅ **Dynamic Scaling** - ENABLE_SCALE_SPAWN adjusts to player proximity
- ✅ **Pause/Resume** - Cleanup continues while spawning paused
- ✅ **Console Close Handler** - Graceful shutdown cleanup

### Debug & Monitoring
- ✅ **In-Memory Log Buffer** - 10,000 entries, file flush on demand
- ✅ **Color-Coded Console** - System vs Debug message routing
- ✅ **Comprehensive Metrics** - Spawn/Recycle/Cleanup/Queue tracking
- ✅ **Per-Player Metrics** - Individual spawn statistics
- ✅ **Admin GUI** - Real-time monitoring with professional interface

### Edge Case Handling
- ✅ **Tracker System** - Auto-cleanup orphaned spawns on restart
- ✅ **Version Validation** - Binary file version checking
- ✅ **Region Name Lookup** - Dictionary-based O(1) region resolution
- ✅ **Graceful Degradation** - Missing files logged but don't crash

---

## 🚀 Version History

### v2.0 - Production Release (Current)
**Major Refactor:**
- Renamed from "SpawnSystem" to "UORespawn" (professional branding)
- Complete binary serialization system (Editor integration)
- Professional admin GUI with real-time stats
- UORespawnTracker edge case cleanup system
- Service-based architecture (5 independent services)
- Region name lookup optimization (O(1) dictionary access)

**New Features:**
- Binary file loading (Settings/Box/Tile/Region)
- Admin GUI ([SpawnAdmin])
- Edge case tracking (UOR_TrackSpawn.txt)
- Dynamic scaling (ENABLE_SCALE_SPAWN)
- Console close handler (Windows API integration)
- In-memory debug log buffer

**Performance:**
- 50,000 recycle pool capacity (up from 200)
- Spatial query optimization
- Distance service separation
- Type reflection caching

### v1.0 - Initial Release
- Player-centric spawning core
- Intelligent recycling system
- Metrics service
- Basic commands
- CSV/JSON data loading (deprecated)

---

## 📞 Support & Resources

### Getting Help

1. **Check Admin GUI**: `[SpawnAdmin` - Most issues visible in real-time display
2. **Review Console**: Color-coded messages explain most problems
3. **Check Metrics**: `[SpawnMetrics` - Detailed performance breakdown
4. **Enable Debug**: `[DebugRespawn` - PlaceHolder spawns for troubleshooting

### Common Questions

**Q: Can I use this with XMLSpawner?**  
A: Yes! UORespawn is independent and complementary. XMLSpawner handles static spawns, UORespawn handles dynamic player-centric spawns.

**Q: How do I create the binary files?**  
A: Use the **UORespawn Editor** (separate Windows Forms application). It generates all 4 .bin files.

**Q: Do I need all 4 binary files?**  
A: Settings file required, others optional. System works with any combination of Box/Tile/Region files.

**Q: Can I edit spawn data in-game?**  
A: Not yet. Binary save methods are placeholders for future in-game editing feature.

**Q: What happens if a binary file is corrupted?**  
A: System logs error and continues with remaining files. Check console for RED error messages.

**Q: How often should I regenerate region lists?**  
A: Only when you add new regions to your server or update ServUO version.

**Q: Can I pause spawning without stopping cleanup?**  
A: Yes! `[SpawnPause` stops new spawns but cleanup continues (intended behavior).

**Q: What's the difference between "Clear Spawns" and "Clear Pool"?**  
A: **Clear Spawns** deletes active mobs in world. **Clear Pool** deletes recycled mobs in storage.

---

## 🎯 Best Practices

### Production Deployment

1. **Generate binary files** using Editor in development/test environment
2. **Test thoroughly** with `ENABLE_DEBUG=true` (staff spawn PlaceHolders)
3. **Monitor metrics** for 24 hours on test server
4. **Tune settings** based on metrics (recycle rate, cleanup time)
5. **Deploy to production** with `ENABLE_DEBUG=false`
6. **Monitor first week** - adjust MAX_MOBS if needed

### Performance Tuning

1. **Start conservative**: MAX_MOBS=12, see how it performs
2. **Monitor recycle rate**: Aim for 30-60%
3. **Watch cleanup time**: Should stay <10ms
4. **Adjust gradually**: Change one setting at a time
5. **Give it time**: 1-2 hours for metrics to stabilize

### Data Management

1. **Backup binary files** before regenerating
2. **Version control** your Editor project files
3. **Document changes** - use version string in SettingsModel
4. **Test region names** with `[GenRegionList` after ServUO updates
5. **Clean old stats** - Auto-cleanup after 7 days (configurable)

---

**System Status:** ✅ Production-Ready | **Version:** 2.0 | **Framework:** .NET 4.8 | **Performance:** Optimized

**Documentation Complete** | **Maintained By:** Wilson | **Last Updated:** 2026
