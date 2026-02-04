using Server.Mobiles;

namespace Server.Custom.SpawnSystem.Mobiles
{
    internal class SmokeEffectNPC : Bird
    {
        [Constructable]
        public SmokeEffectNPC() : base()
        {
            Name = "Smoke Effect";

            Hidden = true;

            Hue = 0x4000;

            Blessed = true;
        }

        public override int GetIdleSound()
        {
            return -1;
        }

        public SmokeEffectNPC(Serial serial) : base(serial)
        {
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
            EffectUtility.TryRunEffect(this, UOREffects.Smoke);

            base.OnAfterMove(oldLocation);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
