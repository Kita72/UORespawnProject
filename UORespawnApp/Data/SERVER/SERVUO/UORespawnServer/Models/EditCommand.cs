using Server.Custom.UORespawnServer.Enums;

namespace Server.Custom.UORespawnServer.Models
{
    /// <summary>
    /// Represents an edit command for synchronization between server and editor.
    /// Format: Action|Target|Section|Trigger|SpawnName|ExtraData
    /// </summary>
    internal class EditCommand
    {
        internal CommandAction Action { get; set; }
        internal CommandTarget Target { get; set; }
        internal SpawnSection Section { get; set; }
        internal SpawnTrigger Trigger { get; set; }
        internal string SpawnName { get; set; }
        internal string ExtraData { get; set; }

        // For settings commands: Key=Section (as string), Value=ExtraData
        internal string SettingKey => Section.ToString();
        internal string SettingValue => ExtraData;

        internal EditCommand()
        {
            Action = CommandAction.None;
            Target = CommandTarget.None;
            Section = SpawnSection.None;
            Trigger = SpawnTrigger.None;
            SpawnName = string.Empty;
            ExtraData = string.Empty;
        }

        internal EditCommand(CommandAction action, CommandTarget target, SpawnSection section, SpawnTrigger trigger, string spawnName, string extraData = "")
        {
            Action = action;
            Target = target;
            Section = section;
            Trigger = trigger;
            SpawnName = spawnName ?? string.Empty;
            ExtraData = extraData ?? string.Empty;
        }

        /// <summary>
        /// Creates a settings update command.
        /// </summary>
        internal static EditCommand CreateSettingsCommand(string settingKey, string settingValue)
        {
            return new EditCommand
            {
                Action = CommandAction.Update,
                Target = CommandTarget.Settings,
                Section = SpawnSection.None,
                Trigger = SpawnTrigger.None,
                SpawnName = settingKey,
                ExtraData = settingValue
            };
        }

        /// <summary>
        /// Creates a spawn add/remove command.
        /// </summary>
        internal static EditCommand CreateSpawnCommand(CommandAction action, CommandTarget target, SpawnSection section, SpawnTrigger trigger, string spawnName)
        {
            return new EditCommand(action, target, section, trigger, spawnName);
        }

        /// <summary>
        /// Creates a vendor edit command.
        /// LocationKey format: MapId|X|Y|Z|IsSign
        /// VendorList: comma-separated vendor names
        /// </summary>
        internal static EditCommand CreateVendorCommand(string locationKey, string vendorList)
        {
            return new EditCommand
            {
                Action = CommandAction.Update,
                Target = CommandTarget.Vendor,
                Section = SpawnSection.None,
                Trigger = SpawnTrigger.None,
                SpawnName = locationKey,
                ExtraData = vendorList
            };
        }

        /// <summary>
        /// Serializes the command to a pipe-delimited string.
        /// </summary>
        internal string ToCommandString()
        {
            return $"{Action}|{Target}|{Section}|{Trigger}|{SpawnName}|{ExtraData}";
        }

        /// <summary>
        /// Parses a pipe-delimited command string into an EditCommand.
        /// Returns null if parsing fails.
        /// </summary>
        internal static EditCommand FromCommandString(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return null;

            // Skip comments and empty lines
            string trimmed = commandLine.Trim();
            if (trimmed.StartsWith("#") || trimmed.Length == 0)
                return null;

            string[] parts = trimmed.Split('|');
            if (parts.Length < 5)
                return null;

            var command = new EditCommand();

            // Parse Action
            if (!System.Enum.TryParse(parts[0], true, out CommandAction action))
                return null;
            command.Action = action;

            // Parse Target
            if (!System.Enum.TryParse(parts[1], true, out CommandTarget target))
                return null;
            command.Target = target;

            // Parse Section
            if (!System.Enum.TryParse(parts[2], true, out SpawnSection section))
                section = SpawnSection.None;
            command.Section = section;

            // Parse Trigger
            if (!System.Enum.TryParse(parts[3], true, out SpawnTrigger trigger))
                trigger = SpawnTrigger.None;
            command.Trigger = trigger;

            // SpawnName
            command.SpawnName = parts[4];

            // ExtraData (optional)
            command.ExtraData = parts.Length > 5 ? parts[5] : string.Empty;

            return command;
        }

        public override string ToString()
        {
            return ToCommandString();
        }
    }
}
