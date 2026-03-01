using UORespawnApp.Scripts.Enums;
using UORespawnApp.Scripts.Helpers;

namespace UORespawnApp.Scripts.Entities
{
    /// <summary>
    /// Represents a vendor spawn for a specific location.
    /// Each sign/hive location can have its own vendor list.
    /// Unlike ISpawnEntity types, vendors use a single list (not 6 categories).
    /// </summary>
    internal class VendorEntity
    {
        /// <summary>
        /// Map ID this vendor spawn belongs to (0=Felucca, 1=Trammel, etc.)
        /// </summary>
        public int MapId { get; set; }

        /// <summary>
        /// True if this is a sign-based vendor location, false for hive locations.
        /// </summary>
        public bool IsSign { get; set; }

        /// <summary>
        /// Sign type (only relevant if IsSign is true).
        /// Determines which shop sign graphic to look for.
        /// </summary>
        public SignTypes Sign { get; set; }

        /// <summary>
        /// Facing direction for the sign (only relevant if IsSign is true).
        /// </summary>
        public FacingTypes Facing { get; set; }

        /// <summary>
        /// 3D world coordinates of the vendor spawn location.
        /// </summary>
        public Point3D Location { get; set; }

        /// <summary>
        /// List of vendor class names that can spawn at this location.
        /// </summary>
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

        /// <summary>
        /// Adds a vendor to this location's vendor list.
        /// </summary>
        /// <param name="name">Vendor class name to add</param>
        public void AddVendor(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                VendorList.Add(name);
            }
        }

        /// <summary>
        /// Removes a vendor from this location's vendor list.
        /// </summary>
        /// <param name="name">Vendor class name to remove</param>
        public void RemoveVendor(string name)
        {
            VendorList.Remove(name);
        }

        /// <summary>
        /// Replaces the entire vendor list with a new list.
        /// </summary>
        /// <param name="vendors">New list of vendor class names</param>
        public void SetVendors(List<string> vendors)
        {
            VendorList = vendors ?? [];
        }
    }
}
