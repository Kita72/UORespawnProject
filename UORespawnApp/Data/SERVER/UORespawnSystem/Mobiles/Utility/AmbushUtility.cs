using Server.Mobiles;

namespace Server.Custom.UORespawnSystem.Mobiles
{
    internal static class AmbushUtility
    {
        internal static Mobile GetBrigandAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new BrigandCannibalMage() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new BrigandCannibal() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new Brigand() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetUndeadAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new SkelementalMage() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new SkelementalKnight() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new Skeleton() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetRatAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new RatmanMage() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new RatmanArcher() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new Ratman() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetLizardAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new LizardmanWitchdoctor() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new LizardmanDefender() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new Lizardman() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetOrcAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new OrcishMage() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new OrcChopper() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new OrcScout() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetKhaldunAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new KhaldunSummoner() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new KhaldunBlood() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new KhaldunZealot() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetJukaAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new JukaLord() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new JukaMage() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new JukaWarrior() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetTitanAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new Titan() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new Cyclops() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new Ettin() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetSavageAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new SavageShaman() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new SavageRidgeback() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new Savage() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetNecroAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new EvilMageLord() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new EvilMage() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new Zombie() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetCrystalAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new CrystalDaemon() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new CrystalElemental() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new CrystalWisp() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetNinjaAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new EliteNinja() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new Samurai() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new Ninja() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetYomotsuAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new YomotsuElder() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new YomotsuPriest() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new YomotsuWarrior() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }

        internal static Mobile GetKepetchAmbush()
        {
            if (Utility.RandomDouble() < 0.1)
            {
                if (Utility.RandomDouble() < 0.1)
                {
                    return new KepetchAmbusher() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
                else
                {
                    return new Kepetch() { IsParagon = Utility.RandomDouble() < 0.01 };
                }
            }
            else
            {
                return new Korpre() { IsParagon = Utility.RandomDouble() < 0.01 };
            }
        }
    }
}
