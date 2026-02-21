using UORespawnApp.Scripts.Enums;
using UORespawnApp.Scripts.Helpers;

namespace UORespawnApp.Scripts.Entities
{
    internal class VendorEntity
    {
        public bool IsSign { get; private set; }

        public SignTypes Sign { get; private set; }

        public FacingTypes Facing { get; private set; }

        public Point3D Location { get; private set; }

        internal List<string> VendorList { get; private set; }

        // MetalPost (Misc Vendors) <BeeKeeper>
        public VendorEntity(Point3D location)
        {
            IsSign = false;

            Sign = SignTypes.MetalPost;

            Facing = FacingTypes.North;

            Location = location;

            VendorList = [];
        }

        // Signs
        public VendorEntity(SignTypes sign, FacingTypes facing, Point3D location)
        {
            IsSign = true;

            Sign = sign;

            Facing = facing;

            Location = location;

            VendorList = [];
        }

        public void AddVendor(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                VendorList.Add(name);
            }
        }
    }
}
