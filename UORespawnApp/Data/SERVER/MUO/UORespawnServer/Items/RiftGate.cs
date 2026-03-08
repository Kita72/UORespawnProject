using System;
using Server.Items;
using ModernUO.Serialization;

namespace Server.Custom.UORespawnServer.Items;
[SerializationGenerator(0, false)]
internal partial class RiftGate : Moongate
{
    public override bool ShowFeluccaWarning => false;

    [Constructible]
    public RiftGate(Point3D location, Map map)
    {
        Dispellable = false;

        Target = location;

        if (map == Map.Trammel)
        {
            TargetMap = Map.Felucca;

            Hue = 2750;
        }

        if (map == Map.Felucca)
        {
            TargetMap = Map.Trammel;

            Hue = 2728;
        }

        Movable = false;

        Light = LightType.Circle300;

        Timer.DelayCall(TimeSpan.FromSeconds(30), () =>
        {
            if (!Deleted)
            {
                Delete();
            }
        });
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        UOR_Core.AddGate();

        Delete();
    }
}
