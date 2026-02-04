using Server.Items;
using Server.Mobiles;

namespace Server.Custom.SpawnSystem.Mobiles
{
    internal enum NPCTypes
    {
        None,
        Merchant,
        Mage,
        Scout,
        Adventurer,
        Elitist,
        Peasant,
        Cleric
    }

    internal static class NPCUtility
    {
        internal static void SetDress(BaseCreature bc, NPCTypes npcType)
        {
            bool canEquip = true;

            if (SpawnSysUtility.IsNight(bc))
            {
                if (Utility.RandomBool())
                {
                    bc.AddItem(new Lantern() { Burning = true });
                }
                else
                {
                    bc.AddItem(new Torch() { Burning = true });
                }

                canEquip = false;
            }

            if (bc.Female)
            {
                switch (npcType)
                {
                    case NPCTypes.Merchant:
                        {
                            bc.AddItem(new FeatheredHat(Utility.RandomNondyedHue()));

                            bc.AddItem(new PlainDress(Utility.RandomNondyedHue()));

                            bc.AddItem(new ThighBoots());

                            break;
                        }
                    case NPCTypes.Mage:
                        {
                            bc.AddItem(new WizardsHat(Utility.RandomNondyedHue()));

                            bc.AddItem(new Robe(Utility.RandomNondyedHue()));

                            bc.AddItem(new Sandals(Utility.RandomNondyedHue()));

                            if (canEquip)
                            {
                                bc.AddItem(new Spellbook());
                            }

                            break;
                        }
                    case NPCTypes.Scout:
                        {
                            bc.AddItem(new Bonnet(Utility.RandomNondyedHue()));

                            bc.AddItem(new Shirt(Utility.RandomNondyedHue()));

                            bc.AddItem(new Skirt(Utility.RandomNondyedHue()));

                            bc.AddItem(new Shoes(Utility.RandomNondyedHue()));

                            break;
                        }
                    case NPCTypes.Adventurer:
                        {
                            bc.AddItem(new SkullCap(Utility.RandomNondyedHue()));

                            bc.AddItem(new FancyShirt(Utility.RandomNondyedHue()));

                            bc.AddItem(new ShortPants(Utility.RandomNondyedHue()));

                            bc.AddItem(new ThighBoots());

                            bc.AddItem(new Cloak(Utility.RandomNondyedHue()));

                            if (canEquip)
                            {
                                bc.AddItem(new Longsword());
                            }

                            break;
                        }
                    case NPCTypes.Elitist:
                        {
                            bc.AddItem(new FloppyHat(Utility.RandomNondyedHue()));

                            bc.AddItem(new FancyDress(Utility.RandomNondyedHue()));

                            bc.AddItem(new Sandals(Utility.RandomNondyedHue()));

                            bc.AddItem(new Cloak(Utility.RandomNondyedHue()));

                            break;
                        }
                    case NPCTypes.Peasant:
                        {
                            bc.AddItem(new WideBrimHat());

                            bc.AddItem(new PlainDress());

                            bc.AddItem(new Sandals());

                            break;
                        }
                    case NPCTypes.Cleric:
                        {
                            bc.AddItem(new WizardsHat(Utility.RandomNondyedHue()));

                            bc.AddItem(new Robe(Utility.RandomNondyedHue()));

                            bc.AddItem(new Sandals(Utility.RandomNondyedHue()));

                            if (canEquip)
                            {
                                bc.AddItem(new GnarledStaff());
                            }

                            break;
                        }
                    default:
                        {
                            bc.AddItem(new PlainDress());

                            bc.AddItem(new Shoes());

                            break;
                        }
                }
            }
            else
            {
                switch (npcType)
                {
                    case NPCTypes.Merchant:
                        {
                            bc.AddItem(new FeatheredHat(Utility.RandomNondyedHue()));

                            bc.AddItem(new Shirt(Utility.RandomNondyedHue()));

                            bc.AddItem(new LongPants(Utility.RandomNondyedHue()));

                            bc.AddItem(new BodySash(Utility.RandomNondyedHue()));

                            bc.AddItem(new Boots());

                            break;
                        }
                    case NPCTypes.Mage:
                        {
                            bc.AddItem(new WizardsHat(Utility.RandomNondyedHue()));

                            bc.AddItem(new Robe(Utility.RandomNondyedHue()));

                            bc.AddItem(new Sandals(Utility.RandomNondyedHue()));

                            if (canEquip)
                            {
                                bc.AddItem(new Spellbook());
                            }

                            break;
                        }
                    case NPCTypes.Scout:
                        {
                            bc.AddItem(new Bonnet(Utility.RandomNondyedHue()));

                            bc.AddItem(new Shirt(Utility.RandomNondyedHue()));

                            bc.AddItem(new ShortPants(Utility.RandomNondyedHue()));

                            bc.AddItem(new Shoes(Utility.RandomNondyedHue()));

                            break;
                        }
                    case NPCTypes.Adventurer:
                        {
                            bc.AddItem(new SkullCap(Utility.RandomNondyedHue()));

                            bc.AddItem(new FancyShirt(Utility.RandomNondyedHue()));

                            bc.AddItem(new ShortPants(Utility.RandomNondyedHue()));

                            bc.AddItem(new Boots());

                            bc.AddItem(new Cloak(Utility.RandomNondyedHue()));

                            bc.AddItem(new BodySash(Utility.RandomNondyedHue()));

                            if (canEquip)
                            {
                                bc.AddItem(new Longsword());
                            }

                            break;
                        }
                    case NPCTypes.Elitist:
                        {
                            bc.AddItem(new FloppyHat(Utility.RandomNondyedHue()));

                            bc.AddItem(new FancyShirt(Utility.RandomNondyedHue()));

                            bc.AddItem(new LongPants(Utility.RandomNondyedHue()));

                            bc.AddItem(new Sandals(Utility.RandomNondyedHue()));

                            bc.AddItem(new Cloak(Utility.RandomNondyedHue()));

                            break;
                        }
                    case NPCTypes.Peasant:
                        {
                            bc.AddItem(new Bandana());

                            bc.AddItem(new Shirt());

                            bc.AddItem(new ShortPants());

                            bc.AddItem(new Sandals());

                            break;
                        }
                    case NPCTypes.Cleric:
                        {
                            bc.AddItem(new WizardsHat(Utility.RandomNondyedHue()));

                            bc.AddItem(new Robe(Utility.RandomNondyedHue()));

                            bc.AddItem(new Sandals(Utility.RandomNondyedHue()));

                            if (canEquip)
                            {
                                bc.AddItem(new GnarledStaff());
                            }

                            break;
                        }
                    default:
                        {
                            bc.AddItem(new Shirt());

                            bc.AddItem(new LongPants());

                            bc.AddItem(new Shoes());

                            break;
                        }
                }
            }
        }

