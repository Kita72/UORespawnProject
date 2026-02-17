using System.Linq;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Custom.UORespawnSystem.Mobiles
{
    internal class AmbushNPC : SkeletalKnight
    {
        [Constructable]
        public AmbushNPC() : base()
        {
            Name = "The Leader";

            CantWalk = true;

            Hidden = true;

            IsParagon = true;
        }

        public AmbushNPC(Serial serial) : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (Hidden && Combatant == null)
            {
                SpawnRandomAmbush(m);

                CantWalk = false;

                Hidden = false;

                Combatant = m;

                Say("We have you now!");
            }

            return base.OnMoveOver(m);
        }

        private void SpawnRandomAmbush(Mobile m)
        {
            var gangCount = Utility.RandomMinMax(1, 5);

            var gangType = Utility.Random(5);

            SpawnGang(m, gangCount, gangType);

            if (GangList.Count > 0)
            {
                Body = GangList[0].Body;

                if (Body == 0x190 || Body == 0x191)
                {
                    Female = Body == 0x191;

                    NPCUtility.SetHair(this);

                    NPCUtility.SetDress(this, NPCTypes.Adventurer);
                }
            }
        }

        private void SpawnGang(Mobile m, int gangCount, int gangType)
        {
            Mobile member = null;

            for (int i = 0; i < gangCount; i++)
            {
                switch (Map.Name)
                {
                    case nameof(Map.Felucca):
                        {
                            switch (gangType)
                            {
                                case 0: member = AmbushUtility.GetUndeadAmbush(); break;
                                case 1: member = AmbushUtility.GetLizardAmbush(); break;
                                case 2: member = AmbushUtility.GetRatAmbush(); break;
                                case 3: member = AmbushUtility.GetOrcAmbush(); break;
                                default: member = AmbushUtility.GetBrigandAmbush(); break;
                            }

                            break;
                        }
                    case nameof(Map.Trammel):
                        {
                            switch (gangType)
                            {
                                case 0: member = AmbushUtility.GetUndeadAmbush(); break;
                                case 1: member = AmbushUtility.GetLizardAmbush(); break;
                                case 2: member = AmbushUtility.GetRatAmbush(); break;
                                case 3: member = AmbushUtility.GetOrcAmbush(); break;
                                default: member = AmbushUtility.GetKhaldunAmbush(); break;
                            }

                            break;
                        }
                    case nameof(Map.Ilshenar):
                        {
                            switch (gangType)
                            {
                                case 0: member = AmbushUtility.GetUndeadAmbush(); break;
                                case 1: member = AmbushUtility.GetLizardAmbush(); break;
                                case 2: member = AmbushUtility.GetSavageAmbush(); break;
                                case 3: member = AmbushUtility.GetJukaAmbush(); break;
                                default: member = AmbushUtility.GetTitanAmbush(); break;
                            }

                            break;
                        }
                    case nameof(Map.Malas):
                        {
                            switch (gangType)
                            {
                                case 0: member = AmbushUtility.GetUndeadAmbush(); break;
                                case 1: member = AmbushUtility.GetNecroAmbush(); break;
                                case 2: member = AmbushUtility.GetRatAmbush(); break;
                                case 3: member = AmbushUtility.GetSavageAmbush(); break;
                                default: member = AmbushUtility.GetCrystalAmbush(); break;
                            }

                            break;
                        }
                    case nameof(Map.Tokuno):
                        {
                            switch (gangType)
                            {
                                case 0: member = AmbushUtility.GetUndeadAmbush(); break;
                                case 1: member = AmbushUtility.GetSavageAmbush(); break;
                                case 2: member = AmbushUtility.GetRatAmbush(); break;
                                case 3: member = AmbushUtility.GetYomotsuAmbush(); break;
                                default: member = AmbushUtility.GetNinjaAmbush(); break;
                            }

                            break;
                        }
                    case nameof(Map.TerMur):
                        {
                            switch (gangType)
                            {
                                case 0: member = AmbushUtility.GetUndeadAmbush(); break;
                                case 1: member = AmbushUtility.GetLizardAmbush(); break;
                                case 2: member = AmbushUtility.GetRatAmbush(); break;
                                case 3: member = AmbushUtility.GetOrcAmbush(); break;
                                default: member = AmbushUtility.GetKepetchAmbush(); break;
                            }

                            break;
                        }
                }

                if (member != null)
                {
                    Spawn(member, i, m);
                }
            }
        }

        private void Spawn(Mobile member, int i, Mobile victim)
        {
            member.OnBeforeSpawn(Location, Map);

            Point3D location;

            switch (Utility.Random(8))
            {
                case 0: location = new Point3D(Location.X + i, Location.Y, Location.Z); break;
                case 1: location = new Point3D(Location.X + i, Location.Y - i, Location.Z); break;
                case 3: location = new Point3D(Location.X + i, Location.Y + i, Location.Z); break;
                case 4: location = new Point3D(Location.X, Location.Y + i, Location.Z); break;
                case 5: location = new Point3D(Location.X, Location.Y - i, Location.Z); break;
                case 6: location = new Point3D(Location.X - i, Location.Y + i, Location.Z); break;
                case 7: location = new Point3D(Location.X - i, Location.Y - i, Location.Z); break;
                case 8: location = new Point3D(Location.X - i, Location.Y, Location.Z); break;
                default: location = new Point3D(Location.X + i, Location.Y + i, Location.Z); break;
            }

            member.MoveToWorld(location, Map);

            UORespawnCore.AddSpawnToList(member);

            member.Say("ATTACK!");

            member.OnAfterSpawn();

            member.Combatant = victim;

            GangList.Add(member);
        }

        private readonly List<Mobile> GangList = new List<Mobile>();

        public override void OnCombatantChange()
        {
            if (Combatant == null)
            {
                Say("HIDE!");

                var count = GangList.Count;

                for (int i = 0; i < count; i++)
                {
                    if (GangList.Count > 0)
                    {
                        GangList.Last().Delete();

                        GangList.Remove(GangList.Last());
                    }
                }

                Delete();
            }

            base.OnCombatantChange();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            _= reader.ReadInt();
        }
    }
}
