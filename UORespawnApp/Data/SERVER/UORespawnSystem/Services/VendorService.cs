using System;
using System.IO;
using System.Collections.Generic;

using Server.Mobiles;

using Server.Custom.UORespawnServer.Mobiles;
using Server.Custom.UORespawnServer.Spawners;
using Server.Custom.UORespawnServer.Managers;

namespace Server.Custom.UORespawnServer.Services
{
    internal class VendorService
    {
        private readonly List<string> _Vendors;

        internal VendorService()
        {
            _Vendors = new List<string>();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[Created]");

            if (UOR_Settings.ENABLE_VENDOR_SPAWN)
            {
                if (!Load())
                {
                    InitializeSpawn();
                }
            }
            else
            {
                if (Load())
                {
                    DeleteVendors();
                }
            }
        }

        internal void ResetVendors()
        {
            DeleteVendors();

            if (UOR_Settings.ENABLE_VENDOR_SPAWN)
            {
                InitializeSpawn();
            }
            else
            {
                UOR_Settings.ENABLE_VENDOR_NIGHT = false;
                UOR_Settings.ENABLE_VENDOR_EXTRA = false;
            }
        }

        private void InitializeSpawn()
        {
            var vendorSpawn = SpawnManager.VendorSpawns;

            int spawnedCount = 0;

            if (vendorSpawn != null && vendorSpawn.Count > 0)
            {
                foreach (var vendor in vendorSpawn)
                {
                    foreach (var entity in vendor.Value)
                    {
                        VendorSpawner.TryToSpawn(vendor.Key, entity, this);

                        spawnedCount++;
                    }
                }
            }

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{_Vendors.Count} Initialized]");
        }

        internal void DeleteVendors()
        {
            if (_Vendors.Count == 0) return;

            List<Serial> list = new List<Serial>();

            int deleted = 0;

            foreach (var mobile in World.Mobiles.Values)
            {
                if (mobile is BaseCreature bc && bc.Home.Z == UOR_Settings.VENDOR_MARKER)
                {
                    list.Add(mobile.Serial);
                }
            }

            foreach (var serial in list)
            {
                Mobile vendor = GetVendor($"{serial.Value}");

                if (vendor != null && !vendor.Deleted)
                {
                    vendor.Delete();
                    deleted++;
                }
            }

            _Vendors.Clear();

            UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{deleted} Deleted]");
        }

        internal void AddVendor(Mobile m)
        {
            if (!_Vendors.Contains($"{m.Serial.Value}"))
            {
                _Vendors.Add($"{m.Serial.Value}");
            }
        }

        private Mobile GetVendor(string serial)
        {
            if (Int32.TryParse(serial, out var value))
            {
                return World.FindMobile(value);
            }

            return null;
        }

        private void ValidateVendors()
        {
            bool allGood = true;

            for (int i = 0; i < _Vendors.Count; i++)
            {
                if (_Vendors.Count > 0 && Int32.TryParse(_Vendors[0], out int serial))
                {
                    if (World.FindMobile(serial) == null)
                    {
                        allGood = false;
                        break; 
                    }
                }
            }

            if (!allGood)
            {
                ResetVendors();
            }
        }

        internal void UpdateTime()
        {
            ToggleWorking(UOR_Settings.ENABLE_VENDOR_NIGHT);
        }

        private void ToggleWorking(bool isEnabled)
        {
            int hidden = 0;
            for (int i = 0; i < _Vendors.Count; i++)
            {
                if (GetVendor(_Vendors[i]) is Mobile m)
                {
                    if (m != null)
                    {
                        if (m is BaseVendor bv)
                        {
                            bv.Hidden = isEnabled && UOR_Utility.IsNight(bv.Map, bv.Location);
                            bv.CantWalk = bv.Hidden;

                            if (bv.Hidden) hidden++;

                            if (!isEnabled)
                            {
                                NPCUtility.CheckNightDress(bv);
                            }
                        }
                        else
                        {
                            NPCUtility.CheckNightDress(m);
                        }
                    }
                }
            }

            if (hidden > 0)
            {
                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{hidden} Hidden]");
            }
        }

        internal void Save()
        {
            if (_Vendors.Count > 0)
            {
                ValidateVendors();

                File.WriteAllLines(UOR_DIR.VENDOR_SPAWN_FILE, _Vendors);

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{_Vendors.Count} Saved]");
            }
        }

        private bool Load()
        {
            if (File.Exists(UOR_DIR.VENDOR_SPAWN_FILE))
            {
                _Vendors.AddRange(File.ReadAllLines(UOR_DIR.VENDOR_SPAWN_FILE));

                UOR_Utility.SendMsg(ConsoleColor.Yellow, $"VENDORS-[{_Vendors.Count} Loaded]");

                return _Vendors.Count > 0;
            }

            return false;
        }
    }
}
