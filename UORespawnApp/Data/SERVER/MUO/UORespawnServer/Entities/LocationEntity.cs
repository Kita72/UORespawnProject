using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Entities;
internal class LocationEntity
{
    internal string PLAYER { get; set; }
    internal Region REGION { get; set; }
    internal Point3D LOCATION { get; set; }
    internal bool VALID { get; set; }
    internal int ATTEMPTS { get; set; }
    internal string REASON { get; set; }
    internal double CHANCE { get; private set; }

    public LocationEntity(PlayerMobile pm)
    {
        PLAYER = pm.Name;

        CHANCE = Utility.RandomDouble();
    }
}
