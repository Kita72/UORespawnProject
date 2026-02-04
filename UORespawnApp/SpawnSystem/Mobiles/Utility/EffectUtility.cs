using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.SpawnSystem.Mobiles
{
    internal enum UOREffects
    {
        None,
        Wave,
        Fire,
        Wind,
        Mist,
        Poison,
        Electric,
        Explosion,
        Smoke,
        Glow,
        Confetti,
        Magic
    }

    internal static class EffectUtility
    {
        // Waves
        private static readonly int waterAnim = Utility.RandomList(0x1FA3, 0x1FA8, 0x1FAD, 0x1FB2, 0x1FB7, 0x1FBC, 0x1FC1, 0x1FC6);

        // Flame
        private static readonly int flameAnim = Utility.RandomList(0x3709, 0x3E27, 0x3E31, 0x3996, 0x398C);

        // Fire
        private static readonly int fireAnim = Utility.RandomList(0x0DE3, 0x19AB);

        // Lava
        private static readonly int lavaAnim = Utility.RandomList(0x3459, 0x1A75);

        // Swamp Bubble
        private static readonly int swampAnim = 0x322C;

        // Poison Field
        private static readonly int poisonfieldAnim = Utility.RandomList(0x3914, 0x3920);

        // Poison Gas
        private static readonly int poisongasAnim = 0x11A6;

        // Sparkles
        private static readonly int sparkleAnim = Utility.RandomList(0x373A, 0x375A, 0x376A, 0x3779);

        // Snow
        private static readonly int snowAnim = Utility.RandomList(0x1153, 0x1158);

        // Wind
        private static readonly int windAnim = Utility.RandomList(0xA7E3, 0xA7F1);

        // Tornado
        private static readonly int tornadoAnim = 0x37CC;

        // Symbols
        private static readonly int symbolAnim = Utility.RandomList(0x0E5C, 0x0E5F, 0x0E62, 0x0E65, 0x0E68);

        // Magic Field
        private static readonly int magicfieldAnim = Utility.RandomList(0x3979, 0x3967);

        // Swirling Mist
        private static readonly int swirlingmistAnim = 0x3789;

        // Sparks
        private static readonly int sparkAnim = Utility.RandomList(0xA652, 0xA657);

        // Explosion
        private static readonly int explosionAnim = Utility.RandomList(0x36B0, 0x36BD);

        // Red Sparkles
        private static readonly int redsparkleAnim = Utility.RandomList(0x42CF, 0x374A);

        // Smoke
        private static readonly int smokeAnim = Utility.RandomList(0x3728, 0x9DAC);

        // Fizzle
        private static readonly int fizzleAnim = 0x3735;

        // Glow
        private static readonly int glowAnim = Utility.RandomList(0x37B9, 0x37C4);

        // Energy
        private static readonly int energyAnim = 0x3818;

        // Confetti
        private static readonly int confettiAnim = 0x9F89;

        internal static UOREffects SetSpawnEffect(Mobile m)
        {
            if (m.Map == Map.Internal) return UOREffects.None;

            string tile = SpawnSysUtility.TryGetWetName(m.Map, m.Location);

            if (string.IsNullOrEmpty(tile) || tile == "NoName")
            {
                tile = SpawnSysTileInfo.GetTileName(new LandTarget(m.Location, m.Map).TileID);
            }

            switch (tile)
            {
                case "water":
                    {
                        m.CanSwim = true;

                        return UOREffects.Wave;
                    }

                case "sand":
                    {
                        return UOREffects.Wind;
                    }

                case "grass":
                    {
                        return UOREffects.Wind;
                    }

                case "snow":
                    {
                        return UOREffects.Wind;
                    }

                case "blood":
                    {
                        return UOREffects.Mist;
                    }

                case "jungle":
                    {
                        return UOREffects.Mist;
                    }

                case "swamp":
                    {
                        return UOREffects.Poison;
                    }

                case "forest":
                    {
                        return UOREffects.Smoke;
                    }

                case "marble":
                    {
                        return UOREffects.Magic;
                    }

                default: return UOREffects.None;
            }
        }

        internal static void TryRunEffect(Mobile m, UOREffects effect)
        {
            if (effect != UOREffects.None && Utility.RandomDouble() < 0.50)
            {
                switch (effect)
                {
                    case UOREffects.Wave:
                        SetEffect(m, UOREffects.Wave, Utility.RandomBlueHue());
                        break;
                    case UOREffects.Fire:
                        SetEffect(m, UOREffects.Fire, 0);
                        break;
                    case UOREffects.Wind:
                        SetEffect(m, UOREffects.Wind, 0x4000);
                        break;
                    case UOREffects.Mist:
                        SetEffect(m, UOREffects.Mist, Utility.RandomRedHue());
                        break;
                    case UOREffects.Poison:
                        SetEffect(m, UOREffects.Poison, Utility.RandomGreenHue());
                        break;
                    case UOREffects.Electric:
                        SetEffect(m, UOREffects.Electric, 1153);
                        break;
                    case UOREffects.Explosion:
                        SetEffect(m, UOREffects.Explosion, 0);
                        break;
                    case UOREffects.Smoke:
                        SetEffect(m, UOREffects.Smoke, 0);
                        break;
                    case UOREffects.Glow:
                        SetEffect(m, UOREffects.Glow, Utility.RandomMetalHue());
                        break;
                    case UOREffects.Confetti:
                        SetEffect(m, UOREffects.Confetti, Utility.RandomBrightHue());
                        break;
                    case UOREffects.Magic:
                        SetEffect(m, UOREffects.Magic, Utility.RandomBrightHue());
                        break;
                }

                var mobs = m.GetMobilesInRange(1);

                foreach (var mob in mobs)
                {
                    if (mob is PlayerMobile pm)
                    {
                        if (effect == UOREffects.Glow || effect == UOREffects.Confetti || effect == UOREffects.Magic)
                        {
                            pm.SendMessage(42, $"You gained {pm.Heal(Utility.Random(pm.Hits / 10))} hitpoints!");
                        }
                        else
                        {
                            pm.SendMessage(42, $"You lost {pm.Damage(Utility.Random(pm.Hits / 10))} hitpoints!");
                        }
                    }
                }

                mobs.Free();
            }
        }

        internal static void SetEffect(Mobile m, UOREffects effect, int hue)
        {
            switch (effect)
            {
                case UOREffects.Wave:
                    {
                        if (Utility.RandomDouble() < 0.01)
                        {
                            m.BoltEffect(hue);
                        }
                        else
                        {
                            Run(m.Location, m.Map, waterAnim, Utility.RandomMinMax(30, 60), hue);
                        }

                        break;
                    }
                case UOREffects.Fire:
                    {
                        if (Utility.RandomDouble() < 0.1)
                        {
                            if (Utility.RandomBool())
                            {
                                Run(m.Location, m.Map, flameAnim, Utility.RandomMinMax(30, 60), hue);
                            }
                            else
                            {
                                Run(m.Location, m.Map, lavaAnim, Utility.RandomMinMax(30, 60), hue);
                            }
                        }
                        else
                        {
                            Run(m.Location, m.Map, fireAnim, Utility.RandomMinMax(30, 60), hue);
                        }

                        break;
                    }
                case UOREffects.Wind:
                    {
                        if (Utility.RandomDouble() < 0.01)
                        {
                            Run(m.Location, m.Map, tornadoAnim, Utility.RandomMinMax(30, 60), 0x4000);
                        }
                        else
                        {
                            if (Utility.RandomBool())
                            {
                                Run(m.Location, m.Map, snowAnim, Utility.RandomMinMax(30, 60), hue);
                            }
                            else
                            {
                                Run(m.Location, m.Map, windAnim, Utility.RandomMinMax(30, 60), 0x4000);
                            }
                        }

                        break;
                    }
                case UOREffects.Mist:
                    {
                        if (Utility.RandomDouble() < 0.01)
                        {
                            Run(m.Location, m.Map, swirlingmistAnim, Utility.RandomMinMax(30, 60), hue);
                        }
                        else
                        {
                            Run(m.Location, m.Map, redsparkleAnim, Utility.RandomMinMax(30, 60), hue);
                        }

                        break;
                    }
                case UOREffects.Poison:
                    {
                        if (Utility.RandomDouble() < 0.01)
                        {
                            Run(m.Location, m.Map, poisonfieldAnim, Utility.RandomMinMax(30, 60), hue);
                        }
                        else
                        {
                            if (Utility.RandomBool())
                            {
                                Run(m.Location, m.Map, poisongasAnim, Utility.RandomMinMax(30, 60), hue);
                            }
                            else
                            {
                                Run(m.Location, m.Map, swampAnim, Utility.RandomMinMax(30, 60), hue);
                            }
                        }

                        break;
                    }
                case UOREffects.Electric:
                    {
                        if (Utility.RandomDouble() < 0.01)
                        {
                            m.BoltEffect(hue);
                        }
                        else
                        {
                            if (Utility.RandomBool())
                            {
                                Run(m.Location, m.Map, energyAnim, Utility.RandomMinMax(30, 60), hue);
                            }
                            else
                            {
                                Run(m.Location, m.Map, sparkAnim, Utility.RandomMinMax(30, 60), hue);
                            }
                        }

                        break;
                    }
                case UOREffects.Explosion:
                    {
                        if (Utility.RandomDouble() < 0.01)
                        {
                            Run(m.Location, m.Map, explosionAnim, Utility.RandomMinMax(30, 60), hue);
                        }
                        else
                        {
                            Run(m.Location, m.Map, lavaAnim, Utility.RandomMinMax(30, 60), hue);
                        }

                        break;
                    }
                case UOREffects.Smoke:
                    {
                        if (Utility.RandomDouble() < 0.01)
                        {
                            Run(m.Location, m.Map, smokeAnim, Utility.RandomMinMax(30, 60), hue);
                        }
                        else
                        {
                            Run(m.Location, m.Map, fizzleAnim, Utility.RandomMinMax(30, 60), hue);
                        }

                        break;
                    }
                case UOREffects.Glow:
                    {
                        if (Utility.RandomDouble() < 0.01)
                        {
                            Run(m.Location, m.Map, glowAnim, Utility.RandomMinMax(30, 60), hue);
                        }
                        else
                        {
                            Run(m.Location, m.Map, symbolAnim, Utility.RandomMinMax(30, 60), hue);
                        }

                        break;
                    }
                case UOREffects.Confetti:
                    {
                        if (Utility.RandomDouble() < 0.01)
                        {
                            Run(m.Location, m.Map, confettiAnim, Utility.RandomMinMax(30, 60), hue);
                        }
                        else
                        {
                            Run(m.Location, m.Map, symbolAnim, Utility.RandomMinMax(30, 60), hue);
                        }

                        break;
                    }
                case UOREffects.Magic:
                    {
                        if (Utility.RandomDouble() < 0.01)
                        {
                            Run(m.Location, m.Map, magicfieldAnim, Utility.RandomMinMax(30, 60), hue);
                        }
                        else
                        {
                            if (Utility.RandomBool())
                            {
                                Run(m.Location, m.Map, sparkleAnim, Utility.RandomMinMax(30, 60), hue);
                            }
                            else
                            {
                                Run(m.Location, m.Map, symbolAnim, Utility.RandomMinMax(30, 60), hue);
                            }
                        }

                        break;
                    }
            }
        }

        private static void Run(Point3D loc, Map map, int id, int duration, int hue, int render = 0)
        {
            Effects.SendLocationEffect(loc, map, id, duration, hue, render);
        }
    }
}
