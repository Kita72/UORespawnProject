using System;

using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Custom.UORespawnSystem.Items;

namespace Server.Custom.UORespawnSystem.Mobiles
{
    [CorpseName("a rift wisp corpse")]
    internal class RiftMob : BaseCreature
    {
        int damageCollected;

        [Constructable]
        public RiftMob() : base(AIType.AI_NecroMage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a rift wisp";
            Body = 165;
            BaseSoundID = 466;

            SetStr(196, 225);
            SetDex(196, 225);
            SetInt(196, 225);

            SetHits(218, 235);

            SetDamage(18, 20);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 20, 40);
            SetResistance(ResistanceType.Cold, 10, 30);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 50, 70);

            SetSkill(SkillName.EvalInt, 80.0);
            SetSkill(SkillName.Magery, 80.0);
            SetSkill(SkillName.MagicResist, 80.0);
            SetSkill(SkillName.Tactics, 80.0);
            SetSkill(SkillName.Wrestling, 80.0);
            SetSkill(SkillName.Necromancy, 80.0);
            SetSkill(SkillName.SpiritSpeak, 80.0);

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 40;

            AddItem(new LightSource());
        }

        public RiftMob(Serial serial) : base(serial)
        {
        }

        public override InhumanSpeech SpeechType
        {
            get
            {
                return InhumanSpeech.Wisp;
            }
        }

        public override TimeSpan ReacquireDelay
        {
            get
            {
                return TimeSpan.FromSeconds(1.0);
            }
        }

        public override void OnAfterSpawn()
        {
            UpdateHue();

            base.OnAfterSpawn();
        }

        public override void OnAfterMove(Point3D oldLocation)
        {
            UpdateHue();

            base.OnAfterMove(oldLocation);
        }

        private void UpdateHue()
        {
            if (Map == Map.Trammel && Hue != 2750)
            {
                Hue = 2750;
            }

            if (Map == Map.Felucca && Hue != 2728)
            {
                Hue = 2728;
            }
        }

        public override void OnDamagedBySpell(Mobile from)
        {
            damageCollected++;

            if (damageCollected < 6)
            {
                if (damageCollected > 4)
                {
                    from.SendMessage(Hue, $"{from.Name}, you feel a strange energy in the air!");
                }

                Effects.SendLocationEffect(Location, Map, 0x375A, 15, 0, 0);
            }

            base.OnDamagedBySpell(from);
        }

        public override bool OnBeforeDeath()
        {
            if (damageCollected > 4)
            {
                RiftGate gate = new RiftGate(Location, Map);

                gate.MoveToWorld(Location, Map);

                Effects.SendBoltEffect(gate);
            }

            return base.OnBeforeDeath();
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Average);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            _ = reader.ReadInt();
        }
    }
}
