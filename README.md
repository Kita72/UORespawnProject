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

**Version:** 2.0.0.1  
**Platform:** Windows, macOS  
**Framework:** .NET 10 MAUI with Blazor  
**License:** MIT

---

## ✨ Features

### 🗺️ **Map Spawn Editor**
- Visual map-based spawn box creation
- Draw spawn areas with left-click and drag
- Pan the map with right-click and drag
- Mini-map with click-to-navigate
- Priority-based spawn layering
- Timed spawn support (day/night cycles)
- XML spawner location overlay
- Server spawn heatmap visualization

### 🌍 **World Spawn System**
- Tile-based automatic spawning
- Configure spawns for terrain types (grass, forest, water, desert, etc.)
- Separate animal and creature spawns
- Three frequency tiers: Common, Uncommon, Rare
- Applies globally to all maps

### 🏛️ **Static Spawn System**
- Spawn creatures near world objects
- Configure spawns for thousands of static items
- Search functionality for easy static selection
- Themed spawns (graveyards, forges, altars, etc.)

### ⚙️ **Advanced Settings**
- Customizable spawn parameters (range, max mobs, crowd control)
- Adjustable spawn chances for all frequency tiers
- Spawn box appearance customization
- Custom map image support (with backup/restore)
- Bestiary management with special NPCs

### 🔄 **Server Integration**
- Auto-sync with ServUO Data folder (Windows/macOS)
- File watcher for real-time updates
- Manual export option for all platforms
- Supports all standard UO maps (Felucca, Trammel, Ilshenar, Malas, Tokuno, Ter Mur)

### 👥 **Special NPCs**
- **TownNPC** - Random NPCs for ambient town life
- **WorldNPC** - Random travelers for roads
- **AmbushNPC** - Player-triggered brigand ambushes
- **Effect NPCs** - Tile-reactive damage/healing effects (Fire, Poison, Electric, Healing, etc.)

---

## 📥 Download

**Latest Release:** [v2.0.0.1](https://github.com/Kita72/UORespawnProject/releases/latest)

Download the latest version from the [Releases](https://github.com/Kita72/UORespawnProject/releases) page.

### Requirements
- **OS:** Windows 10/11 (64-bit) or macOS 12+
- **.NET:** .NET 10 Runtime (included in installer)
- **Disk Space:** ~50 MB
- **Optional:** ServUO server for auto-sync

---

## 🚀 Installation

### Windows

1. Download the latest release `.zip` file
2. Extract to any folder (e.g., `C:\UORespawn\`)
3. Run `UORespawnApp.exe`
4. (Optional) Configure your ServUO Data folder in Settings for auto-sync

### macOS

1. Download the latest release `.dmg` file
2. Mount the DMG and drag UORespawn to Applications
3. Run UORespawn from Applications
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
   - **World Spawn** - Configure tile-based automatic spawns
   - **Static Spawn** - Set spawns near world objects
3. **Add Creatures** - Select from the bestiary
4. **Configure Settings** - Adjust spawn parameters
5. **Auto-Sync** - Files automatically save to your ServUO folder

### Map Spawn Controls

- **Left-Drag:** Draw spawn box
- **Right-Drag:** Pan the map
- **Mini-Map Click:** Jump to location
- **Reset Button:** Return to origin

### Special NPCs

Add these predefined NPCs from the bestiary:
- `TownNPC`, `WorldNPC`, `AmbushNPC`
- Effect NPCs: `FireEffectNPC`, `PoisonEffectNPC`, `GlowEffectNPC`, etc.

### Files Generated

- `UOR_Spawn.csv` - Map spawn boxes
- `UOR_WorldSpawn.csv` - World tile spawns
- `UOR_StaticSpawn.csv` - Static object spawns
- `UOR_SpawnSettings.csv` - Configuration

---

## 🔨 Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2025](https://visualstudio.microsoft.com/) or [Visual Studio 2022](https://visualstudio.microsoft.com/) (v17.14 or later)
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
UORespawn/
├── UORespawnApp/              # Main MAUI application
│   ├── Components/            # Blazor components
│   │   ├── Layout/           # Navigation and layout
│   │   ├── Pages/            # Page components
│   │   ├── MapComponent.razor
│   │   ├── WorldSpawnComponent.razor
│   │   ├── StaticSpawnComponent.razor
│   │   ├── SettingsComponent.razor
│   │   └── InstructionsComponent.razor
│   ├── Scripts/              # Utility scripts
│   ├── wwwroot/              # Static web assets
│   │   ├── maps/            # Map images
│   │   ├── js/              # JavaScript interop
│   │   └── css/             # Styles
│   ├── Data/                 # Spawn data files
│   └── Resources/            # App resources
├── README.md
├── LICENSE
├── .gitignore
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
