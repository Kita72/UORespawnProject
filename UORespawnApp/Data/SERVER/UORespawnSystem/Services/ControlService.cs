using System;
using System.IO;
using System.Text;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Gumps;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Service for Control Gump - handles stats, toggles, and settings adjustments.
    /// Follows SRP: Gump only handles UI, this handles all business logic.
    /// </summary>
    internal class ControlService
    {
        private Timer _RefreshTimer;
        private PlayerMobile _ActiveUser;
        internal bool SystemPower { get; private set; }

        internal ControlService()
        {
            UOR_Utility.SendMsg(ConsoleColor.Green, $"CONTROLS-[Loaded]");

            SystemPower = true;
        }

        #region Gump Management

        internal void OpenGump(PlayerMobile pm)
        {
            _ActiveUser = pm;

            pm.CloseGump(typeof(ControlGump));
            pm.SendGump(new ControlGump(pm, this));

            StartRefresh();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"CONTROLS GUMP-[{pm.Name} Accessed]");
        }

        internal void CloseGump()
        {
            StopRefresh();

            _ActiveUser = null;
        }

        internal void EditSpawn()
        {
            // TODO: Add target Tile, Self->(Box or Region) for Spawn Menu's
            // TODO: Add target Sign, Hive for Spawn Menu's

            _ActiveUser?.SendMessage(0x35, "Spawn Editing - Coming Soon!");
        }

        private void StartRefresh()
        {
            StopRefresh();

            _RefreshTimer = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), RefreshGump);
        }

        private void StopRefresh()
        {
            _RefreshTimer?.Stop();
            _RefreshTimer = null;
        }

        private void RefreshGump()
        {
            if (_ActiveUser == null || _ActiveUser.Deleted || _ActiveUser.NetState == null)
            {
                StopRefresh();
                return;
            }

            _ActiveUser.CloseGump(typeof(ControlGump));
            _ActiveUser.SendGump(new ControlGump(_ActiveUser, this));
        }

        #endregion

        #region System Stats

        internal int GetPlayerCount()
        {
            return UOR_Core.GetRespawners(out var list) ? list.Count : 0;
        }

        internal int GetAllSpawnCount()
        {
            return UOR_Utility.GetAllSpawn()?.Count ?? 0;
        }

        internal int GetQueuedCount()
        {
            int total = 0;

            if (UOR_Core.GetRespawners(out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    total += list[i].GetQueCount();
                }
            }

            return total;
        }

        internal int GetRecycledCount()
        {
            return UOR_Core.GetRecycledCount();
        }

        internal bool GetIsPaused() => UOR_Core.IsPaused;
        internal bool GetIsLocked() => UOR_Core.IsLocked;
        internal string GetVersion() => UOR_Settings.VERSION;

        #endregion

        #region System Toggles

        internal void ToggleLock()
        {
            UOR_Core.ToggleLock();
        }

        internal void ToggleDebug()
        {
            UOR_Settings.ENABLE_DEBUG = !UOR_Settings.ENABLE_DEBUG;
        }

        internal void ToggleEffects()
        {
            UOR_Settings.ENABLE_SPAWN_EFFECTS = !UOR_Settings.ENABLE_SPAWN_EFFECTS;
        }

        internal void ToggleTownSpawn()
        {
            UOR_Settings.ENABLE_TOWN_SPAWN = !UOR_Settings.ENABLE_TOWN_SPAWN;
        }

        internal void ToggleGraveSpawn()
        {
            UOR_Settings.ENABLE_GRAVE_SPAWN = !UOR_Settings.ENABLE_GRAVE_SPAWN;
        }

        internal void ToggleRiftSpawn()
        {
            UOR_Settings.ENABLE_RIFT_SPAWN = !UOR_Settings.ENABLE_RIFT_SPAWN;
        }

        internal void ToggleVendorSpawn()
        {
            UOR_Settings.ENABLE_VENDOR_SPAWN = !UOR_Settings.ENABLE_VENDOR_SPAWN;

            UOR_Core.UpdateVendorService();
        }

        internal void ToggleVendorNight()
        {
            if (UOR_Settings.ENABLE_VENDOR_SPAWN)
            {
                UOR_Settings.ENABLE_VENDOR_NIGHT = !UOR_Settings.ENABLE_VENDOR_NIGHT;
            }
        }

        internal void ToggleVendorExtra()
        {
            if (UOR_Settings.ENABLE_VENDOR_SPAWN)
            {
                UOR_Settings.ENABLE_VENDOR_EXTRA = !UOR_Settings.ENABLE_VENDOR_EXTRA;

                UOR_Core.UpdateVendorService();
            }
        }

        internal void ToggleScaleSpawn()
        {
            UOR_Settings.ENABLE_SCALE_SPAWN = !UOR_Settings.ENABLE_SCALE_SPAWN;
        }

        #endregion

        #region Value Adjustments

        // Scale Modifier
        internal void AdjustScaleMod(double delta)
        {
            double newVal = Math.Max(0.1, Math.Min(5.0, UOR_Settings.SCALE_MOD + delta));

            UOR_Settings.UpdateScaleMod(newVal);
        }

        // Intervals
        internal void AdjustSearchInterval(int delta)
        {
            UOR_Settings.SEARCH_INTERVAL = Math.Max(100, Math.Min(2000, UOR_Settings.SEARCH_INTERVAL + delta));
        }

        internal void AdjustProcessInterval(int delta)
        {
            UOR_Settings.PROCESS_INTERVAL = Math.Max(100, Math.Min(2000, UOR_Settings.PROCESS_INTERVAL + delta));
        }

        internal void AdjustValidateInterval(int delta)
        {
            UOR_Settings.VALIDATE_INTERVAL = Math.Max(5, Math.Min(60, UOR_Settings.VALIDATE_INTERVAL + delta));
        }

        internal void AdjustTimedInterval(int delta)
        {
            UOR_Settings.TIMED_INTERVAL = Math.Max(1, Math.Min(60, UOR_Settings.TIMED_INTERVAL + delta));
        }

        // Limits
        internal void AdjustMaxSpawn(int delta)
        {
            UOR_Settings.MAX_SPAWN_VAL = Math.Max(5, Math.Min(50, UOR_Settings.MAX_SPAWN_VAL + delta));
        }

        internal void AdjustMaxRange(int delta)
        {
            UOR_Settings.MAX_RANGE_VAL = Math.Max(20, Math.Min(100, UOR_Settings.MAX_RANGE_VAL + delta));
        }

        internal void AdjustMinRange(int delta)
        {
            UOR_Settings.MIN_RANGE_VAL = Math.Max(5, Math.Min(30, UOR_Settings.MIN_RANGE_VAL + delta));
        }

        internal void AdjustMaxCrowd(int delta)
        {
            UOR_Settings.MAX_CROWD_VAL = Math.Max(1, Math.Min(10, UOR_Settings.MAX_CROWD_VAL + delta));
        }

        internal void AdjustMaxQueueSize(int delta)
        {
            UOR_Settings.MAX_QUEUE_SIZE = Math.Max(5, Math.Min(50, UOR_Settings.MAX_QUEUE_SIZE + delta));
        }

        // Chances
        internal void AdjustChanceWater(double delta)
        {
            UOR_Settings.CHANCE_WATER = Math.Max(0.0, Math.Min(1.0, UOR_Settings.CHANCE_WATER + delta));
        }

        internal void AdjustChanceWeather(double delta)
        {
            UOR_Settings.CHANCE_WEATHER = Math.Max(0.0, Math.Min(1.0, UOR_Settings.CHANCE_WEATHER + delta));
        }

        internal void AdjustChanceTimed(double delta)
        {
            UOR_Settings.CHANCE_TIMED = Math.Max(0.0, Math.Min(1.0, UOR_Settings.CHANCE_TIMED + delta));
        }

        internal void AdjustChanceCommon(double delta)
        {
            UOR_Settings.CHANCE_COMMON = Math.Max(0.0, Math.Min(1.0, UOR_Settings.CHANCE_COMMON + delta));
        }

        internal void AdjustChanceUncommon(double delta)
        {
            UOR_Settings.CHANCE_UNCOMMON = Math.Max(0.0, Math.Min(1.0, UOR_Settings.CHANCE_UNCOMMON + delta));
        }

        internal void AdjustChanceRare(double delta)
        {
            UOR_Settings.CHANCE_RARE = Math.Max(0.0, Math.Min(1.0, UOR_Settings.CHANCE_RARE + delta));
        }

        #endregion

        #region Save Settings

        internal void SaveSettings()
        {
            var sb = new StringBuilder();

            sb.AppendLine("# UORespawn Settings - Modified In-Game");
            sb.AppendLine($"# Saved: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // System Intervals
            sb.AppendLine("# System Intervals");
            sb.AppendLine($"SEARCH_INTERVAL,{UOR_Settings.SEARCH_INTERVAL}");
            sb.AppendLine($"PROCESS_INTERVAL,{UOR_Settings.PROCESS_INTERVAL}");
            sb.AppendLine($"VALIDATE_INTERVAL,{UOR_Settings.VALIDATE_INTERVAL}");
            sb.AppendLine($"TIMED_INTERVAL,{UOR_Settings.TIMED_INTERVAL}");
            sb.AppendLine();

            // System Limits
            sb.AppendLine("# System Limits");
            sb.AppendLine($"MAX_RECYCLE_TYPE,{UOR_Settings.MAX_RECYCLE_TYPE}");
            sb.AppendLine($"MAX_RECYCLE_TOTAL,{UOR_Settings.MAX_RECYCLE_TOTAL}");
            sb.AppendLine($"MAX_SPAWN_CHECKS,{UOR_Settings.MAX_SPAWN_CHECKS}");
            sb.AppendLine($"MAX_QUEUE_SIZE,{UOR_Settings.MAX_QUEUE_SIZE}");
            sb.AppendLine($"MAX_STAT_SIZE,{UOR_Settings.MAX_STAT_SIZE}");
            sb.AppendLine();

            // Spawn Limits
            sb.AppendLine("# Spawn Limits");
            sb.AppendLine($"MAX_SPAWN,{UOR_Settings.MAX_SPAWN_VAL}");
            sb.AppendLine($"MIN_RANGE,{UOR_Settings.MIN_RANGE_VAL}");
            sb.AppendLine($"MAX_RANGE,{UOR_Settings.MAX_RANGE_VAL}");
            sb.AppendLine($"MAX_CROWD,{UOR_Settings.MAX_CROWD_VAL}");
            sb.AppendLine($"SCALE_MOD,{UOR_Settings.SCALE_MOD:F2}");
            sb.AppendLine();

            // Spawn Chances
            sb.AppendLine("# Spawn Chances");
            sb.AppendLine($"CHANCE_WATER,{UOR_Settings.CHANCE_WATER:F2}");
            sb.AppendLine($"CHANCE_WEATHER,{UOR_Settings.CHANCE_WEATHER:F2}");
            sb.AppendLine($"CHANCE_TIMED,{UOR_Settings.CHANCE_TIMED:F2}");
            sb.AppendLine($"CHANCE_COMMON,{UOR_Settings.CHANCE_COMMON:F2}");
            sb.AppendLine($"CHANCE_UNCOMMON,{UOR_Settings.CHANCE_UNCOMMON:F2}");
            sb.AppendLine($"CHANCE_RARE,{UOR_Settings.CHANCE_RARE:F2}");
            sb.AppendLine();

            // Spawn Toggles
            sb.AppendLine("# Spawn Toggles");
            sb.AppendLine($"ENABLE_SCALE_SPAWN,{UOR_Settings.ENABLE_SCALE_SPAWN}");
            sb.AppendLine($"ENABLE_RIFT_SPAWN,{UOR_Settings.ENABLE_RIFT_SPAWN}");
            sb.AppendLine($"ENABLE_TOWN_SPAWN,{UOR_Settings.ENABLE_TOWN_SPAWN}");
            sb.AppendLine($"ENABLE_GRAVE_SPAWN,{UOR_Settings.ENABLE_GRAVE_SPAWN}");
            sb.AppendLine();

            // Vendor Toggles
            sb.AppendLine("# Vendor Toggles");
            sb.AppendLine($"ENABLE_VENDOR_SPAWN,{UOR_Settings.ENABLE_VENDOR_SPAWN}");
            sb.AppendLine($"ENABLE_VENDOR_NIGHT,{UOR_Settings.ENABLE_VENDOR_NIGHT}");
            sb.AppendLine($"ENABLE_VENDOR_EXTRA,{UOR_Settings.ENABLE_VENDOR_EXTRA}");
            sb.AppendLine();

            // Other Toggles
            sb.AppendLine("# Other Toggles");
            sb.AppendLine($"ENABLE_SPAWN_EFFECTS,{UOR_Settings.ENABLE_SPAWN_EFFECTS}");
            sb.AppendLine($"ENABLE_DEBUG,{UOR_Settings.ENABLE_DEBUG}");

            File.WriteAllText(UOR_DIR.SETTINGS_DATA_FILE, sb.ToString());

            UOR_Utility.SendMsg(ConsoleColor.Green, $"CONTROLS-[Saved]");

            _ActiveUser?.SendMessage(0x35, "Settings saved to file!");
        }

        internal void TogglePower()
        {
            SystemPower = !SystemPower;

            if (SystemPower)
            {
                UOR_Core.ToggleLock();

                UOR_Core.Initialize();

                UOR_Core.RelogPlayers();
            }
            else
            {
                UOR_Core.SHUTDOWN();
            }

            World.Save();
        }

        #endregion
    }
}
