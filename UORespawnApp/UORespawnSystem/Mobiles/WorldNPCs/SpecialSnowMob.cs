using Server.Mobiles;
using Server.Items;
using Server.Custom.UORespawnSystem.SpawnHelpers;

namespace Server.Custom.UORespawnSystem.Mobiles
{
    [CorpseName("a frost warden corpse")]
    internal class SpecialSnowMob : BaseCreature
    {
        private static SpecialSnowMob _Active;

        [Constructable]
        public SpecialSnowMob() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
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

        public SpecialSnowMob(Serial serial) : base(serial)
        {
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

            // Ensure spawn only in snow-type areas (region name contains "snow" or tile type is snow)
            bool allowed = false;

            try
            {
                var region = Region.Find(Location, Map);

                if (region != null && !string.IsNullOrEmpty(region.Name) && region.Name.ToLower().Contains("snow"))
                    allowed = true;

                if (!allowed)
                {
                    string tile = SpawnWaterInfo.TryGetWetName(Map, Location);

                    if (string.IsNullOrEmpty(tile) || tile == "NoName")
                        tile = SpawnTileInfo.GetTileName(Map.Tiles.GetLandTile(Location.X, Location.Y).ID);

                    if (!string.IsNullOrEmpty(tile) && tile.ToLower() == "snow")
                        allowed = true;
                }
            }
            catch
            {
                allowed = false;
            }

            if (!allowed)
            {
                Delete();
                return;
            }

            _Active = this;

            PublicOverheadMessage(Network.MessageType.Emote, 0x3B2, true, "A chill wind heralds the Frost Warden's arrival!");
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

        public static bool IsActive()
        {
            return _Active != null && !_Active.Deleted;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            _ = reader.ReadInt();

            // If no active instance tracked, set this (helps after world load)
            if (_Active == null)
                _Active = this;
        }
    }
}
