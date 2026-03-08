using ModernUO.Serialization;
using System;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Custom.UORespawnServer.Items;

namespace Server.Custom.UORespawnServer.Mobiles;
[CorpseName("a rift wisp corpse")]
[SerializationGenerator(0, false)]
internal partial class RiftMob : BaseCreature
{
    private int _damageCollected;

    [Constructible]
    public RiftMob() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
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

    public override void OnDamagedBySpell(Mobile from, int damage)
    {
        _damageCollected++;

        if (_damageCollected < 6)
        {
            if (_damageCollected > 4)
            {
                from.SendMessage(Hue, $"{from.Name}, you feel a strange energy in the air!");
            }

            Effects.SendLocationEffect(Location, Map, 0x375A, 15, 0, 0);
        }

        base.OnDamagedBySpell(from, damage);
    }

    public override bool OnBeforeDeath()
    {
        if (_damageCollected > 4)
        {
            RiftGate gate = new(Location, Map);

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
}
