using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.UORespawnServer.Mobiles;
[SerializationGenerator(0, false)]
internal partial class PlaceHolder : BaseCreature
{
    private Point3D spawnLocation;

    private Region spawnRegion;

    private string tileName;

    public string SpawnName { get; set; }

    [Constructible]
    public PlaceHolder() : base(AIType.AI_Melee, FightMode.None, 10, 1)
    {
        Name = "a placeholder wisp";

        Body = 165;

        Hue = 2498;

        BaseSoundID = 466;

        Blessed = true;

        AddItem(new LightSource());

        Karma = 20000;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (spawnRegion != null)
        {
            string parent = string.Empty;

            if (spawnRegion.Parent != null)
            {
                parent = spawnRegion.Parent.Name;
            }

            if (spawnRegion.Name != null)
            {
                if (string.IsNullOrEmpty(parent))
                {
                    Say($"{spawnRegion.Name}: {spawnLocation} [{tileName}]");
                }
                else
                {
                    Say($"{parent} > {spawnRegion.Name}: {spawnLocation} [{tileName}]");
                }
            }
            else
            {
                Say($"{spawnRegion}: {spawnLocation} [{tileName}]");
            }
        }
        else
        {
            Say($"No Region: {spawnLocation} [{tileName}]");
        }

        if (!string.IsNullOrEmpty(SpawnName)) { from.SendMessage(53, $"{SpawnName}"); }

        from.Location = spawnLocation;

        base.OnDoubleClick(from);
    }

    public override void OnAfterSpawn()
    {
        if (Region != null)
        {
            spawnRegion = Region;
        }

        spawnLocation = Location;

        tileName = new LandTarget(Location, Map).Name;

        base.OnAfterSpawn();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Delete();
    }
}
