using System;

using Server.Regions;
using Server.Mobiles;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Mobiles;
using Server.Custom.UORespawnServer.Helpers;

namespace Server.Custom.UORespawnServer.Entities
{
    /// <summary>
    /// Holds spawn data for a queued spawn while it is active in the system.
    /// Contains location, environment, and frequency data used to determine what to spawn.
    /// </summary>
    internal class SpawnEntity
    {
        internal string Name { get; private set; }

        internal Map Facet { get; private set; }
        internal Point3D Location { get; set; }
        internal Region RegionName { get; private set; }
        internal string TileName { get; private set; }
        internal int TileId { get; private set; }
        internal Rectangle2D SpawnBox { get; private set; }
        internal double DiceRoll { get; private set; }

        internal FrequencyTypes FrequencyType { get; set; } = FrequencyTypes.Common;
        internal WaterTypes WaterType { get; set; } = WaterTypes.Shallow;
        internal WeatherTypes WeatherType { get; set; } = WeatherTypes.None;
        internal TimeTypes TimeType { get; set; } = TimeTypes.Noon;
        internal SpawnTypes SpawnType { get; set; } = SpawnTypes.None;

        internal bool IsWater { get; set; }
        internal bool IsWeather { get; set; }
        internal bool IsNight { get; set; }
        internal bool IsTown { get; set; }
        internal bool IsDungeon { get; set; }
        internal bool IsLava { get; set; } = false;

        public SpawnEntity(Map map, Point3D location)
        {
            Facet = map;

            Location = location;

            SpawnBox = UOR_Utility.GetSpawnBox(Location, UOR_Settings.MIN_RANGE);

            DiceRoll = Utility.RandomDouble();

            UpdateEntity();
        }

        private void UpdateEntity()
        {
            SetTile();

            SetRegion();

            SetTime();

            SetWater();

            SetWeather();

            SetFrequency();

            SetSpawn();
        }

        private void SetTile()
        {
            var tile = UOR_Utility.GetTile(Facet, Location);

            TileId = tile.ID;

            TileName = IsLava ? "lava" : TileHelper.GetTileName(TileId, Facet, Location);

            IsWater = TileName == "water";
        }

        private void SetRegion()
        {
            RegionName = UOR_Utility.GetRegion(Facet, Location);

            if (RegionName != null && RegionName.Name != null)
            {
                IsTown = RegionName.IsPartOf(typeof(TownRegion));
                IsDungeon = RegionName.IsPartOf(typeof(DungeonRegion));
            }
        }

        private void SetTime()
        {
            TimeType = UOR_Utility.GetTime(Facet, Location);
        }

        private void SetWater()
        {
            if (IsWater)
            {
                WaterType = TileHelper.GetWaterType(TileId);

                if (WaterType == WaterTypes.Shallow)
                {
                    Location = new Point3D(Location.X, Location.Y, (Location.Z + 5));
                }
            }
        }

        private void SetWeather()
        {
            IsWeather = UOR_Utility.HasWeather(Facet, SpawnBox, out WeatherTypes weatherType);

            if (IsWeather)
            {
                WeatherType = weatherType;
            }
        }

        private void SetFrequency()
        {
            if (IsWater)
            {
                return;
            }

            if (UOR_Utility.GetTime(Facet, Location) == TimeType && DiceRoll < UOR_Settings.CHANCE_TIMED)
            {
                FrequencyType = FrequencyTypes.Timed;
            }
            else if (IsWeather && DiceRoll < UOR_Settings.CHANCE_WEATHER)
            {
                FrequencyType = FrequencyTypes.Weather;
            }
            else if (DiceRoll < UOR_Settings.CHANCE_RARE)
            {
                FrequencyType = FrequencyTypes.Rare;
            }
            else if (DiceRoll < UOR_Settings.CHANCE_UNCOMMON)
            {
                FrequencyType = FrequencyTypes.UnCommon;
            }
            else
            {
                if (DiceRoll < UOR_Settings.CHANCE_COMMON) // User can set < 100%! need to have fallback!
                {
                    FrequencyType = FrequencyTypes.Common;
                }
                else
                {
                    FrequencyType = Utility.RandomList(FrequencyTypes.Common, FrequencyTypes.UnCommon, FrequencyTypes.Rare);
                }
            }
        }

        private void SetSpawn()
        {
            IsNight = UOR_Utility.IsNight(Facet, Location);

            Name = BoxSpawner.TryBoxSpawn(this);

            if (!string.IsNullOrEmpty(Name))
            {
                SpawnType = SpawnTypes.Box;

                return;
            }

            if (IsDungeon || IsTown)
            {
                Name = RegionSpawner.TryRegionSpawn(this);

                SpawnType = SpawnTypes.Region;

                return;
            }

            Name = RegionSpawner.TryRegionSpawn(this);

            if (!string.IsNullOrEmpty(Name))
            {
                SpawnType = SpawnTypes.Region;

                return;
            }

            Name = TileSpawner.TryTileSpawn(this);

            SpawnType = SpawnTypes.Tile;

            // In case we fail to get name!
            if (string.IsNullOrEmpty(Name))
            {
                if (UOR_Settings.ENABLE_DEBUG)
                {
                    Name = nameof(PlaceHolder);

                    UOR_Utility.SendMsg(ConsoleColor.Yellow, "SetSpawn() : [PlaceHolder]");
                }
                else
                {
                    Name = nameof(WanderingHealer);

                    UOR_Utility.SendMsg(ConsoleColor.Yellow, "SetSpawn() : [WanderingHealer]");
                }
            }
        }
    }
}
