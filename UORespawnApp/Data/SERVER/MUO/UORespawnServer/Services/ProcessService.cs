using System;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Mobiles;
using Server.Custom.UORespawnServer.Timers;
using Server.Custom.UORespawnServer.Helpers;

namespace Server.Custom.UORespawnServer.Services;
/// <summary>
/// Processes spawn queue and creates mobs in the world.
/// Uses SpawnQueryService to find relocatable spawn before creating new.
/// ISpawner ownership is assigned automatically via AddSpawnToWorld.
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
        {
            return;
        }

        // Try to find relocatable spawn of same type (out-of-sight, furthest away)
        var spawn = UOR_Core.GetRelocatable(entity.Name, out bool isRelocated);

        if (spawn == null)
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"PROCESS-[{entity.Name} Failed]");

            return;
        }

        // Reset relocated spawn stats for fresh encounter
        if (isRelocated)
        {
            ResetSpawnForRelocation(spawn);
        }

        // AddSpawnToWorld assigns ISpawner ownership automatically
        Create(pm, spawn, entity.Facet, entity.Location, isRelocated);

        _SpawnCount++;

        // Chance for extra spawns
        if (entity.DiceRoll < 0.01)
        {
            SpawnExtras(pm, entity);
        }
    }

    /// <summary>
    /// Resets a relocated spawn for fresh encounter.
    /// </summary>
    private static void ResetSpawnForRelocation(Mobile spawn)
    {
        if (spawn is BaseCreature bc)
        {
            bc.Hits = bc.HitsMax;
            bc.Mana = bc.ManaMax;
            bc.Stam = bc.StamMax;
            bc.Combatant = null;
            bc.Warmode = false;
            bc.FocusMob = null;
            bc.InvalidateProperties();
        }
    }

    private static void SpawnExtras(PlayerMobile pm, SpawnEntity entity)
    {
        Mobile extra;

        if (entity.IsWater)
        {
            extra = UOR_Utility.CreateSpawn(SpawnHelper.GetWaterSpawn(entity.WaterType));

            Create(pm, extra, entity.Facet, entity.Location);

            return;
        }

        if (UOR_Settings.ENABLE_GRAVE_SPAWN && entity.IsNight)
        {
            var grave = UOR_Utility.GetStatic(entity.Facet, entity.Location, "grave") ??
                        UOR_Utility.GetStatic(entity.Facet, entity.Location, "gravestone");

            if (grave != null)
            {
                extra = UOR_Utility.CreateSpawn(SpawnHelper.GetUndeadSpawn());

                Create(pm, extra, entity.Facet, entity.Location);

                return;
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

                    return;
                }
            }
        }

        if (UOR_Settings.ENABLE_TOWN_SPAWN && entity.IsTown)
        {
            entity.Location = UOR_Utility.GetSpawnPoint(entity.Location, 3, 10, entity.Facet, out bool isWater, out _);

            if (!isWater)
            {
                extra = new TownNPC();

                Create(pm, extra, entity.Facet, entity.Location);

                return;
            }
        }

        }

    /// <summary>
    /// Create spawn in world. ISpawner ownership assigned via AddSpawnToWorld.
    /// </summary>
    private static void Create(PlayerMobile pm, Mobile spawn, Map map, Point3D location, bool isRelocated = false)
    {
        if (spawn == null)
        {
            return;
        }

        // AddSpawnToWorld assigns UOR_MobSpawner.Instance.Claim() automatically
        UOR_Utility.AddSpawnToWorld(spawn, map, location, isRelocated);

        // Stats tracking (separate from spawn tracking)
        UOR_Core.AddStat(pm, spawn);
    }
}
