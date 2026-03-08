using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Mobiles;
[SerializationGenerator(0, false)]
internal partial class EffectNPC : Bird
{
    private UOREffects _spawnEffect = UOREffects.None;

    [Constructible]
    public EffectNPC()
    {
        Name = "Effect";

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

    public override bool CheckMovement(Direction d, out int newZ)
    {
        _spawnEffect = EffectUtility.SetSpawnEffect(this);

        return base.CheckMovement(d, out newZ);
    }

    public override void OnAfterMove(Point3D oldLocation)
    {
        EffectUtility.TryRunEffect(this, _spawnEffect);

        base.OnAfterMove(oldLocation);
    }
}
