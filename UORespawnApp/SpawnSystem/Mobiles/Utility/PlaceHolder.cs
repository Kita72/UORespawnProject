using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.SpawnSystem.Mobiles
{
    internal class PlaceHolder : BaseCreature
    {
        private Point3D spawnLocation;

        private Region spawnRegion;

        private string tileName;

        [Constructable]
        public PlaceHolder() : base(AIType.AI_Melee, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = "a placeholder wisp";

            Body = 165;

            Hue = 2498;

            BaseSoundID = 466;

            Blessed = true;

            AddItem(new LightSource());

            Karma = 20000;
        }

        public PlaceHolder(Serial serial) : base(serial)
        {
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

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version > -1)
            {
                Delete();
            }
        }
    }
}
