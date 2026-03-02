namespace UORespawnApp.Scripts.Enums;

/// <summary>
/// Defines the action to perform for an edit command
/// Matches server-side CommandAction enum
/// </summary>
public enum CommandAction
{
    /// <summary>Add a creature/vendor to a spawn list</summary>
    Add,

    /// <summary>Remove a creature/vendor from a spawn list</summary>
    Remove,

    /// <summary>Update a setting value</summary>
    Update
}

/// <summary>
/// Defines the target type for an edit command
/// Matches server-side CommandTarget enum
/// </summary>
public enum CommandTarget
{
    /// <summary>Settings configuration (UOR_SpawnSettings.csv)</summary>
    Settings,

    /// <summary>Box spawn definition</summary>
    Box,

    /// <summary>Region spawn definition</summary>
    Region,

    /// <summary>Tile spawn definition</summary>
    Tile,

    /// <summary>Vendor spawn definition</summary>
    Vendor
}

/// <summary>
/// Defines which spawn list section to modify
/// Matches server-side SpawnSection enum
/// </summary>
public enum SpawnSection
{
    /// <summary>Not applicable (used for Settings/Vendor)</summary>
    None,

    /// <summary>Common spawn list (always spawns)</summary>
    Common,

    /// <summary>Uncommon spawn list (~10% chance)</summary>
    Uncommon,

    /// <summary>Rare spawn list (~1% chance)</summary>
    Rare,

    /// <summary>Water spawn list (when on water)</summary>
    Water,

    /// <summary>Weather spawn list (during weather events)</summary>
    Weather,

    /// <summary>Timed spawn list (night time)</summary>
    Timed
}

/// <summary>
/// Defines trigger conditions for spawns
/// Matches server-side SpawnTrigger enum
/// </summary>
public enum SpawnTrigger
{
    /// <summary>No trigger (normal spawn)</summary>
    None,

    /// <summary>Weather-triggered spawn</summary>
    Weather,

    /// <summary>Time-triggered spawn (day/night)</summary>
    Timed
}
