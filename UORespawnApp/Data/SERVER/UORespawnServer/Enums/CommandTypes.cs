namespace Server.Custom.UORespawnServer.Enums
{
    /// <summary>
    /// Action to perform on spawn data.
    /// </summary>
    internal enum CommandAction
    {
        None,
        Add,
        Remove,
        Update
    }

    /// <summary>
    /// Target data type for the command.
    /// </summary>
    internal enum CommandTarget
    {
        None,
        Settings,
        Box,
        Region,
        Tile,
        Vendor
    }

    /// <summary>
    /// Spawn section/list to modify.
    /// </summary>
    internal enum SpawnSection
    {
        None,
        Common,
        Uncommon,
        Rare,
        Water,
        Weather,
        Timed
    }

    /// <summary>
    /// Trigger type for conditional spawns.
    /// </summary>
    internal enum SpawnTrigger
    {
        None,
        Weather,
        Timed
    }
}
