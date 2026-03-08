using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Mobiles;
[SerializationGenerator(0, false)]
internal partial class WaveEffectNPC : Bird
{
    [Constructible]
    public WaveEffectNPC()
    {
        Name = "Wave Effect";

        Hidden = true;

        Hue = 0x4000;

        Blessed = true;

        CanSwim = true;
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
        EffectUtility.TryRunEffect(this, UOREffects.Wave);

        base.OnAfterMove(oldLocation);
    }

}
