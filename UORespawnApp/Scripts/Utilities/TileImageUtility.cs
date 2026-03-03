using UORespawnApp.Scripts.Constants;

namespace UORespawnApp.Scripts.Utilities;

/// <summary>
/// Utility class for tile image operations
/// 
/// TILE IMAGE STORAGE:
/// - Location: Data/TILES/ folder (via PathConstants.TilesPath)
/// - Format: PNG images named as lowercase tile name (e.g., grass.png, water.png)
/// - Fallback: NO_DRAW.png used when tile image not found
/// 
/// TILE IMAGE USAGE:
/// 1. TileSpawnComponent displays selected tile's image
/// 2. Images loaded from Data/TILES/ and converted to base64 for Blazor display
/// 3. Custom tile names from server use NO_DRAW.png fallback if no image exists
/// </summary>
public static class TileImageUtility
{
    private const string FALLBACK_IMAGE = "NO_DRAW.png";
    private static readonly Dictionary<string, string> _imageCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tile descriptions for the 32 standard tile types.
    /// Each tile represents a terrain group - multiple visual variants may exist with the same name.
    /// Descriptions are based on Ultima Online ecology and lore.
    /// </summary>
    private static readonly Dictionary<string, TileDescription> _tileDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Natural Terrains
        ["Grass"] = new("The Verdant Plains",
            "The most common terrain in Britannia. Open grasslands support grazing livestock near cities and wild game in the frontier. Rain draws amphibians from hiding, while predators stalk at dusk.",
            "Natural"),
        ["Forest"] = new("The Deep Woods",
            "Dense woodland found in Yew and the Spiritwood. The canopy creates shade where skittish prey hides from predators. The Witching Hour here is dangerous—Reapers and Wisps emerge from the ancient trees.",
            "Natural"),
        ["Jungle"] = new("The Tropical Belts",
            "Humid tropical terrain found in Eodon and the Valorian Isles. High moisture supports reptilian life and poisonous flora. Giant serpents become active at noon when temperatures peak.",
            "Natural"),
        ["Sand"] = new("The Arid Wastes",
            "Desert terrain of the Lost Lands and Desert of Compassion. Only hardy life survives here—scorpions and snakes dominate by day. At night, the cooling sands attract undead Mummies preserved by the dry air.",
            "Natural"),
        ["Snow"] = new("The Frozen Tundra",
            "Arctic terrain of Dagger Isle and Tokuno's Winter Spur. The ecosystem relies on blubber-heavy mammals like Walrus and Polar Bear, alongside magical ice constructs that thrive in the endless cold.",
            "Natural"),
        ["Swamp"] = new("The Festering Bogs",
            "Murky wetlands near Trinsic and the Lost Lands. Disease vectors are high—Plague Beasts and Bog Things call this home. Rain events trigger massive slime spawns in the constant humidity.",
            "Natural"),
        ["Cave"] = new("Subterranean Natural",
            "Natural caverns distinct from constructed dungeons. Home to troglodytic life: bats, rats, and earth elementals. Without sunlight, day/night matters little—but bats still emerge at dusk by instinct.",
            "Natural"),
        ["Dirt"] = new("Roads and Farmland",
            "Well-traveled paths and tilled earth. Generally safe terrain populated by travel mounts and the occasional bandit looking to waylay merchants along the trade routes.",
            "Natural"),
        ["Furrows"] = new("Plowed Agriculture",
            "Cultivated farmland tiles of Skara Brae and Britain's farms. These food sources attract vermin like rabbits and crows, which in turn draw low-level predators hunting easy meals.",
            "Natural"),
        ["Mountain"] = new("High Altitude",
            "Rocky crags and peaks throughout Britannia. Home to Goats, Eagles, and territorial Giants who use the height advantage to scan for intruders in their domain.",
            "Natural"),
        ["Water_Shallow"] = new("Coastlines",
            "The littoral zone where land meets sea. Crabs scuttle along shores, Walruses bask in the north, and Frogs hunt insects. Sea Serpents use these shallows to strike at land-dwellers.",
            "Natural"),
        ["Water_Deep"] = new("The High Seas",
            "The domain of the Kraken and Sea Serpents. These tiles are only accessible by boat—expect high-threat spawns worthy of brave sailors who venture into deep waters.",
            "Natural"),
        ["Leaves"] = new("Forest Underbrush",
            "The forest floor covered in deep leaf litter, distinct from grass. Small predators like snakes hide here, while Pixies dance in the Weald's ancient 'Old Forest' biome.",
            "Natural"),

