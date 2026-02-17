# UORespawn Editor

<p align="center">
  <img src="UORespawnApp/Resources/AppIcon/appicon.svg" alt="UORespawn Logo" width="200"/>
</p>

<p align="center">
  <strong>A powerful spawn management tool for Ultima Online servers running ServUO</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#download">Download</a> •
  <a href="#installation">Installation</a> •
  <a href="#usage">Usage</a> •
  <a href="#building">Building</a> •
  <a href="#support">Support</a>
</p>

---

## 📌 About

UORespawn is a modern .NET MAUI application that provides a comprehensive visual interface for creating and managing spawn systems in Ultima Online ServUO servers. It offers three complementary spawn methods to populate your world with creatures and NPCs.

**Version:** 2.0  
**Platform:** Windows, macOS  
**Framework:** .NET 10 MAUI with Blazor  
**License:** MIT

---

## ✨ Features

### 🗺️ Map Spawn Editor
- Visual map-based spawn box creation
- Draw spawn areas with left-click and drag
- Pan the map with right-click and drag or WASD/Arrow keys
- Zoom toggle (1x actual size / 2x zoomed view)
- Mini-map with click-to-navigate
- Priority-based spawn layering
- Six frequency tiers (Common, Uncommon, Rare, Water, Weather, Timed)
- **Interactive XML spawner overlay** - hover to highlight, click for tooltip with location and home range
- **Interactive server spawn statistics** - dwell-based tooltips showing player name, location, and total spawn events

### 🏰 Region Spawn System
- Spawn creatures by named server regions
- Configure spawns for regions like "Britain", "Dungeon Despise", etc.
- Automatic region boundary detection
- Per-region creature assignments
- Simplifies large area spawn management

### 🌍 World Spawn System
- Tile-based automatic spawning
- Configure spawns for terrain types (grass, forest, water, desert, etc.)
- Separate animal and creature spawns
- Six frequency tiers for precise control
- Applies globally to all maps

### ⚙️ Server Integration
- **Binary Serialization** - Fast, efficient .bin file format
- **Auto-Sync** - Files automatically sync to ServUO Data folder
- **Professional Admin GUI** - In-game control panel with `[SpawnAdmin`
- **Intelligent Recycling** - Up to 60% mob reuse for optimal performance
- **Real-time Metrics** - Monitor spawn performance with `[SpawnMetrics`

### 🎨 Additional Features
- **Spawn Packs** - Import, export, and share spawn configurations
- Pack Dashboard with detailed statistics
- Dark theme UI
- Custom map image replacement
- Bestiary management (600+ creatures)
- Special NPCs (TownNPC, AmbushNPC, Effect NPCs)
- Cross-platform (Windows and macOS)
- Info icons for contextual help throughout

---

## 📥 Download

### Latest Release
Download the latest version from the [Releases](https://github.com/Kita72/UORespawnProject/releases) page.

### Platform Support
- **Windows 10/11** - x64
- **macOS** - Apple Silicon & Intel

---

## 🚀 Installation

### Windows
1. Download the Windows release (.zip)
2. Extract to your preferred location
3. Run `UORespawnApp.exe`
4. (Optional) Configure your ServUO Data folder in Settings for auto-sync

### macOS
1. Download the macOS release (.app)
2. Move to Applications folder
3. Run the application
4. (Optional) Configure your ServUO Data folder in Settings for auto-sync

### First Run

1. The app will create a `Data` folder for spawn files
2. Map images (Map0-Map5.bmp) should be in the `Data` folder
3. Configure your spawn settings in the Settings page
4. Start creating spawns!

---

## 📖 Usage

### Quick Start

1. **Select a Map** - Use the dropdown in the left navigation
2. **Choose a Spawn Type:**
   - **Map Spawn** - Draw boxes directly on the map
   - **Region Spawn** - Assign creatures to named regions
   - **World Spawn** - Configure tile-based automatic spawns
3. **Add Creatures** - Select from the bestiary
4. **Configure Settings** - Adjust spawn parameters
5. **Auto-Sync** - Files automatically save to your ServUO folder

### Map Spawn Controls

- **Left-Drag:** Draw spawn box
- **Right-Drag:** Pan the map
- **WASD/Arrow Keys:** Pan the map (supports diagonal movement)
- **Zoom Button:** Toggle 1x / 2x zoom
- **Mini-Map Click:** Jump to location
- **XML Toggle:** Show/hide XML spawners (hover to highlight, click for tooltip)
- **Spawns Toggle:** Show/hide server spawn stats (hover for player info)

### Special NPCs

Add these predefined NPCs from the bestiary:
- `TownNPC`, `WorldNPC`, `AmbushNPC`
- Effect NPCs: `FireEffectNPC`, `PoisonEffectNPC`, `GlowEffectNPC`, etc.

### Binary Files Generated

- `UOR_SpawnSettings.bin` - System configuration
- `UOR_BoxSpawn.bin` - Map spawn boxes
- `UOR_TileSpawn.bin` - World tile spawns
- `UOR_RegionSpawn.bin` - Named region spawns

---

## 🔨 Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2026](https://visualstudio.microsoft.com/) or [Visual Studio 2022](https://visualstudio.microsoft.com/) (v17.14 or later)
- Workloads:
  - .NET Multi-platform App UI development
  - ASP.NET and web development

### Clone and Build

```bash
git clone https://github.com/Kita72/UORespawnProject.git
cd UORespawnProject
dotnet restore
dotnet build
```

### Run

```bash
cd UORespawnApp
dotnet run
```

### Platform-Specific Builds

**Windows:**
```bash
dotnet publish -f net10.0-windows10.0.19041.0 -c Release
```

**macOS:**
```bash
dotnet publish -f net10.0-maccatalyst -c Release
```

---

## 📁 Project Structure

```
UORespawnProject/
├── UORespawnApp/              # Main MAUI application
│   ├── Components/            # Blazor components
│   │   ├── Layout/           # Navigation and layout
│   │   └── Controls/         # Reusable components
│   │       ├── BoxSpawnComponent.razor
│   │       ├── RegionSpawnComponent.razor
│   │       ├── TileSpawnComponent.razor
│   │       ├── SpawnPacksComponent.razor
│   │       ├── SettingsComponent.razor
│   │       └── InstructionsComponent.razor
│   ├── Scripts/              # C# utility scripts
│   ├── UORespawnSystem/      # Server-side scripts for ServUO
│   ├── wwwroot/              # Web assets (JS, CSS)
│   └── Resources/            # App resources
├── README.md
├── LICENSE
└── UORespawnProject.sln
```

---

## 💬 Support

### Need Help?

- **Wilson (Creator):** [ServUO Profile](https://www.servuo.com/members/wilson.12169/)
- **ServUO Community:** [www.servuo.com](https://www.servuo.com)
- **Issues:** [GitHub Issues](https://github.com/Kita72/UORespawnProject/issues)

### Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- ServUO team for the amazing server platform
- Ultima Online community for continued support
- All contributors and testers

---

<p align="center">
  Made with ❤️ for the ServUO community
</p>

<p align="center">
  <a href="#top">Back to Top</a>
</p>
