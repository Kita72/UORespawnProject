using System;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Mobiles;
using Server.Custom.UORespawnServer.Timers;
using Server.Custom.UORespawnServer.Helpers;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Processes spawn queue and creates mobs in the world.
    /// ISpawner ownership is assigned automatically via AddSpawnToWorld.
    /// No manual tracking needed - ISpawner handles it.
    /// </summary>
    internal class ProcessService
    {
        private readonly ProcessTimer _ProcessTimer;
        private int _SpawnCount;

        internal ProcessService()
        {
            _ProcessTimer = new ProcessTimer(this, TimeSpan.FromMilliseconds(UOR_Settings.PROCESS_INTERVAL));
            _SpawnCount = 0;

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"PROCESS-[{UOR_Settings.PROCESS_INTERVAL}ms Interval]");
        }

        internal void Start()
        {
            if (!_ProcessTimer.Running)
            {
                _ProcessTimer?.Start();
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"PROCESS-[Started]");
            }
        }

        internal void Stop()
        {
            if (_ProcessTimer.Running)
            {
                _ProcessTimer?.Stop();
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"PROCESS-[Stopped]");
            }
        }

        internal void Spawn(PlayerMobile pm, SpawnEntity entity)
        {
            if (entity == null)
                return;

            var spawn = UOR_Core.GetRecycled(entity.Name, out bool isRecycled);

            if (spawn == null)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"PROCESS-[{entity.Name} Failed]");
                entity.LocationQueryUpdate();
                return;
            }

            // Reset recycled spawn stats
            if (isRecycled)
            {
                spawn.Hits = spawn.HitsMax;
                spawn.Mana = spawn.ManaMax;
                spawn.Stam = spawn.StamMax;
                spawn.Combatant = null;
                spawn.ResetStatTimers();
                spawn.InvalidateProperties();
            }

            // AddSpawnToWorld assigns ISpawner ownership automatically
            Create(pm, spawn, entity.Facet, entity.Location);

            entity.LocationQueryUpdate();
            _SpawnCount++;

            // Chance for extra spawns
            if (Utility.RandomDouble() < 0.01)
            {
                SpawnExtras(pm, entity);
            }
        }

        private void SpawnExtras(PlayerMobile pm, SpawnEntity entity)
        {
            Mobile extra = null;

            if (entity.IsWater)
            {
                extra = UOR_Utility.CreateSpawn(SpawnHelper.GetWaterSpawn(entity.WaterType));
                Create(pm, extra, entity.Facet, entity.Location);
            }

            if (UOR_Settings.ENABLE_GRAVE_SPAWN && entity.IsNight)
            {
                var grave = UOR_Utility.GetStatic(entity.Facet, entity.Location, "grave") ??
                            UOR_Utility.GetStatic(entity.Facet, entity.Location, "gravestone");

                if (grave != null)
                {
                    extra = UOR_Utility.CreateSpawn(SpawnHelper.GetUndeadSpawn());
                    Create(pm, extra, entity.Facet, entity.Location);
                }
            }

            if (entity.IsWeather)
            {
                if (Utility.RandomDouble() < UOR_Settings.CHANCE_WEATHER)
                {
                    switch (entity.WeatherType)
                    {
                        case Enums.WeatherTypes.Rain:
                            EffectUtility.TryRunEffect(pm, UOREffects.Electric);
                            break;
                        case Enums.WeatherTypes.Snow:
                            EffectUtility.TryRunEffect(pm, UOREffects.Wind);
                            break;
                        case Enums.WeatherTypes.Storm:
                            EffectUtility.TryRunEffect(pm, UOREffects.Electric);
                            break;
                        case Enums.WeatherTypes.Blizzard:
                            EffectUtility.TryRunEffect(pm, UOREffects.Wind);
                            break;
                    }

                    if (UOR_Settings.ENABLE_RIFT_SPAWN)
                    {
                        extra = UOR_Utility.CreateSpawn(SpawnHelper.GetWeatherSpawn(entity.WeatherType));
                        Create(pm, extra, entity.Facet, entity.Location);
                    }
                }
            }

            if (UOR_Settings.ENABLE_TOWN_SPAWN && entity.IsTown)
            {
                entity.Location = UOR_Utility.GetSpawnPoint(entity.Location, 3, 10, entity.Facet, out bool isWater);

                if (!isWater)
                {
                    extra = new TownNPC();
                    Create(pm, extra, entity.Facet, entity.Location);
                }
            }

            if (extra != null)
            {
                _SpawnCount++;
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"SPAWNED-[{_SpawnCount} Extra]");
            }
        }

        /// <summary>
        /// Create spawn in world. ISpawner ownership assigned via AddSpawnToWorld.
        /// </summary>
        private void Create(PlayerMobile pm, Mobile spawn, Map map, Point3D location)
        {
            if (spawn == null) 
                return;

            // AddSpawnToWorld assigns UOR_MobSpawner.Instance.Claim() automatically
            UOR_Utility.AddSpawnToWorld(spawn, map, location);

            // Stats tracking (separate from spawn tracking)
            UOR_Core.AddStat(pm, spawn);
        }
    }
}