        // Constructed Terrains
        ["Flagstone"] = new("City Streets & Plazas",
            "Urban paving of Britannia's cities. Within guard zones, threats are suppressed—but in Felucca or ruined towns, these stones host urban vermin and criminals who prey on the unwary.",
            "Constructed"),
        ["Brick"] = new("Dungeon Interiors",
            "The quintessential dungeon architecture of Deceit and Despise. These 'Old World' walls house Skeletons, Zombies, and the ever-present Rats that feed on the fallen.",
            "Constructed"),
        ["Wood_Plank"] = new("Docks, Bridges, Floors",
            "Wooden platforms at docks like Vesper and Buccaneer's Den. Rats gnaw the planks while Seagulls cry overhead. In lawless areas, Pirates and Brigands lurk for victims.",
            "Constructed"),
        ["Marble"] = new("Palaces and High Shrines",
            "Prestigious architecture of Montor and Nujel'm. In ruins, Terathans claim these halls; in active shrines, Nobles conduct business guarded by Gargoyle sentinels.",
            "Constructed"),
        ["Stone_Moss"] = new("Overgrown Ruins",
            "Ancient ruins being reclaimed by nature in the Lost Lands. Where moss creeps over stone, Slimes ooze, Snakes coil, and Rotting Corpses shamble through the decay.",
            "Constructed"),
        ["Sandstone"] = new("Canyons and Desert Cities",
            "Sun-baked stone of the Desert of Compassion and Orc Forts. Scorpions and Earth Elementals blend perfectly with this terrain that Orcs favor for their strongholds.",
            "Constructed"),
        ["Gravel"] = new("Roadways & Mines",
            "Loose stone of lesser roads and mine floors. Wolves and bandits ambush travelers here. In Minoc's mines, Ore Elementals form from the disturbed earth.",
            "Constructed"),
        ["Embank"] = new("Cliff Edges & Ridges",
            "Sharp mountain edges and steep ridges. Eagles nest on these heights while Harpies roost on sheer faces, using gravity to dive-bomb prey far below.",
            "Constructed"),

