using System;
using System.IO;
using Server.Engines.Spawners;

namespace Server.Custom.UORespawnServer.Managers;
/// <summary>
/// Processes spawner commands from the UORespawn editor.
/// Commands are written to a text file that the server processes on startup.
/// Supports DELETE operations by serial number.
/// </summary>
internal static class XMLManager
{
    private static readonly string CommandFilePath = Path.Combine(UOR_DIR.INPUT_DIR, "UOR_XmlCommands.txt");

    // Command types
    private const string CMD_DELETE = "DELETE";
    private const string CMD_ADD = "ADD";
    private const string CMD_EDIT = "EDIT";

    /// <summary>
    /// Processes all pending XML spawner commands from the command file.
    /// Called during server startup after world load.
    /// </summary>
    internal static void ProcessCommands()
    {
        if (!File.Exists(CommandFilePath))
        {
            return;
        }

        UOR_Utility.SendMsg(ConsoleColor.Cyan, "XMLCMD-[Processing commands...]");

        int deleted = 0;
        int added = 0;
        int edited = 0;
        int failed = 0;

        try
        {
            string[] lines = File.ReadAllLines(CommandFilePath);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] parts = line.Split('|');

                if (parts.Length < 2)
                {
                    failed++;
                    UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[Invalid command format: {line}]");
                    continue;
                }

                string command = parts[0].ToUpperInvariant();

                switch (command)
                {
                    case CMD_DELETE:
                        if (ProcessDeleteCommand(parts))
                        {
                            deleted++;
                        }
                        else
                        {
                            failed++;
                        }
                        break;

                    case CMD_ADD:
                        if (ProcessAddCommand(parts))
                        {
                            added++;
                        }
                        else
                        {
                            failed++;
                        }

                        break;

                    case CMD_EDIT:
                        if (ProcessEditCommand(parts))
                        {
                            edited++;
                        }
                        else
                        {
                            failed++;
                        }

                        break;

                    default:
                        failed++;
                        UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[Unknown command: {command}]");
                        break;
                }
            }

            // Delete command file after processing to prevent re-execution
            File.Delete(CommandFilePath);

            UOR_Utility.SendMsg(ConsoleColor.Green, $"XMLCMD-[Processed: {deleted} deleted, {added} added, {edited} edited, {failed} failed]");
        }
        catch (Exception ex)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[Error: {ex.Message}]");
        }
    }

    /// <summary>
    /// Processes a DELETE command.
    /// Format: DELETE|Serial
    /// </summary>
    private static bool ProcessDeleteCommand(string[] parts)
    {
        if (parts.Length < 2)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[DELETE: Missing serial]");
            return false;
        }

        if (!int.TryParse(parts[1], out int serialValue))
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[DELETE: Invalid serial format: {parts[1]}]");
            return false;
        }

        Item item = World.FindItem((Serial)(uint)serialValue);

        if (item == null)
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[DELETE: Item not found: {serialValue}]");
            return false;
        }

        try
        {
            item.Delete();
            UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[DELETE: Item({serialValue})]");
            return true;
        }
        catch (Exception ex)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[DELETE: Error deleting {serialValue}: {ex.Message}]");
            return false;
        }
    }

    /// <summary>
    /// Processes an ADD command by creating a new MUO Spawner at the specified location.
    /// Format: ADD|mapIndex|x|y|z|homeRange|minDelayMinutes|maxDelayMinutes[|entry1|entry2|...]
    /// mapIndex: 0=Felucca, 1=Trammel, 2=Ilshenar, 3=Malas, 4=Tokuno, 5=TerMur
    /// Delays are in minutes (decimals accepted, e.g. 0.5 for 30 seconds).
    /// </summary>
    private static bool ProcessAddCommand(string[] parts)
    {
        if (parts.Length < 8)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[ADD: Expected: ADD|mapIndex|x|y|z|homeRange|minDelay|maxDelay[|entry...]]");
            return false;
        }

        if (!int.TryParse(parts[1], out int mapIndex) ||
            !int.TryParse(parts[2], out int x) ||
            !int.TryParse(parts[3], out int y) ||
            !int.TryParse(parts[4], out int z) ||
            !int.TryParse(parts[5], out int homeRange) ||
            !double.TryParse(parts[6], out double minMinutes) ||
            !double.TryParse(parts[7], out double maxMinutes))
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[ADD: Invalid parameters: {string.Join("|", parts)}]");
            return false;
        }

        if (mapIndex < 0 || mapIndex >= Map.Maps.Length)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[ADD: Invalid map index: {mapIndex}]");
            return false;
        }

        Map map = Map.Maps[mapIndex];

        if (map == null || map == Map.Internal)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[ADD: Map {mapIndex} is not a valid world map]");
            return false;
        }

        try
        {
            var spawner = new Spawner();
            spawner.MoveToWorld(new Point3D(x, y, z), map);
            spawner.HomeRange = homeRange;
            spawner.MinDelay = TimeSpan.FromMinutes(minMinutes);
            spawner.MaxDelay = TimeSpan.FromMinutes(maxMinutes);

            for (int i = 8; i < parts.Length; i++)
            {
                string entry = parts[i].Trim();
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    spawner.AddEntry(entry, 100, 1, false);
                }
            }

            UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[ADD: Spawner({spawner.Serial}) at {map} {x},{y},{z} entries={spawner.Entries.Count}]");
            return true;
        }
        catch (Exception ex)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[ADD: Error: {ex.Message}]");
            return false;
        }
    }

    /// <summary>
    /// Processes an EDIT command by updating an existing MUO Spawner's basic properties.
    /// Format: EDIT|serial|homeRange|minDelayMinutes|maxDelayMinutes
    /// Delays are in minutes (decimals accepted, e.g. 0.5 for 30 seconds).
    /// Note: entry-level editing is handled through MUO's own SpawnerGump via [SpawnAdmin].
    /// When MUO ships its XmlSpawner this will also target those via BaseSpawner.
    /// </summary>
    private static bool ProcessEditCommand(string[] parts)
    {
        if (parts.Length < 5)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, "XMLCMD-[EDIT: Expected: EDIT|serial|homeRange|minDelay|maxDelay]");
            return false;
        }

        if (!int.TryParse(parts[1], out int serialValue) ||
            !int.TryParse(parts[2], out int homeRange) ||
            !double.TryParse(parts[3], out double minMinutes) ||
            !double.TryParse(parts[4], out double maxMinutes))
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[EDIT: Invalid parameters: {string.Join("|", parts)}]");
            return false;
        }

        Item item = World.FindItem((Serial)(uint)serialValue);

        if (item is not BaseSpawner spawner)
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"XMLCMD-[EDIT: Spawner not found: {serialValue}]");
            return false;
        }

        try
        {
            spawner.HomeRange = homeRange;
            spawner.MinDelay = TimeSpan.FromMinutes(minMinutes);
            spawner.MaxDelay = TimeSpan.FromMinutes(maxMinutes);

            UOR_Utility.SendMsg(ConsoleColor.Cyan, $"XMLCMD-[EDIT: Spawner({serialValue}) homeRange={homeRange} min={minMinutes}m max={maxMinutes}m]");
            return true;
        }
        catch (Exception ex)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"XMLCMD-[EDIT: Error updating {serialValue}: {ex.Message}]");
            return false;
        }
    }
}
