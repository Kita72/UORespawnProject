using ModernUO.Serialization;
using Server.Mobiles;
using System.Collections.Generic;

using Server.Custom.UORespawnServer.Spawners;

namespace Server.Custom.UORespawnServer.Mobiles;
[SerializationGenerator(0, false)]
internal partial class AmbushNPC : SkeletalKnight
{
    [Constructible]
    public AmbushNPC()
    {
        Name = "The Leader";

        CantWalk = true;

        Hidden = true;

        IsParagon = true;
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
                        member = gangType switch
                        {
                            0 => AmbushUtility.GetUndeadAmbush(),
                            1 => AmbushUtility.GetLizardAmbush(),
                            2 => AmbushUtility.GetRatAmbush(),
                            3 => AmbushUtility.GetOrcAmbush(),
                            _ => AmbushUtility.GetBrigandAmbush(),
                        };
                        break;
                    }
                case nameof(Map.Trammel):
                    {
                        member = gangType switch
                        {
                            0 => AmbushUtility.GetUndeadAmbush(),
                            1 => AmbushUtility.GetLizardAmbush(),
                            2 => AmbushUtility.GetRatAmbush(),
                            3 => AmbushUtility.GetOrcAmbush(),
                            _ => AmbushUtility.GetKhaldunAmbush(),
                        };
                        break;
                    }
                case nameof(Map.Ilshenar):
                    {
                        member = gangType switch
                        {
                            0 => AmbushUtility.GetUndeadAmbush(),
                            1 => AmbushUtility.GetLizardAmbush(),
                            2 => AmbushUtility.GetSavageAmbush(),
                            3 => AmbushUtility.GetJukaAmbush(),
                            _ => AmbushUtility.GetTitanAmbush(),
                        };
                        break;
                    }
                case nameof(Map.Malas):
                    {
                        member = gangType switch
                        {
                            0 => AmbushUtility.GetUndeadAmbush(),
                            1 => AmbushUtility.GetNecroAmbush(),
                            2 => AmbushUtility.GetRatAmbush(),
                            3 => AmbushUtility.GetSavageAmbush(),
                            _ => AmbushUtility.GetCrystalAmbush(),
                        };
                        break;
                    }
                case nameof(Map.Tokuno):
                    {
                        member = gangType switch
                        {
                            0 => AmbushUtility.GetUndeadAmbush(),
                            1 => AmbushUtility.GetSavageAmbush(),
                            2 => AmbushUtility.GetRatAmbush(),
                            3 => AmbushUtility.GetYomotsuAmbush(),
                            _ => AmbushUtility.GetNinjaAmbush(),
                        };
                        break;
                    }
                case nameof(Map.TerMur):
                    {
                        member = gangType switch
                        {
                            0 => AmbushUtility.GetUndeadAmbush(),
                            1 => AmbushUtility.GetLizardAmbush(),
                            2 => AmbushUtility.GetRatAmbush(),
                            3 => AmbushUtility.GetOrcAmbush(),
                            _ => AmbushUtility.GetKepetchAmbush(),
                        };
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
        // Assign ISpawner for tracking - ambush spawn is owned by UOR_MobSpawner
        if (member is BaseCreature bc)
        {
            UOR_MobSpawner.Instance.Claim(bc);
        }

        member.OnBeforeSpawn(Location, Map);
        var location = Utility.Random(8) switch
        {
            0 => new Point3D(Location.X, Location.Y - i, Location.Z),
            1 => new Point3D(Location.X + i, Location.Y - i, Location.Z),
            2 => new Point3D(Location.X + i, Location.Y, Location.Z),
            3 => new Point3D(Location.X + i, Location.Y + i, Location.Z),
            4 => new Point3D(Location.X, Location.Y + i, Location.Z),
            5 => new Point3D(Location.X - i, Location.Y + i, Location.Z),
            6 => new Point3D(Location.X - i, Location.Y, Location.Z),
            7 => new Point3D(Location.X - i, Location.Y - i, Location.Z),
            _ => new Point3D(Location.X + i, Location.Y + i, Location.Z),
        };
        member.MoveToWorld(location, Map);

        member.Say("ATTACK!");

        member.OnAfterSpawn();

        member.Combatant = victim;

        GangList.Add(member);
    }

    private readonly List<Mobile> GangList = [];

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
                    GangList[^1].Delete();

                    GangList.Remove(GangList[^1]);
                }
            }

            Delete();
        }

        base.OnCombatantChange();
    }
}