        // Exotic Terrains
        ["Obsidian"] = new("Volcanic/Evil",
            "Black volcanic glass of Hythloth and the Fire Dungeon. It radiates heat and malice—home to Fire Elementals, Daemons, and Lava Lizards whose biology is silicon or plasma-based.",
            "Exotic"),
        ["Void"] = new("Ethereal Plane / Ter Mur",
            "The space between worlds—the abyss of Ter Mur. Korpre, Void Wanderers, and Ortanords drift here. This terrain bridges dimensions, allowing 'alien' entities to manifest.",
            "Exotic"),
        ["Acid"] = new("Corrosive Environments",
            "Sickly green terrain of the Blighted Grove and Palace of Paroxysmus. Contact causes damage. Acid Elementals, Plague Beasts, and Slimes thrive in the caustic pools.",
            "Exotic"),
        ["Blood"] = new("Necromantic Grounds",
            "Crimson-stained terrain of Covetous and champion altars. Blood Elementals, Rotting Corpses, and Flesh Golems are drawn here—the biological imperative is hematophagy.",
            "Exotic"),
        ["Cloud"] = new("The Sky Realms",
            "Magical terrain of Wind and high-altitude passages. Air Elementals ride the currents, Harpies soar on thermals, and Wisps drift through the upper atmosphere.",
            "Exotic"),
        ["Crystal"] = new("The Prism of Light",
            "Shimmering, refractive terrain of the Prism of Light. Crystal Elementals and energy beings use the refraction for camouflage in this silicon-based ecosystem.",
            "Exotic"),
        ["Mycelium"] = new("Fungal Floors",
            "Spongy fungal terrain of Solen Hives and the Twisted Weald. Ecology is dominated by insectoids—Solens and Myrmidex—alongside fungal symbionts that feed on decay.",
            "Exotic"),
        ["Shadow"] = new("Dark Tiles of Umbra",
            "Perpetually dark terrain of Malas (Umbra) and Shadowguard. Shadow Wisps, Necromancers, and light-fearing undead thrive in this endless darkness.",
            "Exotic"),
        ["Lava"] = new("Molten Rock",
            "Liquid fire. Only fire-immune creatures survive—Lava Lizards, Fire Elementals, and Hell Hounds. Non-immune beings would perish instantly on contact.",
            "Exotic"),
        ["Ice_Slippery"] = new("Frozen Lakes",
            "The treacherous surface of the Ice Dungeon. Low friction favors Ice Snakes and Frost Oozes perfectly adapted to gliding across the frozen expanse.",
            "Exotic"),
        ["Ash"] = new("Desolation & Volcanic Fallout",
            "Barren aftermath of volcanic activity in the Stygian Abyss. Hell Hounds and undead roam these dead lands where vegetation cannot grow and life struggles to persist.",
            "Exotic"),
    };

    /// <summary>
    /// Gets the description for a tile type
    /// </summary>
    /// <param name="tileName">The tile name</param>
    /// <returns>TileDescription with title, description, and category; or default for unknown tiles</returns>
    public static TileDescription GetTileDescription(string tileName)
    {
        if (string.IsNullOrWhiteSpace(tileName))
            return GetCustomTileDescription();

        if (_tileDescriptions.TryGetValue(tileName, out var description))
            return description;

        // Return custom/unknown tile description
        return GetCustomTileDescription();
    }

    /// <summary>
    /// Gets the description for custom/unknown tiles
    /// </summary>
    private static TileDescription GetCustomTileDescription()
    {
        return new TileDescription(
            "Custom Tile Type",
            "This terrain type was defined by your server and doesn't have a standard description. Custom tiles allow server administrators to create unique biomes with their own ecology. Configure spawns based on your server's specific design.",
            "Custom"
        );
    }

    /// <summary>
    /// Gets the tile image as a base64 data URL for display in Blazor
    /// </summary>
    /// <param name="tileName">The tile name (e.g., "Grass", "Water")</param>
    /// <returns>Base64 data URL string, or fallback image if not found</returns>
    public static string GetTileImageDataUrl(string tileName)
    {
        if (string.IsNullOrWhiteSpace(tileName))
            return GetFallbackImageDataUrl();

        // Check cache first
        if (_imageCache.TryGetValue(tileName, out var cachedUrl))
            return cachedUrl;

        try
        {
            var imagePath = GetTileImagePath(tileName);
            
            if (File.Exists(imagePath))
            {
                var imageBytes = File.ReadAllBytes(imagePath);
                var base64 = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:image/png;base64,{base64}";
                
                _imageCache[tileName] = dataUrl;
                return dataUrl;
            }
            
            // Try fallback image
            return GetFallbackImageDataUrl();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading tile image for '{tileName}'", ex);
            return GetFallbackImageDataUrl();
        }
    }

    /// <summary>
    /// Gets the fallback (NO_DRAW) image as base64 data URL
    /// </summary>
    private static string GetFallbackImageDataUrl()
    {
        const string fallbackKey = "__FALLBACK__";
        
        if (_imageCache.TryGetValue(fallbackKey, out var cachedUrl))
            return cachedUrl;

        try
        {
            var fallbackPath = Path.Combine(PathConstants.TilesPath, FALLBACK_IMAGE);
            
            if (File.Exists(fallbackPath))
            {
                var imageBytes = File.ReadAllBytes(fallbackPath);
                var base64 = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:image/png;base64,{base64}";
                
                _imageCache[fallbackKey] = dataUrl;
                return dataUrl;
            }
            
            // Return empty string if no fallback exists
            return string.Empty;
        }
        catch (Exception ex)
        {
            Logger.Error("Error loading fallback tile image", ex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the full path to a tile image
    /// Images are stored as lowercase tile name with .png extension
    /// </summary>
    /// <param name="tileName">The tile name</param>
    /// <returns>Full path to the tile image</returns>
    public static string GetTileImagePath(string tileName)
    {
        var fileName = $"{tileName.ToLowerInvariant()}.png";
        return Path.Combine(PathConstants.TilesPath, fileName);
    }

    /// <summary>
    /// Checks if a tile has a dedicated image (not using fallback)
    /// </summary>
    /// <param name="tileName">The tile name to check</param>
    /// <returns>True if a dedicated image exists for this tile</returns>
    public static bool HasTileImage(string tileName)
    {
        if (string.IsNullOrWhiteSpace(tileName))
            return false;

        return File.Exists(GetTileImagePath(tileName));
    }

    /// <summary>
    /// Clears the image cache (useful when images are updated)
    /// </summary>
    public static void ClearCache()
    {
        _imageCache.Clear();
    }

    /// <summary>
    /// Gets a list of tile names that have images available
    /// </summary>
    /// <returns>List of tile names with images</returns>
    public static List<string> GetAvailableTileImages()
    {
        List<string> tiles = [];

        try
        {
            var tilesFolder = PathConstants.TilesPath;
            
            if (Directory.Exists(tilesFolder))
            {
                var imageFiles = Directory.GetFiles(tilesFolder, "*.png");
                tiles = [.. imageFiles
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Where(n => !n.Equals("NO_DRAW", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)];
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Error getting available tile images", ex);
        }

        return tiles;
    }
}

/// <summary>
/// Represents a tile type description with ecological information
/// </summary>
/// <param name="Title">Short descriptive title (e.g., "The Verdant Plains")</param>
/// <param name="Description">Full ecological description of the terrain</param>
/// <param name="Category">Terrain category: Natural, Constructed, Exotic, or Custom</param>
public record TileDescription(string Title, string Description, string Category);
