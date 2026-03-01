namespace Server.Custom.UORespawnServer.Items
{
    internal class DebugFlag : Item
    {
        private string _PlayerName;
        private string _RegionName;
        private string _TileName;
        private string _Reason;

        [Constructable]
        public DebugFlag() : base(0xAA7F)
        {
            Name = "Debug Flag";
            Movable = false;
        }

        internal void SetInfo(string name, string region, string tile, string reason)
        {
            _PlayerName = name;
            _RegionName = region;
            _TileName = tile;
            _Reason = reason;
        }

        public DebugFlag(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage(53, $"{_PlayerName} | {_RegionName} | {_TileName} | {_Reason}");
            from.SendMessage(43, $"Reasons | {_Reason}");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            UOR_Core.AddFlag();

            Delete();
        }
    }
}
