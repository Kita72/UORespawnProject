using System;

using Server.Items;
using Server.Mobiles;

namespace Server.Custom.UORespawnSystem.Mobiles
{
    internal class TownNPC : BaseCreature
    {
        private NPCTypes npcType = NPCTypes.None;

        public override bool ShowFameTitle => true;

        public override bool ClickTitle => true;

        public override bool CanTeach => false;

        public override bool IsInvulnerable => true;

        public override void OnCombatantChange()
        {
            Combatant = null;

            base.OnCombatantChange();
        }

        [Constructable]
        public TownNPC() : base(AIType.AI_Vendor, FightMode.None, 10, 1, 0.2, 0.4)
        {
            npcType = Utility.RandomList
                (
                    NPCTypes.Merchant,
                    NPCTypes.Mage,
                    NPCTypes.Scout,
                    NPCTypes.Adventurer,
                    NPCTypes.Elitist,
                    NPCTypes.Peasant,
                    NPCTypes.Cleric
                );

            Title = $"the {npcType}";

            InitStats(31, 41, 51);

            Hue = Utility.RandomSkinHue();

            SpeechHue = Utility.RandomDyedHue();

            if (Female = Utility.RandomBool())
            {
                Body = 0x191;

                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;

                Name = NameList.RandomName("male");
            }

            NPCUtility.SetDress(this, npcType);

            NPCUtility.SetHair(this);

            YellowHealthbar = false;

            Container pack = new Backpack
            {
                Movable = false
            };

            AddItem(pack);
        }

        public TownNPC(Serial serial) : base(serial)
        {
        }

        public override void OnAfterMove(Point3D oldLocation)
        {
            if (Utility.RandomDouble() < 0.01)
            {
                Say(NPCUtility.GetRandomSpeech(), Utility.RandomNondyedHue());
            }

            base.OnAfterMove(oldLocation);
        }

        public override void OnAfterSpawn()
        {
            Criminal = Map == Map.Felucca;

            Karma = Map == Map.Felucca? Utility.RandomMinMax(-1000, -20000) : Utility.RandomMinMax(1000, 20000);

            Fame = Utility.RandomMinMax(1000, 10000);

            base.OnAfterSpawn();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write(npcType.ToString());
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version > -1)
            {
                if (Enum.TryParse(reader.ReadString(), out NPCTypes result))
                {
                    npcType = result;
                }
            }
        }
    }
}
