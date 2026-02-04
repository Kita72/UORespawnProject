using Server.Items;
using System;

namespace Server.Custom.SpawnSystem.Items
{
    internal class RiftGate : Moongate
    {
        public override bool ShowFeluccaWarning { get { return false; } }

        public override bool TeleportPets { get { return true; } }

        [Constructable]
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

        public RiftGate(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            Delete();
        }
    }
}
