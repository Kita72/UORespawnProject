using System;
using System.Collections.Generic;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Entities;
using Server.Custom.UORespawnServer.Gumps;
using Server.Custom.UORespawnServer.Managers;

namespace Server.Custom.UORespawnServer.Services
{
    /// <summary>
    /// Service for VendorEditGump - handles all business logic for editing vendor spawn lists.
    /// Follows SRP: Gump handles UI only, this handles all logic.
    /// Vendors are separate from ISpawnEntity - they have a simple single VendorList.
    /// </summary>
    internal class VendorEditService
    {
        private readonly PlayerMobile _Player;
        private readonly VendorEntity _Entity;
        private readonly Map _Map;
        private readonly string _LocationName;

        // Current UI state
        internal int ScrollOffset { get; private set; }

        // Working copy of vendor list (edited in memory, written on save)
        private List<string> _WorkingVendorList;

        // Track if changes were made
        private bool _HasChanges;

        /// <summary>
        /// Creates a new VendorEditService for editing a vendor entity.
        /// </summary>
        /// <param name="player">The player editing the vendor</param>
        /// <param name="entity">The VendorEntity to edit</param>
        /// <param name="map">The map the vendor is on</param>
        /// <param name="locationName">Display name for the location (Sign type or "Beehive")</param>
        internal VendorEditService(PlayerMobile player, VendorEntity entity, Map map, string locationName)
        {
            _Player = player;
            _Entity = entity;
            _Map = map;
            _LocationName = locationName;

            ScrollOffset = 0;
            _HasChanges = false;

            // Create working copy of vendor list
            InitializeWorkingCopy();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDOR EDIT-[{player.Name} editing: {locationName}]");
        }

        private void InitializeWorkingCopy()
        {
            _WorkingVendorList = new List<string>();

            if (_Entity.VendorList != null)
            {
                foreach (string vendor in _Entity.VendorList)
                {
                    _WorkingVendorList.Add(vendor);
                }
            }
        }

        #region Gump Management

        internal void OpenGump()
        {
            _Player.SendGump(new VendorEditGump(_Player, this));
        }

        internal void RefreshGump()
        {
            OpenGump();
        }

        internal void CancelEdit()
        {
            _Player.SendMessage(0x35, "Vendor editing cancelled - no changes saved.");

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDOR EDIT-[{_Player.Name} cancelled editing: {_LocationName}]");
        }

        #endregion

        #region Display Helpers

        /// <summary>
        /// Gets the location info for display in the gump title.
        /// </summary>
        internal string GetLocationInfo()
        {
            return _LocationName;
        }

        /// <summary>
        /// Gets whether this is a sign or beehive location.
        /// </summary>
        internal bool IsSign => _Entity.IsSign;

        /// <summary>
        /// Gets the vendor list for display.
        /// </summary>
        internal List<string> GetVendorList()
        {
            return _WorkingVendorList;
        }

        /// <summary>
        /// Gets the vendor count for display.
        /// </summary>
        internal int GetVendorCount()
        {
            return _WorkingVendorList.Count;
        }

        #endregion

        #region Scrolling

        /// <summary>
        /// Scrolls the vendor list up or down.
        /// </summary>
        internal void Scroll(int direction)
        {
            int maxOffset = Math.Max(0, _WorkingVendorList.Count - 10);

            ScrollOffset += direction;
            ScrollOffset = Math.Max(0, Math.Min(maxOffset, ScrollOffset));
        }

        #endregion

        #region Vendor List Operations

        /// <summary>
        /// Adds a vendor name to the list.
        /// </summary>
        internal void AddVendor(string vendorName)
        {
            if (string.IsNullOrWhiteSpace(vendorName))
                return;

            // Check for duplicates (case-insensitive)
            foreach (string existing in _WorkingVendorList)
            {
                if (existing.Equals(vendorName, StringComparison.OrdinalIgnoreCase))
                {
                    _Player.SendMessage(0x22, $"'{vendorName}' already exists in this list.");
                    return;
                }
            }

            _WorkingVendorList.Add(vendorName);
            _HasChanges = true;

            _Player.SendMessage(0x40, $"Added '{vendorName}' to vendor list.");

            // Scroll to show the new entry
            int maxOffset = Math.Max(0, _WorkingVendorList.Count - 10);
            ScrollOffset = maxOffset;
        }

        /// <summary>
        /// Removes a vendor name from the list by index.
        /// </summary>
        internal void RemoveVendor(int index)
        {
            if (index < 0 || index >= _WorkingVendorList.Count)
                return;

            string removedName = _WorkingVendorList[index];
            _WorkingVendorList.RemoveAt(index);
            _HasChanges = true;

            _Player.SendMessage(0x20, $"Removed '{removedName}' from vendor list.");

            // Adjust scroll if necessary
            int maxOffset = Math.Max(0, _WorkingVendorList.Count - 10);
            ScrollOffset = Math.Min(ScrollOffset, maxOffset);
        }

        #endregion

        #region Save Changes

        /// <summary>
        /// Saves all changes by generating commands for the CommandManager
        /// and immediately respawning vendors at this location using ISpawner.
        /// </summary>
        internal void SaveChanges()
        {
            if (!_HasChanges)
            {
                _Player.SendMessage(0x35, "No changes to save.");
                return;
            }

            int commandsWritten = 0;

            // Build the vendor list as comma-separated string
            string vendorListStr = string.Join(",", _WorkingVendorList);

            // Write a single command for the entire vendor list
            // Format: VENDOR|MapId|X|Y|Z|IsSign|VendorList
            string locationKey = $"{_Map.MapID}|{_Entity.Location.X}|{_Entity.Location.Y}|{_Entity.Location.Z}|{_Entity.IsSign}";

            if (CommandManager.WriteVendorCommand(locationKey, vendorListStr))
            {
                commandsWritten++;
            }

            // Apply changes to in-memory entity
            ApplyChangesToEntity();

            // Immediately respawn vendors at this location using ISpawner
            int spawned = UOR_Core.RespawnVendorsAtLocation(_Map, _Entity);

            if (commandsWritten > 0)
            {
                _Player.SendMessage(0x40, $"Saved vendor changes - {spawned} vendors respawned. Sync with editor to persist.");

                UOR_Utility.SendMsg(ConsoleColor.Green, $"VENDOR EDIT-[{_Player.Name} saved {commandsWritten} command, {spawned} respawned for: {_LocationName}]");
            }
            else
            {
                _Player.SendMessage(0x22, "Failed to save vendor changes.");
            }

            _HasChanges = false;
        }

        /// <summary>
        /// Applies working copy changes back to the VendorEntity.
        /// </summary>
        private void ApplyChangesToEntity()
        {
            // Clear existing vendor list and repopulate from working copy
            _Entity.VendorList.Clear();
            foreach (string vendor in _WorkingVendorList)
            {
                _Entity.AddVendor(vendor);
            }
        }

        #endregion
    }
}
