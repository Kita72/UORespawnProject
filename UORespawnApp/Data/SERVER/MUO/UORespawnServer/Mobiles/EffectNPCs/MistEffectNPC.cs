using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Mobiles;
[SerializationGenerator(0, false)]
internal partial class MistEffectNPC : Bird
{
    [Constructible]
    public MistEffectNPC()
    {
        Name = "Mist Effect";

        Hidden = true;

        Hue = 0x4000;

        Blessed = true;
    }

    public override int GetIdleSound()
    {
        return -1;
    }

    public override void OnHiddenChanged()
    {
        if (!Hidden)
        {
            Hidden = true;
        }

        base.OnHiddenChanged();
    }

    public override void OnAfterMove(Point3D oldLocation)
    {
        EffectUtility.TryRunEffect(this, UOREffects.Mist);

        base.OnAfterMove(oldLocation);
    }

}
