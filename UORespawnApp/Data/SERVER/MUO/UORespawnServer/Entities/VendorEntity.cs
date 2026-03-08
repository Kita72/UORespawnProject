using Server.Items;
using System.Collections.Generic;

namespace Server.Custom.UORespawnServer.Entities;
/// <summary>
/// Configuration entity for vendor spawn locations.
/// Contains only configuration data (location, sign info, vendor types).
/// Runtime tracking is handled by ISpawner pattern via UOR_VendorSpawner.
/// </summary>
internal class VendorEntity
{
    public bool IsSign { get; private set; }

    public SignType Sign { get; private set; }

    public SignFacing Facing { get; private set; }

    public Point3D Location { get; private set; }

    /// <summary>
    /// Type names for vendors to spawn (from editor data).
    /// </summary>
    internal List<string> VendorList { get; private set; }

    public VendorEntity(Point3D location)
    {
        IsSign = false;
        Sign = SignType.MetalPost;
        Facing = SignFacing.North;
        Location = GetInsideLocation(Facing, location);
        VendorList = [];
    }

    public VendorEntity(SignType sign, SignFacing facing, Point3D location)
    {
        IsSign = true;
        Sign = sign;
        Facing = facing;
        Location = GetInsideLocation(facing, location);
        VendorList = [];
    }

    /// <summary>
    /// Adds a vendor type name to the spawn list.
    /// </summary>
    public void AddVendor(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            VendorList.Add(name);
        }
    }

    /// <summary>
    /// Removes a vendor type name from the spawn list.
    /// </summary>
    public void RemoveVendor(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            VendorList.Remove(name);
        }
    }

    /// <summary>
    /// Checks if this entity has any vendor types configured.
    /// </summary>
    public bool NeedsSpawn()
    {
        return VendorList.Count > 0;
    }

    private Point3D GetInsideLocation(SignFacing facing, Point3D location)
    {
        if (IsSign)
        {
            return facing switch
            {
                SignFacing.West => new Point3D(location.X - 2, location.Y, location.Z),
                SignFacing.North => new Point3D(location.X, location.Y - 2, location.Z),
                _ => location
            };
        }

        return new Point3D(location.X + 1, location.Y + 1, location.Z);
    }
}
