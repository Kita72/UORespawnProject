# Quick Start Guide

## Installation

1. Download the latest release from GitHub
2. Extract the ZIP file to your desired location
3. Run `UORespawnApp.exe` (Windows) or `UORespawnApp.app` (macOS)

## First Time Setup

### 1. Configure Server Integration (Optional)

- Click **Settings** in the left menu
- Click **Browse** under "Server Integration"
- Select your `ServUO\Data\` folder
- Files will now auto-sync!

### 2. Select Your Map

- Use the dropdown in the left navigation
- Choose: Felucca, Trammel, Ilshenar, Malas, Tokuno, or Ter Mur

## Creating Your First Spawn

### Map Spawn (Manual)

1. Click **Map Spawn** in the left menu
2. **Left-click and drag** on the map to draw a box
3. Click the spawn in the right panel to select it
4. Click **Common**, **Uncommon**, or **Rare** button
5. Search and add creatures from the bestiary
6. Click **Save** (auto-saves!)

### World Spawn (Automatic)

1. Click **World Spawn** in the left menu
2. Select a tile type (e.g., "Grass", "Forest")
3. Click **Edit** on Common/Uncommon/Rare
4. Add creatures for animals and/or creatures
5. Applies to ALL maps automatically!

### Static Spawn (Object-Based)

1. Click **Static Spawn** in the left menu
2. Search for a static (e.g., "tree", "rock")
3. Select the static from the list
4. Click **Edit** to add creatures
5. Spawns near those objects in-game!

## Common Tasks

### Navigate the Map
- **Pan:** Right-click and drag
- **Jump:** Click on mini-map
- **Reset:** Click reset button in mini-map

### View XML Spawners
- Toggle **XML** button in mini-map (green = on)
- Shows default spawner locations

### View Spawn Heatmap
- Toggle **Spawns** button in mini-map
- Requires server spawn statistics

### Customize Appearance
- Go to **Settings** ? "Spawn Box Appearance"
- Change color, line thickness, brightness

### Add Custom Creatures
- Go to **Settings** ? "Bestiary"
- Type creature name and click **Add**
- Use in any spawn system!

## Special NPCs

Add these from the bestiary for special effects:

- `TownNPC` - Random town citizens
- `WorldNPC` - Road travelers
- `AmbushNPC` - Brigand ambush trigger
- `FireEffectNPC` - Fire damage tile
- `GlowEffectNPC` - Healing tile
- And more!

## Tips

- ? Everything auto-saves
- ? Use priority system for layered dungeons
- ? Combine spawn systems for variety
- ? Test on your server before going live
- ? Use mini-map for quick navigation

## Keyboard Shortcuts

- **Left Mouse:** Draw spawn box (Map view)
- **Right Mouse:** Pan map
- **Ctrl+Click:** Quick select (mini-map)

## Troubleshooting

### Files not syncing?
- Check Settings ? Server Integration path
- Ensure folder exists and has write permissions
- Windows/macOS only - other platforms need manual copy

### Map not loading?
- Ensure .bmp files are in `Data` folder
- Check file dimensions match map requirements
- See Settings ? Custom Maps for specs

### Spawns not appearing in-game?
- Use `[UORespawn reload` command
- Check spawn files in ServUO Data folder
- Verify UORespawnSystem.cs is in Scripts

## Need More Help?

- ?? Full instructions in-app: Click **Instructions** button
- ?? Wilson's Profile: [servuo.dev/members/wilson.12169](https://www.servuo.dev/members/wilson.12169)
- ?? ServUO Forums: [www.servuo.com](https://www.servuo.com)
- ?? Report Issues: [GitHub Issues](https://github.com/yourusername/UORespawn/issues)

---

**Happy Spawning! ??**
