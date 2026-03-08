using ModernUO.Serialization;

namespace Server.Custom.UORespawnServer.Items;
[SerializationGenerator(0, false)]
internal partial class DebugFlag : Item
{
    private string _PlayerName;
    private string _RegionName;
    private string _TileName;
    private string _Reason;

    [Constructible]
    public DebugFlag() : base(0xAA7F)
    {
        Name = "Debug Flag";
        Movable = false;
    }

    internal void SetInfo(string name, string region, string tile, string reason)
    {
        _PlayerName = name;
        _RegionName = region;
        _TileName = tile;
        _Reason = reason;
    }

    public override void OnDoubleClick(Mobile from)
    {
        from.SendMessage(53, $"{_PlayerName} | {_RegionName} | {_TileName} | {_Reason}");
        from.SendMessage(43, $"Reasons | {_Reason}");
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        UOR_Core.AddFlag();

        Delete();
    }
}
