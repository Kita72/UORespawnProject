using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Server.Mobiles;
using Server.Commands;
using Server.Custom.SpawnSystem.Mobiles;

using static Server.Custom.SpawnSystem.SpawnSysSettings;

namespace Server.Custom.SpawnSystem
{
    internal static class SpawnSysCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("PushRespawn", AccessLevel.Administrator, new CommandEventHandler(PushSpawn_OnCommand));
            CommandSystem.Register("DebugRespawn", AccessLevel.Administrator, new CommandEventHandler(ToggleDebug_OnCommand));
            CommandSystem.Register("TrackRespawn", AccessLevel.Administrator, new CommandEventHandler(TrackRespawn_OnCommand));
            CommandSystem.Register("ClearRespawn", AccessLevel.Administrator, new CommandEventHandler(ClearRespawn_OnCommand));
            CommandSystem.Register("GenRespawnList", AccessLevel.Administrator, new CommandEventHandler(GenSpawnList_OnCommand));
            CommandSystem.Register("GenStaticList", AccessLevel.Administrator, new CommandEventHandler(GenStaticList_OnCommand));
            CommandSystem.Register("GenSpawnerList", AccessLevel.Administrator, new CommandEventHandler(GenSpawnerList_OnCommand));
            CommandSystem.Register("ReloadRespawn", AccessLevel.Administrator, new CommandEventHandler(ReloadSpawn_OnCommand));
            CommandSystem.Register("PushRespawnStats", AccessLevel.Administrator, new CommandEventHandler(PushRespawnStats_OnCommand));
        }

        // PushRespawn
        [Usage("PushRespawn")]
        [Description("UORespawn: Push Spawn")]
        public static void PushSpawn_OnCommand(CommandEventArgs e)
        {
            SpawnSysCore.UpdateWorldSpawn();
        }

        // DebugRespawn
        [Usage("DebugRespawn")]
        [Description("UORespawn: Turn Debug On/Off")]
        public static void ToggleDebug_OnCommand(CommandEventArgs e)
        {
            ENABLE_DEBUG = !ENABLE_DEBUG;

            string state = ENABLE_DEBUG ? "ON" : "OFF";

            e.Mobile.SendMessage($"UORespawn Debug - {state}");
        }

        // TrackRespawn
        [Usage("TrackRespawn")]
        [Description("UORespawn: View Tracked Spawn Statistics")]
        public static void TrackRespawn_OnCommand(CommandEventArgs e)
        {
            int trackedCount = SpawnSysCore.GetTrackedSpawnCount();

            var (ValidCount, DeletedCount) = SpawnSysCore.GetTrackedSpawnDetails();

            e.Mobile.SendMessage(68, "===== UORespawn Tracking Statistics =====");
            e.Mobile.SendMessage(85, $"Total Tracked: {trackedCount}");
            e.Mobile.SendMessage(85, $"Valid Spawns: {ValidCount}");
            e.Mobile.SendMessage(85, $"Deleted/Invalid: {DeletedCount}");
            e.Mobile.SendMessage(68, "=========================================");

            SpawnSysUtility.SendConsoleMsg(ConsoleColor.Yellow, $"{e.Mobile.Name} Checked tracking - {trackedCount} total, {ValidCount} valid");
        }

        // ClearRespawn
        [Usage("ClearRespawn")]
        [Description("UORespawn: Clear All Tracked Spawns (Deletes all spawned creatures)")]
        public static void ClearRespawn_OnCommand(CommandEventArgs e)
        {
            int deletedCount = SpawnSysCore.ClearAllTrackedSpawns();

            e.Mobile.SendMessage(38, $"UORespawn: Cleared and deleted {deletedCount} tracked spawns!");

            SpawnSysUtility.SendConsoleMsg(ConsoleColor.Yellow, $"{e.Mobile.Name} Cleared all tracked spawns - {deletedCount} deleted");
        }

        // GenRespawnList
        [Usage("GenRespawnList")]
        [Description("UORespawn: Gen Bestiary List")]
        public static void GenSpawnList_OnCommand(CommandEventArgs e)
        {
            SaveClassesToFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_BestiaryList.txt"), typeof(BaseCreature), e.Mobile);
        }

        private static void SaveClassesToFile(string filePath, Type type, Mobile m)
        {
            try
            {
                var allTypes = Assembly.GetExecutingAssembly().GetTypes();

                var types = allTypes.Where(t => IsValidSpawn(t, type)).ToList();

                if (types.Count > 0)
                {
                    File.WriteAllLines(filePath, types.Select(t => t.Name).ToList());
                }
            }
            catch (Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.Red, $"List Failed to Generate? : {ex}");

                return;
            }

            m.SendMessage("List Generated!");
        }

        private static bool IsValidSpawn(Type t, Type type)
        {
            if (t.Name == nameof(RiftMob) || t.Name == nameof(PlaceHolder)) { return false; }

            if (t.Name.EndsWith("EffectNPC") || t.Name == nameof(AmbushNPC)) { return true; }

            if (t.IsClass)
            {
                if (!t.IsAbstract)
                {
                    if (t.BaseType == type)
                    {
                        return t.GetConstructors().Any(c => c.GetParameters().Length == 0);
                    }
                }
            }

            return false;
        }

        // GenStaticList
        [Usage("GenStaticList")]
        [Description("UORespawn: Gen Statics List")]
        public static void GenStaticList_OnCommand(CommandEventArgs e)
        {
            List<string> statics = new List<string>();

            try
            {
                string name;

                for (int i = 0; i < TileData.MaxItemValue; i++)
                {
                    name = TileData.ItemTable[i & TileData.MaxItemValue].Name;

                    if (!string.IsNullOrEmpty(name) && !statics.Contains(name))
                    {
                        statics.Add(TileData.ItemTable[i & TileData.MaxItemValue].Name);
                    }
                }
            }
            catch (Exception ex)
            {
                SpawnSysUtility.SendConsoleMsg(ConsoleColor.Red, $"Gen Static Error: {ex}");
            }

            if (statics.Count > 0)
            {
                File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_StaticList.txt"), statics);

                e.Mobile.SendMessage("List Generated!");
            }
            else
            {
                e.Mobile.SendMessage("No Statics Found!");
            }
        }

        // GenSpawnerList
        [Usage("GenSpawnerList")]
        [Description("UORespawn: Gen Spawner List")]
        public static void GenSpawnerList_OnCommand(CommandEventArgs e)
        {
            List<string> allSpawners = new List<string>();

            var spawnerList = World.Items.Values.Where(s => s is ISpawner);

            foreach (var spawner in spawnerList)
            {
                if (spawner is Spawner s && s is ISpawner spwnr)
                {
                    allSpawners.Add($"{s.Map}:{s.X}:{s.Y}:{spwnr.HomeRange}");
                }

                if (spawner is XmlSpawner xml && xml is ISpawner xspwnr)
                {
                    allSpawners.Add($"{xml.Map}:{xml.X}:{xml.Y}:{xspwnr.HomeRange}");
                }
            }

            if (allSpawners.Count > 0)
            {
                File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UOR_SpawnerList.txt"), allSpawners);

                e.Mobile.SendMessage("List Generated!");
            }
            else
            {
                e.Mobile.SendMessage("No Spawners Found!");
            }
        }

        // ReloadRespawn
        [Usage("ReloadRespawn")]
        [Description("UORespawn: Reload Spawn")]
        public static void ReloadSpawn_OnCommand(CommandEventArgs e)
        {
            SpawnSysDataBase.ReLoadSpawns();

            e.Mobile.SendMessage($"UORespawn Reloaded!");
        }

        // PushRespawnStats
        [Usage("PushRespawnStats")]
        [Description("UORespawn: Push Spawn Stats")]
        public static void PushRespawnStats_OnCommand(CommandEventArgs e)
        {
            World.Save();

            e.Mobile.SendMessage($"UORespawn: Spawn Stats Pushed!");
        }
    }
}
