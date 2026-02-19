using Server.Items;
using Server.Mobiles;
using System.Collections.Generic;
using Server.Custom.UORespawnSystem.SpawnHelpers;
using Server.Custom.UORespawnSystem.SpawnUtility;

namespace Server.Custom.UORespawnSystem.Entities
{
    internal class VendorEntity
    {
        public SignType Sign { get; set; }

        public SignFacing Facing { get; set; }

        public Point3D Location { get; set; }

        private List<BaseCreature> Vendors { get; set; }

        public VendorEntity(SignType sign, SignFacing facing, Point3D location)
        {
            Sign = sign;

            Facing = facing;

            Location = location;
        }

        public void Spawn(Map map)
        {
            if (Vendors == null)
            {
                Vendors = new List<BaseCreature>();

                Vendors = SpawnVendors.GetVendors(Sign);
            }

            foreach (BaseCreature m in Vendors)
            {
                if (m != null)
                {
                    m.Home = Location;
                    m.RangeHome = 15;

                    Location = GetInsideLocation(Facing, Location);

                    m.OnBeforeSpawn(Location, map);
                    m.MoveToWorld(Location, map);
                    m.OnAfterSpawn();

                    SpawnVendors.VendorSpawnList.Add(m.Serial.Value);
                }
            }
        }

        private Point3D GetInsideLocation(SignFacing facing, Point3D location)
        {
            switch (facing)
            {
                case SignFacing.West: location = new Point3D(location.X - 2, location.Y, location.Z);
                    break;
                case SignFacing.North: location = new Point3D(location.X, location.Y - 2, location.Z);
                    break;
            }

            return location;
        }
    }
}