        internal static void SetHair(BaseCreature bc)
        {
            Utility.AssignRandomHair(bc);

            bc.HairHue = Utility.RandomHairHue();

            if (!bc.Female)
            {
                Utility.AssignRandomFacialHair(bc);

                bc.FacialHairHue = bc.HairHue;
            }
        }

        internal static string GetRandomSpeech()
        {
            switch (Utility.Random(50))
            {
                case 0: return "Greetings, traveler! Have you seen the mystical runes scattered across our lands?";
                case 1: return "In the moonlight, the shadows dance like ancient spirits.";
                case 2: return "I've heard tales of a legendary warrior who once walked these streets.";
                case 3: return "Do you feel the magic in the air, or is it just my imagination?";
                case 4: return "The tavern here serves a drink that warms the soul on a cold night.";
                case 5: return "The local bards sing tales of brave adventurers and distant lands.";
                case 6: return "Legends speak of a powerful artifact hidden deep within the mountains.";
                case 7: return "Sometimes, I wonder what lies beyond the horizon.";
                case 8: return "A wise sage once told me, 'Patience is the key to unlocking mysteries.'";
                case 9: return "Do you believe in the power of luck and fortune?";
                case 10: return "The moonstone fountain in the center of town has an enchanting glow at night.";
                case 11: return "I overheard whispers of a secret passage beneath the old cathedral.";
                case 12: return "The local alchemist concocts potions rumored to grant extraordinary abilities.";
                case 13: return "Ancient ruins hold the echoes of a forgotten civilization.";
                case 14: return "Have you ever danced with the fairies in the moonlit glade?";
                case 15: return "The guildmasters gather in solemn meetings, discussing matters of great importance.";
                case 16: return "Beyond the city gates lies a forest teeming with mysterious creatures.";
                case 17: return "Rumors say a wise old sage resides atop the tallest tower in the city.";
                case 18: return "The starry night sky reveals the secrets of the cosmos.";
                case 19: return "Whispers of a lost city hidden in the clouds reach my ears.";
                case 20: return "Have you met the mysterious wanderer who sells rare trinkets?";
                case 21: return "The ancient oak tree in the town square is said to have witnessed centuries pass.";
                case 22: return "The local blacksmith forges weapons with a touch of magic.";
                case 23: return "The sea breeze carries tales of undiscovered islands across the ocean.";
                case 24: return "Legends speak of a mystical portal that transports adventurers to distant realms.";
                case 25: return "I often dream of soaring through the skies on the back of a majestic griffin.";
                case 26: return "The local apothecary brews potions that can heal wounds and cure ailments.";
                case 27: return "The wise old librarian guards the ancient tomes in the city's grand library.";
                case 28: return "Have you ever attended the magical masquerade ball in the castle?";
                case 29: return "Rumors say a ghost ship sails the seas, crewed by lost souls seeking redemption.";
                case 30: return "The echoes of laughter from the town square bring joy to weary hearts.";
                case 31: return "I've heard of a hidden cave where rare crystals glow with ethereal light.";
                case 32: return "The constellation of the phoenix guides those in search of rebirth.";
                case 33: return "The town guard trains tirelessly to protect us from lurking dangers.";
                case 34: return "The ancient stone circle in the woods is said to possess mystical energies.";
                case 35: return "In the mystic hour of dusk, the city gates cast long shadows.";
                case 36: return "The local healer's herbs are said to have miraculous properties.";
                case 37: return "The wise old owl in the town square is rumored to give sage advice.";
                case 38: return "Rumors tell of a hidden garden where flowers bloom even in winter.";
                case 39: return "The town crier spreads news of distant lands and epic quests.";
                case 40: return "The moonlit path through the graveyard is both eerie and enchanting.";
                case 41: return "The glistening lake to the east hides secrets beneath its tranquil surface.";
                case 42: return "Legends speak of an immortal bard who wanders the land, singing tales of heroes.";
                case 43: return "The mystical fog in the forest conceals passages to otherworldly realms.";
                case 44: return "The ancient standing stones hold the memories of bygone eras.";
                case 45: return "Rumors say the old well in the town square grants wishes to those who dare to dream.";
                case 46: return "The local sculptor creates statues that seem to come alive in the moonlight.";
                case 47: return "Have you witnessed the celestial dance of the northern lights?";
                case 48: return "The town's fortune teller sees glimpses of destiny in her crystal ball.";
                case 49: return "I once glimpsed a majestic unicorn in the enchanted glade to the west.";
            }

            return "What a beautiful day!";
        }
    }
}
