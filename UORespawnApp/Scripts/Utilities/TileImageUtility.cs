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
    private static readonly Lock _cacheLock = new();

    /// <summary>
    /// Tile descriptions for terrain types in Ultima Online.
    /// Each tile represents a terrain group - multiple visual variants may exist with the same name.
    /// Descriptions are based on Ultima Online ecology, lore, and gameplay mechanics.
    /// </summary>
    private static readonly Dictionary<string, TileDescription> _tileDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        // ==================== NATURAL TERRAINS ====================

        ["Grass"] = new("Open Meadows",
            "The verdant fields and rolling meadows that define much of Sosaria's landscape. These open grasslands support diverse wildlife—deer graze peacefully while rabbits dart between patches of wildflowers. However, danger lurks even in these pastoral settings, as bandits frequently ambush unwary travelers crossing the frontier, and predators stalk prey during dusk and dawn hours.",
            "Natural"),

        ["Forest"] = new("Dense Woodlands",
            "Thick canopies and ancient groves like those found in Yew Forest create an ecosystem where light filters dimly through towering oaks. Wolves prowl between the trunks hunting deer, bears forage for berries and honey, while shadowy druids conduct their mysterious rituals in hidden clearings. The Witching Hour brings particular danger—Reapers and Wisps emerge from the oldest trees.",
            "Natural"),

        ["Jungle"] = new("Tropical Wilds",
            "The vine-choked, humid lands surrounding Terathan Keep and the Lost Lands pulse with dangerous life. Tigers stalk silently through the undergrowth, venomous snakes coil on sun-warmed rocks, and cougars wait in ambush on low branches. Giant serpents become most active at noon when temperatures peak, making midday travel particularly treacherous.",
            "Natural"),

        ["Sand"] = new("Desert Dunes",
            "The shifting sands of the Paroxysmus wastes and Desert of Compassion create an unforgiving environment where only the hardiest creatures survive. Scorpions burrow beneath the surface waiting to strike, and giant beetles tunnel through the dunes searching for prey. By day, the heat is oppressive; by night, the cooling sands attract undead Mummies preserved by the arid conditions.",
            "Natural"),

        ["Snow"] = new("Frozen Tundra",
            "The icy expanses of Ice Islands and Dagger Isle support a unique ecosystem adapted to endless cold. Polar bears hunt seals along frozen coastlines, frost trolls ambush travelers in blinding blizzards, and ice elementals drift through the eternal white. The ecosystem relies on blubber-heavy mammals like Walrus, alongside magical constructs that thrive in sub-zero temperatures.",
            "Natural"),

        ["Swamp"] = new("Murky Bogs",
            "The fetid mires of the Bog of Desolation and Fens of the Dead breed disease and danger in equal measure. Lizardmen lurk in the murky waters, mongbats screech from dead trees, and tentacled horrors thrash beneath the surface. Plague Beasts and Bog Things call these festering wetlands home, while rain events trigger massive slime spawns in the constant humidity.",
            "Natural"),

        ["Cave"] = new("Subterranean Depths",
            "The dark rocky floors of mountain caves and early dungeon levels create a world without sunlight where unique creatures have adapted to eternal darkness. Bats roost in their thousands on cavern ceilings, giant spiders spin webs across passages, and troglodytes guard their underground territories. Earth elementals stir from the living rock itself, forming from the mineral-rich stone.",
            "Natural"),

        ["Dirt"] = new("Barren Fields",
            "The packed earth of untamed plains and well-traveled roads. While generally safer than wilderness terrain, these barren stretches still harbor threats—earth elementals rise from disturbed soil, and bandits set ambushes along trade routes to waylay merchants. Travel mounts and pack animals are common sights on the more civilized paths near cities.",
            "Natural"),

        ["Furrows"] = new("Plowed Farmlands",
            "The tilled soil of Skara Brae and Britain's agricultural heartland. These cultivated fields attract vermin seeking easy meals—mongbats raid ripening crops, rabbits devastate gardens, and crows feast on newly planted seeds. The disturbance of farming also awakens earth elementals from their slumber, rising to defend the land from those who plow it.",
            "Natural"),

        ["Rock"] = new("Mountain Trails",
            "The jagged paths winding through the Ice Mountains and other highland regions. These rocky trails offer little cover from the giants and ettins who claim the peaks as their domain. Harpies nest on sheer cliff faces, using the height advantage to spot and dive-bomb travelers far below. Every step on the loose scree risks attracting unwanted attention.",
            "Natural"),

        ["Leaves"] = new("Forest Litter",
            "The forest floor carpeted in deep leaf litter, distinct from grassy clearings. Decades of decay create a soft layer where small predators like snakes hide perfectly camouflaged. Boars root through the decomposing matter searching for tubers, while treants guard the most ancient groves where Pixies dance among the oldest trees of the 'Old Forest' biome.",
            "Natural"),

        ["Tree"] = new("Woodland Bases",
            "The gnarled trunks and root systems of deep forest trees. Elves make their homes among these ancient giants, living in harmony with the woodland creatures. Dire wolves den in hollow trunks, and forest spirits are said to dwell within the oldest oaks. These areas represent the heart of Sosaria's wilderness, where civilization has never taken hold.",
            "Natural"),

        ["Embankment"] = new("River Slopes",
            "The grassy banks along Felucca's rivers and streams where the land meets water. Beavers industriously dam the waterways, creating pools that attract diverse wildlife. Trolls occasionally roam these slopes seeking easy prey, and the transition zone between land and water supports unique creatures adapted to both environments.",
            "Natural"),

        ["Embank"] = new("River Slopes",
            "The grassy banks along Felucca's rivers and streams where the land meets water. Beavers industriously dam the waterways, creating pools that attract diverse wildlife. Trolls occasionally roam these slopes seeking easy prey, and the transition zone between land and water supports unique creatures adapted to both environments.",
            "Natural"),

        ["Water"] = new("Lakes and Seas",
            "The deep oceans and inland waters that cover much of Sosaria's surface. Sea serpents patrol coastal waters while krakens lurk in the abyssal depths, emerging to drag ships to watery graves. These tiles are primarily accessible by boat, and sailors know to expect high-threat spawns worthy of those brave enough to venture into deep waters.",
            "Natural"),

        // ==================== CONSTRUCTED TERRAINS ====================

        ["Flagstone"] = new("Stone Paths",
            "Chiseled stone slabs forming roads that wind to remote shrines and connect distant settlements. Orc scouts patrol these routes seeking targets, while travelers and merchants pass in relative safety during daylight. Within guard zones the threats are suppressed, but in Felucca or ruined towns, these ancient stones host urban vermin and criminals.",
            "Constructed"),

        ["Cobblestones"] = new("City Streets",
            "The weathered stones that line the roads in cities like Moonglow and Trinsic. Guards patrol these urban thoroughfares, maintaining order in the bustling marketplaces. Yet even under watchful eyes, pickpockets prowl for distracted marks, and in the darker hours, more dangerous elements emerge from the shadows between buildings.",
            "Constructed"),

        ["Brick"] = new("Urban Construction",
            "Sturdy red bricks pave the floors in Britain's taverns and Vesper's warehouses. These safe indoor environments provide refuge from the dangers outside, where guards maintain order and merchants conduct business. The warmth of a brick-floored inn offers comfort to weary travelers after long journeys through dangerous wilderness.",
            "Constructed"),

        ["Planks"] = new("Wooden Interiors",
            "Rough-hewn boards forming the floors of humble homes and rustic establishments throughout Sosaria. These wooden surfaces provide safe domestic environments, sheltered from the wilderness threats that prowl beyond the walls. Craftsmen and commoners alike find security within plank-floored structures.",
            "Constructed"),

        ["Marble"] = new("Noble Halls",
            "The polished floors of Lord British's Castle and the grand estates of nobility. These prestigious spaces represent safety and civilization at its finest—nobles and knights reside within marble halls, conducting the business of the realm. In abandoned ruins, however, Terathans may claim these once-grand chambers.",
            "Constructed"),

        ["Stone_Moss"] = new("Ancient Ruins",
            "Moss-covered slabs in forgotten crypts and ancient ruins being slowly reclaimed by nature. Where moss creeps over crumbling stone, Slimes ooze through cracks, Snakes coil in shadowed corners, and Rotting Corpses shamble through the decay. The undead and liches awaken in these places where time has worn away the boundaries between worlds.",
            "Constructed"),

        ["Stone Moss"] = new("Ancient Ruins",
            "Moss-covered slabs in forgotten crypts and ancient ruins being slowly reclaimed by nature. Where moss creeps over crumbling stone, Slimes ooze through cracks, Snakes coil in shadowed corners, and Rotting Corpses shamble through the decay. The undead and liches awaken in these places where time has worn away the boundaries between worlds.",
            "Constructed"),

        ["Sandstone"] = new("Desert Ruins",
            "Crumbling blocks baking under the relentless sun in the Desert of Desolation and ancient Orc strongholds. Mummies wrapped in ancient bandages haunt these sun-scorched ruins, while lamias slither through sand-filled chambers. Scorpions and Earth Elementals blend perfectly with this terrain that Orcs favor for their fortifications.",
            "Constructed"),

        ["Sand Stone"] = new("Desert Ruins",
            "Crumbling blocks baking under the relentless sun in the Desert of Desolation and ancient Orc strongholds. Mummies wrapped in ancient bandages haunt these sun-scorched ruins, while lamias slither through sand-filled chambers. Scorpions and Earth Elementals blend perfectly with this terrain that Orcs favor for their fortifications.",
            "Constructed"),

        ["Stone"] = new("Dungeon Floors",
            "Cold flagstone forming the floors of Wrong, Despise, and countless other dungeons throughout Sosaria. Skeletons patrol endlessly through these grim corridors, wraiths drift through solid walls, and the echoes of ancient suffering permeate every stone. These depths have witnessed countless battles between adventurers and the creatures of darkness.",
            "Constructed"),

        ["Tile"] = new("Ceramic Surfaces",
            "Glazed ceramic tiles adorning the floors of baths, kitchens, and refined establishments. These decorative surfaces indicate safe domestic environments where daily life continues undisturbed by the dangers plaguing the wilderness. The craftsmanship speaks to civilized spaces maintained by careful hands.",
            "Constructed"),

        ["Wooden_Floor"] = new("Rustic Homes",
            "Polished wooden boards in common cottages and rural homesteads throughout Britannia. These secure domestic spaces offer protection from the beasts that roam outside, providing warm hearths and safe rest for farmers, craftsmen, and travelers seeking shelter from the dangers of the wild.",
            "Constructed"),

        ["Wooden Floor"] = new("Rustic Homes",
            "Polished wooden boards in common cottages and rural homesteads throughout Britannia. These secure domestic spaces offer protection from the beasts that roam outside, providing warm hearths and safe rest for farmers, craftsmen, and travelers seeking shelter from the dangers of the wild.",
            "Constructed"),

        // ==================== EXOTIC TERRAINS ====================

        ["Obsidian"] = new("Infernal Depths",
            "Black volcanic stone radiating heat and malice in Destard and the Fire Dungeon. This terrain of Hythloth's deepest levels hosts creatures whose biology is silicon or plasma-based—Fire Elementals dance on the superheated surface, Daemons plot eternal schemes, and Balrons command legions of lesser fiends. Only the foolhardy venture here unprepared.",
            "Exotic"),

        ["Void"] = new("Impassable Abyss",
            "The space between worlds—bottomless chasms where reality itself fails. Instant death awaits any who fall into these gaps in existence. In Ter Mur, Korpre and Void Wanderers drift through this dimension-bridging terrain, while Ortanords manifest from the cosmic darkness. This abyss connects—and separates—all worlds.",
            "Exotic"),

        ["Acid"] = new("Corrosive Environments",
            "Sickly green terrain characterizing the Blighted Grove and Palace of Paroxysmus. Contact with these caustic surfaces inflicts continuous damage to unprotected flesh. Acid Elementals rise from the bubbling pools, Plague Beasts spread corruption, and various Slimes thrive in conditions that would dissolve ordinary creatures.",
            "Exotic"),

        ["Blood"] = new("Necromantic Grounds",
            "Crimson-stained terrain of Covetous and champion altar sites where dark rituals have saturated the earth. Blood Elementals form from accumulated essence, Rotting Corpses drag themselves from shallow graves, and Flesh Golems lumber through these grounds. The biological imperative here is hematophagy—feeding on life force itself.",
            "Exotic"),

        ["Cloud"] = new("Ethereal Heights",
            "Floating misty platforms in airy realms high above the material world. Air Elementals ride the eternal currents, Harpies soar on powerful thermals, and Wisps drift through the upper atmosphere. These magical terrains of Wind and high-altitude passages connect the mortal realm to something beyond ordinary understanding.",
            "Exotic"),

        ["Crystal"] = new("The Prism of Light",
            "Shimmering, refractive terrain of the Prism of Light dungeon where reality bends with the light. Crystal Elementals and energy beings use the constant refraction for perfect camouflage in this silicon-based ecosystem. The beauty is deadly—creatures here have evolved to exploit the disorienting visual effects.",
            "Exotic"),

        ["Mycelium"] = new("Fungal Floors",
            "Spongy fungal terrain of Solen Hives and the Twisted Weald where mushrooms tower like trees. The ecology is dominated by insectoids—Solens march in endless columns, Myrmidex defend their colonies, and fungal symbionts feed on decay. The air is thick with spores that can befuddle the unprepared.",
            "Exotic"),

        ["Shadow"] = new("Dark Tiles of Umbra",
            "Perpetually dark terrain of Malas's Umbra district and the forbidding Shadowguard. Shadow Wisps flicker at the edge of perception, Necromancers conduct their dark research, and light-fearing undead flourish in this endless darkness. Even magical illumination struggles against the oppressive shadows here.",
            "Exotic"),

        ["Lava"] = new("Volcanic Flows",
            "Molten rivers of liquid fire in the Fire Dungeon depths and volcanic regions. Only fire-immune creatures survive contact with this terrain—Lava Lizards swim through the magma, Fire Elementals dance on the surface, and Hell Hounds erupt from cooling vents. Non-immune beings perish instantly on contact.",
            "Exotic"),

        ["Ice_Slippery"] = new("Frozen Lakes",
            "The treacherous frozen surface of the Ice Dungeon where footing is uncertain. Low friction favors creatures adapted to gliding across the ice—Ice Snakes slither effortlessly, Frost Oozes slide with deadly purpose, and ice elementals form from the frozen surface. Adventurers struggle to maintain balance while these creatures attack.",
            "Exotic"),

        ["Ash"] = new("Desolation & Volcanic Fallout",
            "Barren aftermath of volcanic devastation in the Stygian Abyss and fire-ravaged lands. Hell Hounds prowl these dead expanses, and undead roam where vegetation cannot grow and ordinary life struggles to persist. The ash-choked air and lifeless terrain create an apocalyptic landscape of perpetual twilight.",
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
        lock (_cacheLock)
        {
            if (_imageCache.TryGetValue(tileName, out var cachedUrl))
                return cachedUrl;
        }

        try
        {
            var imagePath = GetTileImagePath(tileName);
            
            if (File.Exists(imagePath))
            {
                var imageBytes = File.ReadAllBytes(imagePath);
                var base64 = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:image/png;base64,{base64}";

                lock (_cacheLock)
                {
                    _imageCache[tileName] = dataUrl;
                }
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
        
        lock (_cacheLock)
        {
            if (_imageCache.TryGetValue(fallbackKey, out var cachedUrl))
                return cachedUrl;
        }

        try
        {
            var fallbackPath = Path.Combine(PathConstants.TilesPath, FALLBACK_IMAGE);
            
            if (File.Exists(fallbackPath))
            {
                var imageBytes = File.ReadAllBytes(fallbackPath);
                var base64 = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:image/png;base64,{base64}";

                lock (_cacheLock)
                {
                    _imageCache[fallbackKey] = dataUrl;
                }
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
