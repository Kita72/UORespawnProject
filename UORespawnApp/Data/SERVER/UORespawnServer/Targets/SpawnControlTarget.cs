using Server.Mobiles;
using Server.Targeting;

using Server.Custom.UORespawnServer.Services;

namespace Server.Custom.UORespawnServer.Targets
{
    internal class SpawnControlTarget : Target
    {
        private PlayerMobile m_Mobile;
        private ControlService s_Service;

        [Constructable]
        public SpawnControlTarget(PlayerMobile pm, ControlService service) : base(20, true, TargetFlags.None)
        {
            m_Mobile = pm;
            s_Service = service;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (s_Service == null) return;

            if (from == m_Mobile && m_Mobile.AccessLevel >= AccessLevel.GameMaster)
            {
                if (targeted is LandTarget land)
                {
                    s_Service.TryOpenSpawnEditor(m_Mobile, land);
                }
                else if (targeted is StaticTarget decor)
                {
                    s_Service.TryOpenSpawnEditor(m_Mobile, decor);
                }
                else if (targeted is Item item)
                {
                    s_Service.TryOpenSpawnEditor(m_Mobile, item);
                }
                else
                {
                    m_Mobile.SendMessage(53, "No Spawn Date There, Try again!");

                    m_Mobile.Target = new SpawnControlTarget(m_Mobile, s_Service);
                }
            }
        }
    }
}
