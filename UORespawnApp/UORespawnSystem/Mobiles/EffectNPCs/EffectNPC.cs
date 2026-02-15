using Server.Mobiles;

namespace Server.Custom.UORespawnSystem.Mobiles
{
    internal class EffectNPC : Bird
    {
        private UOREffects spawnEffect = UOREffects.None;

        [Constructable]
        public EffectNPC() : base()
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

        public EffectNPC(Serial serial) : base(serial)
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

        public override bool CheckMovement(Direction d, out int newZ)
        {
            spawnEffect = EffectUtility.SetSpawnEffect(this);

            return base.CheckMovement(d, out newZ);
        }

        public override void OnAfterMove(Point3D oldLocation)
        {
            EffectUtility.TryRunEffect(this, spawnEffect);

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

            _= reader.ReadInt();
        }
    }
}
