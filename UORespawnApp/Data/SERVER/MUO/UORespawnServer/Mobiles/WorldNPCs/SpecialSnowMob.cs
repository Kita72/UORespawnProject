using ModernUO.Serialization;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.UORespawnServer.Mobiles;
[CorpseName("a frost warden corpse")]
[SerializationGenerator(0, false)]
internal partial class SpecialSnowMob : BaseCreature
{
    private static SpecialSnowMob _Active;

    [Constructible]
    public SpecialSnowMob() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "the frost warden";
        Body = 241;
        Hue = 1150;

        BaseSoundID = 367;

        SetStr(600, 750);
        SetDex(100, 140);
        SetInt(200, 260);

        SetHits(2000);

        SetDamage(18, 26);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 50);

        SetResistance(ResistanceType.Physical, 50, 65);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 70, 90);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 45, 60);

        SetSkill(SkillName.MagicResist, 100.0);
        SetSkill(SkillName.Tactics, 110.0);
        SetSkill(SkillName.Wrestling, 110.0);

        Fame = 15000;
        Karma = -15000;

        VirtualArmor = 60;

        IsParagon = true;
    }

    public override void OnAfterSpawn()
    {
        base.OnAfterSpawn();

        // Enforce single active instance
        if (_Active != null && !_Active.Deleted && _Active != this)
        {
            Delete();
            return;
        }

        string tile = UOR_Utility.GetTileName(Map, Location);

        // Ensure spawn only on snow
        if (!(!string.IsNullOrEmpty(tile) && tile.ToLower() == "snow"))
        {
            Delete();
            return;
        }

        _Active = this;

        PublicOverheadMessage(MessageType.Emote, 0x3B2, true, "A chill wind heralds the Frost Warden's arrival!");
    }

    public override void OnDeath(Container c)
    {
        if (_Active == this)
            _Active = null;

        base.OnDeath(c);
    }

    public override void Delete()
    {
        if (_Active == this)
            _Active = null;

        base.Delete();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        // If no active instance tracked, set this (helps after world load)
        if (_Active == null)
            _Active = this;
    }
}
