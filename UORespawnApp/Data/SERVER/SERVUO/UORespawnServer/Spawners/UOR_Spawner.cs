using System;
using System.Collections.Generic;

using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Spawners
{
    /// <summary>
    /// Base Spawner implementation for UORespawn system.
    /// Inherits from ServUO's Spawner class so MySpawner property works correctly.
    /// Uses singleton pattern - one invisible spawner for all UOR mobs, one for vendors.
    /// Re-implements ISpawner to override Remove behavior.
    /// 
    /// IMPORTANT: Mobile.Spawner property is NOT serialized by ServUO.
    /// We must save our own list of spawned serials and re-claim them on deserialize.
    /// </summary>
    public abstract class UOR_Spawner : Spawner, ISpawner
    {
        // Track spawned creature serials for serialization
        // Mobile.Spawner is NOT saved by ServUO, so we must track and re-claim
        private List<int> _spawnedSerials = new List<int>();
        private HashSet<int> _serialsSet = new HashSet<int>(); // shadow for O(1) Contains

        /// <summary>
        /// Display name for logging purposes.
        /// </summary>
        public abstract string SpawnerName { get; }

        /// <summary>
        /// Default home range for spawned creatures.
        /// </summary>
        public virtual int DefaultHomeRange => 50;

        /// <summary>
        /// Whether creatures should be unlinked from this spawner when tamed.
        /// True = tamed creatures lose spawner reference (recommended for mobs).
        /// </summary>
        public virtual bool UnlinkOnTaming => true;

        /// <summary>
        /// Protected constructor for singleton subclasses.
        /// Creates an invisible, intangible spawner item that exists in memory only.
        /// </summary>
        protected UOR_Spawner() : base()
        {
            // Make invisible and intangible - this spawner doesn't exist in the world
            Visible = false;
            Movable = false;

            // Move to internal map so it's not in the game world
            MoveToWorld(Point3D.Zero, Map.Internal);

            // Set default properties
            HomeRange = DefaultHomeRange;
            Running = false; // We don't use the timer-based spawning
        }

        /// <summary>
        /// Public serial constructor for deserialization.
        /// </summary>
        public UOR_Spawner(Serial serial) : base(serial)
        {
            _spawnedSerials = new List<int>();
            _serialsSet = new HashSet<int>();
        }

        /// <summary>
        /// Override to prevent normal spawner behavior - we control spawning ourselves.
        /// </summary>
        public override void OnDelete()
        {
            // Don't delete spawned creatures when spawner is removed
            // UOR system manages creature lifecycle separately
        }

        /// <summary>
        /// Override to prevent default spawner properties from showing on creatures.
        /// Only shows spawner info when debug mode is enabled.
        /// </summary>
        public override void GetSpawnProperties(ISpawnable spawn, ObjectPropertyList list)
        {
            // Only show spawner info in debug mode
            if (UOR_Settings.ENABLE_DEBUG)
            {
                list.Add($"UOR Spawner: {SpawnerName}");
            }
        }

        /// <summary>
        /// Called when spawn is removed. Override in subclasses for count tracking.
        /// </summary>
        protected virtual void OnSpawnRemoved(ISpawnable spawn)
        {
            // Remove from tracking list
            if (spawn is Mobile m)
            {
                _spawnedSerials.Remove(m.Serial.Value);
                _serialsSet.Remove(m.Serial.Value);
            }
        }

        /// <summary>
        /// Explicit interface implementation for ISpawner.Remove.
        /// Called by ServUO when a spawn should be removed from this spawner's control.
        /// </summary>
        void ISpawner.Remove(ISpawnable spawn)
        {
            // Notify subclass for count tracking
            OnSpawnRemoved(spawn);
        }

        /// <summary>
        /// Assigns this spawner to a creature, establishing ownership.
        /// Sets creature.Spawner = this (MySpawner will now return this).
        /// Also tracks the serial for serialization.
        /// </summary>
        public void Claim(BaseCreature creature, Point3D homeLocation = default)
        {
            if (creature == null || creature.Deleted)
                return;

            creature.Spawner = this;
            creature.Home = homeLocation != default ? homeLocation : creature.Location;
            creature.RangeHome = DefaultHomeRange;

            // Track for serialization — O(1) duplicate check via HashSet shadow
            if (!_serialsSet.Contains(creature.Serial.Value))
            {
                _spawnedSerials.Add(creature.Serial.Value);
                _serialsSet.Add(creature.Serial.Value);
            }
        }

        /// <summary>
        /// Releases a creature from this spawner's ownership.
        /// </summary>
        public void Release(BaseCreature creature)
        {
            if (creature == null || creature.Deleted)
                return;

            if (creature.Spawner == this)
            {
                creature.Spawner = null;
                _spawnedSerials.Remove(creature.Serial.Value);
                _serialsSet.Remove(creature.Serial.Value);
            }
        }

        /// <summary>
        /// Re-claims all tracked creatures after world load.
        /// Mobile.Spawner is not serialized, so we must restore the reference.
        /// Returns the count of reclaimed creatures.
        /// </summary>
        public int ReclaimAll()
        {
            int reclaimed = 0;
            var toRemove = new List<int>();

            foreach (int serial in _spawnedSerials)
            {
                if (World.FindMobile(serial) is BaseCreature bc && !bc.Deleted)
                {
                    bc.Spawner = this;
                    reclaimed++;
                }
                else
                {
                    toRemove.Add(serial);
                }
            }

            // Clean up invalid serials (deleted creatures)
            foreach (int serial in toRemove)
            {
                _spawnedSerials.Remove(serial);
                _serialsSet.Remove(serial);
            }

            return reclaimed;
        }

        /// <summary>
        /// Serialization - saves spawned creature serials.
        /// </summary>
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // Version 1: Save spawned creature serials
            writer.Write(_spawnedSerials.Count);
            foreach (int serial in _spawnedSerials)
            {
                writer.Write(serial);
            }
        }

        /// <summary>
        /// Deserialization - loads spawned creature serials.
        /// Reclaim is handled by TrackService after world load completes.
        /// </summary>
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            _spawnedSerials = new List<int>();

            if (version >= 1)
            {
                int count = reader.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    _spawnedSerials.Add(reader.ReadInt());
                }
            }

            _serialsSet = new HashSet<int>(_spawnedSerials);

            // NOTE: Reclaim is NOT done here
            // to ensure proper ordering: Reclaim → Cleanup → Vendor Init → Services
        }

        /// <summary>
        /// Counts creatures owned by this spawner that can swim.
        /// Iterates serial list — O(n) but avoids full World.Mobiles scan.
        /// </summary>
        protected int CountCanSwim()
        {
            int count = 0;

            foreach (int serial in _spawnedSerials)
            {
                if (World.FindMobile(serial) is BaseCreature bc && !bc.Deleted && bc.CanSwim)
                    count++;
            }

            return count;
        }

        #region Static Query Methods

        /// <summary>
        /// Finds all creatures owned by a specific spawner type.
        /// On-demand query - used sparingly for cleanup/validation operations.
        /// </summary>
        public static List<BaseCreature> FindAll<T>() where T : UOR_Spawner
        {
            var results = new List<BaseCreature>();

            foreach (var mobile in World.Mobiles.Values)
            {
                if (mobile is BaseCreature bc && !bc.Deleted && bc.Spawner is T)
                {
                    results.Add(bc);
                }
            }

            return results;
        }

        /// <summary>
        /// Finds all creatures owned by a specific spawner instance.
        /// </summary>
        public static List<BaseCreature> FindAll(UOR_Spawner spawner)
        {
            var results = new List<BaseCreature>();

            if (spawner == null)
                return results;

            foreach (var mobile in World.Mobiles.Values)
            {
                if (mobile is BaseCreature bc && !bc.Deleted && bc.Spawner == spawner)
                {
                    results.Add(bc);
                }
            }

            return results;
        }

        /// <summary>
        /// Counts all creatures owned by a specific spawner type.
        /// Uses full scan - prefer cached counts on singleton spawners for frequent queries.
        /// </summary>
        public static int Count<T>() where T : UOR_Spawner
        {
            int count = 0;

            foreach (var mobile in World.Mobiles.Values)
            {
                if (mobile is BaseCreature bc && !bc.Deleted && bc.Spawner is T)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Deletes all creatures owned by a specific spawner type.
        /// Returns the count of deleted creatures.
        /// </summary>
        public static int DeleteAll<T>() where T : UOR_Spawner
        {
            var toDelete = FindAll<T>();
            int deleted = 0;

            foreach (var creature in toDelete)
            {
                if (creature != null && !creature.Deleted)
                {
                    creature.Delete();
                    deleted++;
                }
            }

            return deleted;
        }

        /// <summary>
        /// Checks if a creature belongs to any UOR spawner.
        /// </summary>
        public static bool IsUORSpawn(BaseCreature creature)
        {
            return creature?.Spawner is UOR_Spawner;
        }

        /// <summary>
        /// Checks if a creature belongs to a specific spawner type.
        /// </summary>
        public static bool IsSpawnType<T>(BaseCreature creature) where T : UOR_Spawner
        {
            return creature?.Spawner is T;
        }

        /// <summary>
        /// Finds a creature by serial if owned by specific spawner type.
        /// O(1) lookup - use when you have a serial reference.
        /// </summary>
        public static BaseCreature Find<T>(Serial serial) where T : UOR_Spawner
        {
            if (World.FindMobile(serial) is BaseCreature bc && !bc.Deleted && bc.Spawner is T)
            {
                return bc;
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// Spawner for main creature spawn (non-vendor mobiles).
    /// Cleaned up on server start when system initializes.
    /// Uses cached count for efficient frequent queries.
    /// </summary>
    public sealed class UOR_MobSpawner : UOR_Spawner
    {
        private static UOR_MobSpawner _instance;
        private int _cachedCount;
        private DateTime _lastCacheValidation;
        private static readonly TimeSpan CacheValidationInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Singleton instance for mob spawner.
        /// Creates new instance if not yet loaded from world save.
        /// </summary>
        public static UOR_MobSpawner Instance
        {
            get
            {
                if (_instance == null || _instance.Deleted)
                {
                    _instance = new UOR_MobSpawner();
                }
                return _instance;
            }
        }

        public override string SpawnerName => "UOR_MobSpawner";

        public override int DefaultHomeRange => 50;

        public override bool UnlinkOnTaming => true;

        private UOR_MobSpawner() : base()
        { 
            _cachedCount = 0;
            _lastCacheValidation = DateTime.MinValue;
            Name = "UOR Mob Spawner"; // Shows in MySpawner props
        }

        /// <summary>
        /// Public serial constructor required for ServUO deserialization.
        /// </summary>
        public UOR_MobSpawner(Serial serial) : base(serial)
        {
            _cachedCount = 0;
            _lastCacheValidation = DateTime.MinValue;
        }

        /// <summary>
        /// Claims a creature, incrementing cached count.
        /// </summary>
        public new void Claim(BaseCreature creature, Point3D homeLocation = default)
        {
            if (creature == null || creature.Deleted)
                return;

            // Only increment if not already claimed by this spawner
            if (creature.Spawner != this)
            {
                base.Claim(creature, homeLocation);
                _cachedCount++;
            }
        }

        /// <summary>
        /// Called when spawn is deleted - decrements cached count and removes from tracking.
        /// </summary>
        protected override void OnSpawnRemoved(ISpawnable spawn)
        {
            base.OnSpawnRemoved(spawn); // Remove from serial tracking list
            if (_cachedCount > 0)
                _cachedCount--;
        }

        /// <summary>
        /// Finds all creatures owned by the mob spawner.
        /// </summary>
        public static List<BaseCreature> GetAllSpawn()
        {
            return FindAll<UOR_MobSpawner>();
        }

        /// <summary>
        /// Deletes all creatures owned by the mob spawner.
        /// Used on server start/restart. Resets cached count.
        /// </summary>
        public static int CleanupAll()
        {
            int deleted = DeleteAll<UOR_MobSpawner>();
            Instance._cachedCount = 0;
            return deleted;
        }

        /// <summary>
        /// Gets count of active mob spawn using cached value.
        /// Validates cache periodically for accuracy.
        /// </summary>
        public static int GetCount()
        {
            var instance = Instance;

            // Periodic validation to correct any drift
            if (DateTime.UtcNow - instance._lastCacheValidation > CacheValidationInterval)
            {
                instance._cachedCount = Count<UOR_MobSpawner>();
                instance._lastCacheValidation = DateTime.UtcNow;
            }

            return instance._cachedCount;
        }

        /// <summary>
        /// Counts mob spawn that can swim. Uses serial list — avoids full World.Mobiles scan.
        /// </summary>
        public static int CountSwimmers()
        {
            return Instance.CountCanSwim();
        }

        /// <summary>
        /// Finds a mob spawn by serial. O(1) lookup.
        /// </summary>
        public static BaseCreature Find(Serial serial)
        {
            return Find<UOR_MobSpawner>(serial);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            _ = reader.ReadInt();

            // Restore singleton on deserialize
            _instance = this;
            _cachedCount = 0;
            _lastCacheValidation = DateTime.MinValue;
        }
    }

    /// <summary>
    /// Spawner for vendor group spawn (vendors, TownNPC, extras).
    /// Only cleaned up when explicitly requested (toggle off, reset, etc).
    /// Uses cached count for efficient frequent queries.
    /// </summary>
    public sealed class UOR_VendorSpawner : UOR_Spawner
    {
        private static UOR_VendorSpawner _instance;
        private int _cachedCount;
        private DateTime _lastCacheValidation;
        private static readonly TimeSpan CacheValidationInterval = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Singleton instance for vendor spawner.
        /// Creates new instance if not yet loaded from world save.
        /// </summary>
        public static UOR_VendorSpawner Instance
        {
            get
            {
                if (_instance == null || _instance.Deleted)
                {
                    _instance = new UOR_VendorSpawner();
                }
                return _instance;
            }
        }

        public override string SpawnerName => "UOR_VendorSpawner";

        public override int DefaultHomeRange => 15;

        /// <summary>
        /// Vendors should stay with their spawner even when "tamed" (hired).
        /// </summary>
        public override bool UnlinkOnTaming => false;

        private UOR_VendorSpawner() : base()
        { 
            _cachedCount = 0;
            _lastCacheValidation = DateTime.MinValue;
            Name = "UOR Vendor Spawner"; // Shows in MySpawner props
        }

        /// <summary>
        /// Public serial constructor required for ServUO deserialization.
        /// </summary>
        public UOR_VendorSpawner(Serial serial) : base(serial)
        {
            _cachedCount = 0;
            _lastCacheValidation = DateTime.MinValue;
        }

        /// <summary>
        /// Claims a vendor, incrementing cached count.
        /// </summary>
        public new void Claim(BaseCreature creature, Point3D homeLocation = default)
        {
            if (creature == null || creature.Deleted)
                return;

            // Only increment if not already claimed by this spawner
            if (creature.Spawner != this)
            {
                base.Claim(creature, homeLocation);
                _cachedCount++;
            }
        }

        /// <summary>
        /// Called when vendor is deleted - decrements cached count and removes from tracking.
        /// </summary>
        protected override void OnSpawnRemoved(ISpawnable spawn)
        {
            base.OnSpawnRemoved(spawn); // Remove from serial tracking list
            if (_cachedCount > 0)
                _cachedCount--;
        }

        /// <summary>
        /// Finds all creatures owned by the vendor spawner.
        /// </summary>
        public static List<BaseCreature> GetAllSpawn()
        {
            return FindAll<UOR_VendorSpawner>();
        }

        /// <summary>
        /// Deletes all creatures owned by the vendor spawner.
        /// Used when vendor system is disabled or reset. Resets cached count.
        /// </summary>
        public static int CleanupAll()
        {
            int deleted = DeleteAll<UOR_VendorSpawner>();
            Instance._cachedCount = 0;
            return deleted;
        }

        /// <summary>
        /// Gets count of active vendor spawn using cached value.
        /// Validates cache periodically for accuracy.
        /// </summary>
        public static int GetCount()
        {
            var instance = Instance;

            // Periodic validation to correct any drift (less frequent for vendors)
            if (DateTime.UtcNow - instance._lastCacheValidation > CacheValidationInterval)
            {
                instance._cachedCount = Count<UOR_VendorSpawner>();
                instance._lastCacheValidation = DateTime.UtcNow;
            }

            return instance._cachedCount;
        }

        /// <summary>
        /// Finds a vendor spawn by serial. O(1) lookup.
        /// </summary>
        public static BaseCreature Find(Serial serial)
        {
            return Find<UOR_VendorSpawner>(serial);
        }

        /// <summary>
        /// Gets all vendors (BaseVendor types only).
        /// </summary>
        public static List<BaseVendor> GetAllVendors()
        {
            var results = new List<BaseVendor>();

            foreach (var mobile in World.Mobiles.Values)
            {
                if (mobile is BaseVendor bv && !bv.Deleted && bv.Spawner is UOR_VendorSpawner)
                {
                    results.Add(bv);
                }
            }

            return results;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            _ = reader.ReadInt();

            // Restore singleton on deserialize
            _instance = this;
            _cachedCount = 0;
            _lastCacheValidation = DateTime.MinValue;
        }
    }
}
