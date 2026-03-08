using System;
using System.IO;
using System.Collections.Generic;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Models;

namespace Server.Custom.UORespawnServer.Managers;
/// <summary>
/// Manages command files for synchronization between server and editor.
/// Commands are written to files and consumed (deleted) after processing.
/// </summary>
internal static class CommandManager
{

    /// <summary>
    /// Gets the edit file path for a given command target.
    /// </summary>
    internal static string GetEditFilePath(CommandTarget target)
    {
        return target switch
        {
            CommandTarget.Settings => UOR_DIR.SETTINGS_EDIT_FILE,
            CommandTarget.Box => UOR_DIR.BOX_EDIT_FILE,
            CommandTarget.Region => UOR_DIR.REGION_EDIT_FILE,
            CommandTarget.Tile => UOR_DIR.TILE_EDIT_FILE,
            CommandTarget.Vendor => UOR_DIR.VENDOR_EDIT_FILE,
            _ => null,
        };
    }

    /// <summary>
    /// Appends a single command to the appropriate edit file.
    /// </summary>
    internal static bool WriteCommand(EditCommand command)
    {
        if (command == null || command.Target == CommandTarget.None)
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, "COMMAND-[Invalid command, cannot write]");
            return false;
        }

        string filePath = GetEditFilePath(command.Target);
        if (string.IsNullOrEmpty(filePath))
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"COMMAND-[Unknown target: {command.Target}]");
            return false;
        }

        try
        {
            // Append command line to file (creates if doesn't exist)
            using (var writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine(command.ToCommandString());
            }

            UOR_Utility.SendMsg(ConsoleColor.Green, $"COMMAND-[Logged: {command.Action} {command.Target}]");
            return true;
        }
        catch (Exception ex)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"COMMAND WRITE ERROR-[{ex.Message}]");
            return false;
        }
    }

    /// <summary>
    /// Writes a settings change command.
    /// </summary>
    internal static bool WriteSettingsCommand(string settingKey, string settingValue)
    {
        var command = EditCommand.CreateSettingsCommand(settingKey, settingValue);
        return WriteCommand(command);
    }

    /// <summary>
    /// Writes a spawn add/remove command.
    /// </summary>
    internal static bool WriteSpawnCommand(CommandAction action, CommandTarget target, SpawnSection section, SpawnTrigger trigger, string spawnName)
    {
        var command = EditCommand.CreateSpawnCommand(action, target, section, trigger, spawnName);
        return WriteCommand(command);
    }

    /// <summary>
    /// Writes a vendor edit command.
    /// Format: SET|VENDOR|LocationKey|VendorList
    /// </summary>
    internal static bool WriteVendorCommand(string locationKey, string vendorList)
    {
        var command = EditCommand.CreateVendorCommand(locationKey, vendorList);
        return WriteCommand(command);
    }



    /// <summary>
    /// Checks if there are any pending commands for the given target.
    /// </summary>
    internal static bool HasPendingCommands(CommandTarget target)
    {
        string filePath = GetEditFilePath(target);
        return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
    }

    /// <summary>
    /// Checks if there are any pending commands across all targets.
    /// </summary>
    internal static bool HasAnyPendingCommands()
    {
        return HasPendingCommands(CommandTarget.Settings)
            || HasPendingCommands(CommandTarget.Box)
            || HasPendingCommands(CommandTarget.Region)
            || HasPendingCommands(CommandTarget.Tile)
            || HasPendingCommands(CommandTarget.Vendor);
    }

    /// <summary>
    /// Reads all commands from an edit file without deleting it.
    /// </summary>
    internal static List<EditCommand> ReadCommands(CommandTarget target)
    {
        var commands = new List<EditCommand>();
        string filePath = GetEditFilePath(target);

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return commands;
        }

        try
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                var command = EditCommand.FromCommandString(line);
                if (command != null)
                {
                    commands.Add(command);
                }
                else if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#'))
                {
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, $"COMMAND-[Invalid format: {line}]");
                }
            }

            UOR_Utility.SendMsg(ConsoleColor.Green, $"COMMAND-[Read {commands.Count} commands for {target}]");
        }
        catch (Exception ex)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"COMMAND READ ERROR-[{ex.Message}]");
        }

        return commands;
    }

    /// <summary>
    /// Reads all pending commands across all targets.
    /// </summary>
    internal static Dictionary<CommandTarget, List<EditCommand>> ReadAllCommands()
    {
        var allCommands = new Dictionary<CommandTarget, List<EditCommand>>();

        CommandTarget[] targets = [CommandTarget.Settings, CommandTarget.Box, CommandTarget.Region, CommandTarget.Tile, CommandTarget.Vendor];

        foreach (var target in targets)
        {
            if (HasPendingCommands(target))
            {
                allCommands[target] = ReadCommands(target);
            }
        }

        return allCommands;
    }



    /// <summary>
    /// Deletes the edit file for the given target after processing.
    /// </summary>
    internal static bool ConsumeCommands(CommandTarget target)
    {
        string filePath = GetEditFilePath(target);

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return true; // Nothing to consume
        }

        try
        {
            File.Delete(filePath);
            UOR_Utility.SendMsg(ConsoleColor.Green, $"COMMAND-[Consumed {target} edits]");
            return true;
        }
        catch (Exception ex)
        {
            UOR_Utility.SendMsg(ConsoleColor.Red, $"COMMAND CONSUME ERROR-[{ex.Message}]");
            return false;
        }
    }

    /// <summary>
    /// Reads, processes, and consumes all commands for a target.
    /// Returns the list of commands that were processed.
    /// </summary>
    internal static List<EditCommand> ProcessAndConsumeCommands(CommandTarget target)
    {
        var commands = ReadCommands(target);

        if (commands.Count > 0)
        {
            // Commands will be applied by the caller
            // After successful application, consume the file
            ConsumeCommands(target);
        }

        return commands;
    }



    /// <summary>
    /// Validates a command before applying.
    /// Returns true if command is valid, false otherwise.
    /// </summary>
    internal static bool ValidateCommand(EditCommand command)
    {
        if (command == null)
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, "COMMAND VALIDATION-[Null command]");
            return false;
        }

        if (command.Action == CommandAction.None)
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, "COMMAND VALIDATION-[No action specified]");
            return false;
        }

        if (command.Target == CommandTarget.None)
        {
            UOR_Utility.SendMsg(ConsoleColor.Yellow, "COMMAND VALIDATION-[No target specified]");
            return false;
        }

        // Settings commands need a key (stored in SpawnName)
        if (command.Target == CommandTarget.Settings)
        {
            if (string.IsNullOrWhiteSpace(command.SpawnName))
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, "COMMAND VALIDATION-[Settings command missing key]");
                return false;
            }
        }
        else
        {
            // Spawn commands need a section for add/remove
            if (command.Action == CommandAction.Add || command.Action == CommandAction.Remove)
            {
                if (command.Section == SpawnSection.None)
                {
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, "COMMAND VALIDATION-[Spawn command missing section]");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(command.SpawnName))
                {
                    UOR_Utility.SendMsg(ConsoleColor.Yellow, "COMMAND VALIDATION-[Spawn command missing spawn name]");
                    return false;
                }
            }
        }

        return true;
    }

}
