using UORespawnApp.Scripts.Enums;
using UORespawnApp.Scripts.Helpers;

namespace UORespawnApp.Scripts.Entities
{
    /// <summary>
    /// Represents a vendor spawn for a specific location.
    /// Each sign/hive location can have its own vendor list.
    /// </summary>
    internal class VendorEntity
    {
        public int MapId { get; set; }

        public bool IsSign { get; set; }

        public SignTypes Sign { get; set; }

        public FacingTypes Facing { get; set; }

        public Point3D Location { get; set; }

        public List<string> VendorList { get; set; }

        /// <summary>
        /// Creates a hive vendor entity (not a sign).
        /// </summary>
        public VendorEntity(Point3D location)
        {
            IsSign = false;
            Sign = SignTypes.MetalPost;
            Facing = FacingTypes.North;
            Location = location;
            VendorList = [];
        }

        /// <summary>
        /// Creates a sign vendor entity.
        /// </summary>
        public VendorEntity(SignTypes sign, FacingTypes facing, Point3D location)
        {
            IsSign = true;
            Sign = sign;
            Facing = facing;
            Location = location;
            VendorList = [];
        }

        /// <summary>
        /// Default constructor for deserialization.
        /// </summary>
        public VendorEntity()
        {
            IsSign = false;
            Sign = SignTypes.MetalPost;
            Facing = FacingTypes.North;
            Location = new Point3D();
            VendorList = [];
        }

        public void AddVendor(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                VendorList.Add(name);
            }
        }

        public void RemoveVendor(string name)
        {
            VendorList.Remove(name);
        }

        public void SetVendors(List<string> vendors)
        {
            VendorList = vendors ?? [];
        }
    }
}
