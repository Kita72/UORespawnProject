using Server.Regions;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Mobiles;
using System;
using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Entities
{
    // Used to hold spawn data while spawn active in the system!
    internal class SpawnEntity
    {
        // Spawn
        internal string Name { get; private set; }

        // Location Data
        internal Map Facet { get; private set; }
        internal Point3D Location { get; set; }
        internal Region RegionName { get; private set; }
        internal string TileName { get; private set; }

        // Spawn Data
        internal FrequencyTypes FrequencyType { get; set; } = FrequencyTypes.Common;
        internal WaterTypes WaterType { get; set; } = WaterTypes.Shallow;
        internal WeatherTypes WeatherType { get; set; } = WeatherTypes.None;
        internal TimeTypes TimeType { get; set; } = TimeTypes.Noon;
        internal SpawnTypes SpawnType { get; set; } = SpawnTypes.None;

        // Spawn Quick Checks
        internal bool IsWater { get; set; }
        internal bool IsWeather { get; set; }
        internal bool IsNight { get; set; }
        internal bool IsTown { get; set; }
        internal bool IsDungeon { get; set; }

        public SpawnEntity(Map map, Point3D location)
        {
            Facet = map;
            Location = location;

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

            SetSpawnType();

            if (string.IsNullOrEmpty(Name))
            {
                SetSpawn();
            }
        }

        internal bool HasLocation(Point3D location)
        {
            return GetSpawnBox().Contains(location);
        }

        internal Rectangle2D GetSpawnBox()
        {
            return UOR_Utility.GetSpawnBox(Location, UOR_Settings.MIN_RANGE);
        }

        private void SetTile()
        {
            TileName = UOR_Utility.GetTileName(Facet, Location);
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
            IsNight = UOR_Utility.IsNight(Facet, Location);

            TimeType = UOR_Utility.GetTime(Facet, Location);
        }

        private void SetWater()
        {
            IsWater = UOR_Utility.HasWater(Facet, Location, out WaterTypes waterType);

            if (IsWater)
            {
                WaterType = waterType;
            }
        }

        private void SetWeather()
        {
            IsWeather = UOR_Utility.HasWeather(Facet, Location, out WeatherTypes weatherType);

            if (IsWeather)
            {
                WeatherType = weatherType;
            }
        }

        private void SetFrequency()
        {
            if (IsWater)
            {
                FrequencyType = FrequencyTypes.Water;

                return;
            }

            var roll = Utility.RandomDouble();

            if (Utility.RandomBool() && roll < UOR_Settings.CHANCE_TIMED)
            {
                FrequencyType = FrequencyTypes.Timed;
            }
            else if (IsWeather && roll < UOR_Settings.CHANCE_WEATHER)
            {
                FrequencyType = FrequencyTypes.Weather;
            }
            else if (roll < UOR_Settings.CHANCE_RARE)
            {
                FrequencyType = FrequencyTypes.Rare;
            }
            else if (roll < UOR_Settings.CHANCE_UNCOMMON)
            {
                FrequencyType = FrequencyTypes.UnCommon;
            }
            else
            {
                if (roll < UOR_Settings.CHANCE_COMMON)
                {
                    FrequencyType = FrequencyTypes.Common;
                }
                else
                {
                    FrequencyType = Utility.RandomList(FrequencyTypes.Common, FrequencyTypes.UnCommon, FrequencyTypes.Rare);
                }
            }
        }

        private void SetSpawnType()
        {
            if (SpawnType == SpawnTypes.None)
            {
                switch (Utility.Random(3))
                {
                    case 1: SpawnType = SpawnTypes.Box; break;
                    case 2: SpawnType = SpawnTypes.Region; break;
                    default: SpawnType = SpawnTypes.Tile; break;
                }
            }
        }

        private void SetSpawn()
        {
            switch (SpawnType)
            {
                case SpawnTypes.Box: Name = BoxSpawner.TryBoxSpawn(this);
                    break;
                case SpawnTypes.Region: Name = RegionSpawner.TryRegionSpawn(this);
                    break;
                case SpawnTypes.Tile: Name = TileSpawner.TryTileSpawn(this);
                    break;
            }

            // In case we fail to get name!
            if (string.IsNullOrEmpty(Name))
            {
                Name = TileSpawner.TryTileSpawn(this);

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

        internal void OnAfterSpawn()
        {
            UOR_Utility.ReleaseQueuedSpawnLocation(Facet, Location);
        }
    }
}
