using Server.Items;
using System.Collections.Generic;
using Server.Custom.UORespawnSystem.SpawnWorkers;

namespace Server.Custom.UORespawnSystem.Entities
{
    internal class VendorEntity
    {
        public bool IsSign { get; private set; }

        public SignType Sign { get; private set; }

        public SignFacing Facing { get; private set; }

        public Point3D Location { get; private set; }

        internal List<string> VendorList { get; private set; }

        public VendorEntity(Point3D location)
        {
            IsSign = false;

            Sign = SignType.MetalPost;

            Facing = SignFacing.North;

            Location = GetInsideLocation(Facing, location);

            VendorList = new List<string>();
        }

        public VendorEntity(SignType sign, SignFacing facing, Point3D location)
        {
            IsSign = true;

            Sign = sign;

            Facing = facing;

            Location = GetInsideLocation(facing, location);

            VendorList = new List<string>();
        }

        public void Spawn(Map map)
        {
            VendorSpawner.TryToSpawn(map, this);
        }

        public void AddVendor(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                VendorList.Add(name);
            }
        }

        private Point3D GetInsideLocation(SignFacing facing, Point3D location)
        {
            if (IsSign)
            {
                switch (facing)
                {
                    case SignFacing.West:
                        location = new Point3D(location.X - 2, location.Y, location.Z);
                        break;
                    case SignFacing.North:
                        location = new Point3D(location.X, location.Y - 2, location.Z);
                        break;
                }
            }
            else
            {
                location = new Point3D(location.X + 1, location.Y + 1, location.Z);
            }

            return location;
        }
    }
}
