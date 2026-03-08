using Server.Mobiles;
using Server.Targeting;

using Server.Custom.UORespawnServer.Services;

namespace Server.Custom.UORespawnServer.Targets;
internal class SpawnControlTarget : Target
{
    private readonly PlayerMobile _mobile;
    private readonly ControlService _service;

    public SpawnControlTarget(PlayerMobile pm, ControlService service) : base(20, true, TargetFlags.None)
    {
        _mobile = pm;
        _service = service;
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_service == null)
        {
            return;
        }

        if (from == _mobile && _mobile.AccessLevel >= AccessLevel.GameMaster)
        {
            if (targeted is LandTarget land)
            {
                _service.TryOpenSpawnEditor(_mobile, land);
            }
            else if (targeted is StaticTarget decor)
            {
                _service.TryOpenSpawnEditor(_mobile, decor);
            }
            else if (targeted is Item item)
            {
                _service.TryOpenSpawnEditor(_mobile, item);
            }
            else
            {
                _mobile.SendMessage(53, "No Spawn Date There, Try again!");

                _mobile.Target = new SpawnControlTarget(_mobile, _service);
            }
        }
    }
}
