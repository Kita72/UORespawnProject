using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Mobiles;
[SerializationGenerator(0, false)]
internal partial class TownNPC : BaseCreature
{
    [SerializableField(0)]
    private NPCTypes _npcType;

    public override bool ShowFameTitle => true;

    public override bool ClickTitle => true;

    public override bool CanTeach => false;

    public override bool IsInvulnerable => true;

    public override void OnCombatantChange()
    {
        Combatant = null;

        base.OnCombatantChange();
    }

    [Constructible]
    public TownNPC() : base(AIType.AI_Vendor, FightMode.None, 10, 1)
    {
        _npcType = Utility.RandomList
            (
                NPCTypes.Merchant,
                NPCTypes.Mage,
                NPCTypes.Scout,
                NPCTypes.Adventurer,
                NPCTypes.Elitist,
                NPCTypes.Peasant,
                NPCTypes.Cleric
            );

        Title = $"the {_npcType}";

        InitStats(31, 41, 51);

        Hue = Race.Human.RandomSkinHue();

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

        NPCUtility.SetDress(this, _npcType);

        NPCUtility.SetHair(this);

        YellowHealthbar = false;

        Container pack = new Backpack
        {
            Movable = false
        };

        AddItem(pack);
    }

    public override void OnAfterMove(Point3D oldLocation)
    {
        if (Utility.RandomDouble() < 0.01)
        {
            Say(NPCUtility.GetRandomSpeech());
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
}
