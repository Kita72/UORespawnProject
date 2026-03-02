using System;
using System.Collections;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Enums;
using Server.Custom.UORespawnServer.Gumps;
using Server.Custom.UORespawnServer.Interfaces;
using Server.Custom.UORespawnServer.Managers;
using Server.Custom.UORespawnServer.Models;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Service for SpawnEditGump - handles all business logic for editing spawn lists.
    /// Follows SRP: Gump handles UI only, this handles all logic.
    /// </summary>
    internal class SpawnEditService
    {
        private readonly PlayerMobile _Player;
        private readonly ISpawnEntity _Entity;
        private readonly CommandTarget _TargetType;
        private readonly string _EntityName;

        // Current UI state
        internal SpawnSection CurrentSection { get; private set; }
        internal int ScrollOffset { get; private set; }

        // Working copies of spawn lists (edited in memory, written on save)
        private ArrayList _WorkingWaterList;
        private ArrayList _WorkingWeatherList;
        private ArrayList _WorkingTimedList;
        private ArrayList _WorkingCommonList;
        private ArrayList _WorkingUncommonList;
        private ArrayList _WorkingRareList;

        // Working copies of triggers
        private WeatherTypes _WorkingWeatherType;
        private TimeTypes _WorkingTimedType;

        // Track if changes were made
        private bool _HasChanges;

        // Static arrays in DECLARATION ORDER for index-to-enum conversion
        // (Binary files store indices, not the actual enum values)
        private static readonly TimeTypes[] TimeTypesInOrder =
        {
            TimeTypes.Witching_Hour,     // index 0
            TimeTypes.Middle_of_Night,   // index 1
            TimeTypes.Early_Morning,     // index 2
            TimeTypes.Late_Morning,      // index 3
            TimeTypes.Noon,              // index 4
            TimeTypes.Afternoon,         // index 5
            TimeTypes.Early_Evening,     // index 6
            TimeTypes.Late_at_Night      // index 7
        };

        /// <summary>
        /// Creates a new SpawnEditService for editing a spawn entity.
        /// </summary>
        /// <param name="player">The player editing the spawn</param>
        /// <param name="entity">The spawn entity to edit (BoxEntity, RegionEntity, or TileEntity)</param>
        /// <param name="targetType">The type of target (Box, Region, or Tile)</param>
        /// <param name="entityName">Display name for the entity</param>
        internal SpawnEditService(PlayerMobile player, ISpawnEntity entity, CommandTarget targetType, string entityName)
        {
            _Player = player;
            _Entity = entity;
            _TargetType = targetType;
            _EntityName = entityName;

            CurrentSection = SpawnSection.Common;
            ScrollOffset = 0;
            _HasChanges = false;

            // Calculate gump position offset based on target type (so multiple gumps don't stack)
            _GumpOffsetX = GetGumpOffset();

            // Create working copies of spawn lists
            InitializeWorkingCopies();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"SPAWN EDIT-[{player.Name} editing {targetType}: {entityName}]");
        }

        // Gump position offset to prevent stacking
        private readonly int _GumpOffsetX;

        private int GetGumpOffset()
        {
            // Offset each gump type so they appear side by side
            switch (_TargetType)
            {
                case CommandTarget.Box: return 0;
                case CommandTarget.Region: return 340;
                case CommandTarget.Tile: return 680;
                default: return 0;
            }
        }

        private void InitializeWorkingCopies()
        {
            _WorkingWaterList = CloneList(_Entity.WaterList);
            _WorkingWeatherList = CloneList(_Entity.WeatherList);
            _WorkingTimedList = CloneList(_Entity.TimedList);
            _WorkingCommonList = CloneList(_Entity.CommonList);
            _WorkingUncommonList = CloneList(_Entity.UnCommonList);
            _WorkingRareList = CloneList(_Entity.RareList);

            _WorkingWeatherType = _Entity.WeatherType;

            // TimeTypes is stored as 1-based (1-8) in binary, convert to proper enum
            int rawTimed = (int)_Entity.TimedType;
            if (rawTimed >= 1 && rawTimed <= 8)
            {
                _WorkingTimedType = TimeTypesInOrder[rawTimed - 1];
            }
            else
            {
                _WorkingTimedType = TimeTypesInOrder[0]; // Default to first
            }
        }

        private ArrayList CloneList(ArrayList source)
        {
            if (source == null)
                return new ArrayList();

            return new ArrayList(source);
        }

        #region Gump Management

        internal void OpenGump()
        {
            _Player.SendGump(new SpawnEditGump(_Player, this, _GumpOffsetX));
        }

        internal void RefreshGump()
        {
            // The gump that triggered OnResponse is already closed by the server
            // Just open a new gump - don't close others (we want multiple editors open)
            OpenGump();
        }

        internal void CancelEdit()
        {
            _Player.SendMessage(0x35, "Spawn editing cancelled - no changes saved.");

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"SPAWN EDIT-[{_Player.Name} cancelled editing {_TargetType}: {_EntityName}]");
        }

        #endregion

        #region Display Helpers

        /// <summary>
        /// Gets the target info for display in the gump title.
        /// </summary>
        internal string GetTargetInfo()
        {
            return $"{_TargetType}: {_EntityName}";
        }

        /// <summary>
        /// Gets the current trigger text for display (Weather/Timed sections only).
        /// </summary>
        internal string GetCurrentTriggerText()
        {
            if (CurrentSection == SpawnSection.Weather)
            {
                return _WorkingWeatherType.ToString().Replace("_", " ");
            }
            else if (CurrentSection == SpawnSection.Timed)
            {
                return _WorkingTimedType.ToString().Replace("_", " ");
            }

            return "N/A";
        }

        /// <summary>
        /// Gets the current spawn list based on selected section.
        /// </summary>
        internal ArrayList GetCurrentSpawnList()
        {
            switch (CurrentSection)
            {
                case SpawnSection.Water: return _WorkingWaterList;
                case SpawnSection.Weather: return _WorkingWeatherList;
                case SpawnSection.Timed: return _WorkingTimedList;
                case SpawnSection.Common: return _WorkingCommonList;
                case SpawnSection.Uncommon: return _WorkingUncommonList;
                case SpawnSection.Rare: return _WorkingRareList;
                default: return _WorkingCommonList;
            }
        }

        #endregion

        #region Section Navigation

        /// <summary>
        /// Sets the current section and resets scroll position.
        /// </summary>
        internal void SetSection(SpawnSection section)
        {
            if (CurrentSection != section)
            {
                CurrentSection = section;
                ScrollOffset = 0;
            }
        }

        /// <summary>
        /// Scrolls the spawn list up or down.
        /// </summary>
        internal void Scroll(int direction)
        {
            ArrayList list = GetCurrentSpawnList();
            int maxOffset = Math.Max(0, (list?.Count ?? 0) - 10);

            ScrollOffset += direction;
            ScrollOffset = Math.Max(0, Math.Min(maxOffset, ScrollOffset));
        }

        #endregion

        #region Trigger Management

        /// <summary>
        /// Cycles the trigger for Weather or Timed sections.
        /// Weather trigger cannot be None - it must have a valid type.
        /// </summary>
        internal void CycleTrigger(int direction)
        {
            if (CurrentSection == SpawnSection.Weather)
            {
                CycleWeatherTrigger(direction);
                _HasChanges = true;
            }
            else if (CurrentSection == SpawnSection.Timed)
            {
                CycleTimedTrigger(direction);
                _HasChanges = true;
            }
        }

        private void CycleWeatherTrigger(int direction)
        {
            // Weather trigger cannot be None - cycle through valid types only
            WeatherTypes[] validTypes = { WeatherTypes.Rain, WeatherTypes.Snow, WeatherTypes.Storm, WeatherTypes.Blizzard };

            int currentIndex = Array.IndexOf(validTypes, _WorkingWeatherType);

            if (currentIndex < 0)
                currentIndex = 0;

            currentIndex += direction;

            // Wrap around
            if (currentIndex < 0)
                currentIndex = validTypes.Length - 1;
            else if (currentIndex >= validTypes.Length)
                currentIndex = 0;

            _WorkingWeatherType = validTypes[currentIndex];
        }

        private void CycleTimedTrigger(int direction)
        {
            // Find current position in array
            int currentIndex = Array.IndexOf(TimeTypesInOrder, _WorkingTimedType);

            if (currentIndex < 0)
                currentIndex = 0;

            currentIndex += direction;

            // Wrap around
            if (currentIndex < 0)
                currentIndex = TimeTypesInOrder.Length - 1;
            else if (currentIndex >= TimeTypesInOrder.Length)
                currentIndex = 0;

            _WorkingTimedType = TimeTypesInOrder[currentIndex];
        }

        #endregion

        #region Spawn List Operations

        /// <summary>
        /// Adds a spawn name to the current section's list.
        /// </summary>
        internal void AddSpawn(string spawnName)
        {
            if (string.IsNullOrWhiteSpace(spawnName))
                return;

            ArrayList list = GetCurrentSpawnList();

            // Check for duplicates (case-insensitive)
            foreach (string existing in list)
            {
                if (existing.Equals(spawnName, StringComparison.OrdinalIgnoreCase))
                {
                    _Player.SendMessage(0x22, $"'{spawnName}' already exists in this list.");
                    return;
                }
            }

            list.Add(spawnName);
            _HasChanges = true;

            _Player.SendMessage(0x40, $"Added '{spawnName}' to {CurrentSection} list.");

            // Scroll to show the new entry
            int maxOffset = Math.Max(0, list.Count - 10);
            ScrollOffset = maxOffset;
        }

        /// <summary>
        /// Removes a spawn name from the current section's list by index.
        /// </summary>
        internal void RemoveSpawn(int index)
        {
            ArrayList list = GetCurrentSpawnList();

            if (index < 0 || index >= list.Count)
                return;

            string removedName = list[index] as string;
            list.RemoveAt(index);
            _HasChanges = true;

            _Player.SendMessage(0x20, $"Removed '{removedName}' from {CurrentSection} list.");

            // Adjust scroll if necessary
            int maxOffset = Math.Max(0, list.Count - 10);
            ScrollOffset = Math.Min(ScrollOffset, maxOffset);
        }

        #endregion

        #region Save Changes

        /// <summary>
        /// Saves all changes by generating commands for the CommandManager.
        /// </summary>
        internal void SaveChanges()
        {
            if (!_HasChanges)
            {
                _Player.SendMessage(0x35, "No changes to save.");
                return;
            }

            int commandCount = 0;

            // Generate commands for each section that has differences
            commandCount += GenerateListCommands(SpawnSection.Water, _Entity.WaterList, _WorkingWaterList);
            commandCount += GenerateListCommands(SpawnSection.Weather, _Entity.WeatherList, _WorkingWeatherList);
            commandCount += GenerateListCommands(SpawnSection.Timed, _Entity.TimedList, _WorkingTimedList);
            commandCount += GenerateListCommands(SpawnSection.Common, _Entity.CommonList, _WorkingCommonList);
            commandCount += GenerateListCommands(SpawnSection.Uncommon, _Entity.UnCommonList, _WorkingUncommonList);
            commandCount += GenerateListCommands(SpawnSection.Rare, _Entity.RareList, _WorkingRareList);

            // Generate trigger update commands if changed
            if (_WorkingWeatherType != _Entity.WeatherType)
            {
                var cmd = new EditCommand(
                    CommandAction.Update,
                    _TargetType,
                    SpawnSection.Weather,
                    SpawnTrigger.Weather,
                    _EntityName,
                    _WorkingWeatherType.ToString()
                );
                CommandManager.WriteCommand(cmd);
                commandCount++;
            }

            if (_WorkingTimedType != _Entity.TimedType)
            {
                var cmd = new EditCommand(
                    CommandAction.Update,
                    _TargetType,
                    SpawnSection.Timed,
                    SpawnTrigger.Timed,
                    _EntityName,
                    _WorkingTimedType.ToString()
                );
                CommandManager.WriteCommand(cmd);
                commandCount++;
            }

            // Apply changes to the in-memory entity
            ApplyChangesToEntity();

            _Player.SendMessage(0x40, $"Saved {commandCount} spawn changes to {_TargetType}: {_EntityName}.");

            UOR_Utility.SendMsg(ConsoleColor.Cyan, $"SPAWN EDIT-[{_Player.Name} saved {commandCount} commands to {_TargetType}: {_EntityName}]");

            _HasChanges = false;
        }

        private int GenerateListCommands(SpawnSection section, ArrayList original, ArrayList working)
        {
            int count = 0;
            ArrayList originalList = original ?? new ArrayList();
            ArrayList workingList = working ?? new ArrayList();

            // Find removed items (in original but not in working)
            foreach (string item in originalList)
            {
                bool found = false;
                foreach (string workingItem in workingList)
                {
                    if (workingItem.Equals(item, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var cmd = EditCommand.CreateSpawnCommand(
                        CommandAction.Remove,
                        _TargetType,
                        section,
                        GetTriggerForSection(section),
                        item
                    );
                    cmd.ExtraData = _EntityName; // Store entity identifier
                    CommandManager.WriteCommand(cmd);
                    count++;
                }
            }

            // Find added items (in working but not in original)
            foreach (string item in workingList)
            {
                bool found = false;
                foreach (string originalItem in originalList)
                {
                    if (originalItem.Equals(item, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var cmd = EditCommand.CreateSpawnCommand(
                        CommandAction.Add,
                        _TargetType,
                        section,
                        GetTriggerForSection(section),
                        item
                    );
                    cmd.ExtraData = _EntityName; // Store entity identifier
                    CommandManager.WriteCommand(cmd);
                    count++;
                }
            }

            return count;
        }

        private SpawnTrigger GetTriggerForSection(SpawnSection section)
        {
            switch (section)
            {
                case SpawnSection.Weather: return SpawnTrigger.Weather;
                case SpawnSection.Timed: return SpawnTrigger.Timed;
                default: return SpawnTrigger.None;
            }
        }

        private void ApplyChangesToEntity()
        {
            // Apply working copies back to the entity (in-memory update)
            _Entity.WaterList = _WorkingWaterList;
            _Entity.WeatherList = _WorkingWeatherList;
            _Entity.TimedList = _WorkingTimedList;
            _Entity.CommonList = _WorkingCommonList;
            _Entity.UnCommonList = _WorkingUncommonList;
            _Entity.RareList = _WorkingRareList;

            _Entity.WeatherType = _WorkingWeatherType;
            _Entity.TimedType = _WorkingTimedType;
        }

        #endregion
    }
}
