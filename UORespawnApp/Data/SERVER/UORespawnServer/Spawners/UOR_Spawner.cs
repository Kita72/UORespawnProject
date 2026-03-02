using System;
using System.Collections.Generic;
using System.Linq;

using Server.ContextMenus;
using Server.Mobiles;

namespace Server.Custom.UORespawnServer.Spawners
{
    /// <summary>
    /// Base ISpawner implementation for UORespawn system.
    /// Provides ownership tracking via ServUO's built-in ISpawnable.Spawner pattern.
    /// No markers, no serial tracking - just clean DI-based spawn identification.
    /// </summary>
    internal abstract class UOR_Spawner : ISpawner
    {
        /// <summary>
        /// Display name for logging purposes.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Default home range for spawned creatures.
        /// </summary>
        public virtual int HomeRange => 50;

        /// <summary>
        /// Whether creatures should be unlinked from this spawner when tamed.
        /// True = tamed creatures lose spawner reference (recommended).
        /// </summary>
        public virtual bool UnlinkOnTaming => true;

        /// <summary>
        /// Home location is determined per-creature at spawn time, not globally.
        /// Returns Point3D.Zero as placeholder; actual home is set on creature.Home property.
        /// </summary>
        public virtual Point3D HomeLocation => Point3D.Zero;

        /// <summary>
        /// Called by ServUO when a spawn should be removed from this spawner's control.
        /// </summary>
        public virtual void Remove(ISpawnable spawn)
        {
            // No tracking list to remove from - we use on-demand queries
            // Just let the creature handle its own deletion
        }

        /// <summary>
        /// Adds spawn properties to the creature's property list (for [props] command).
        /// </summary>
        public virtual void GetSpawnProperties(ISpawnable spawn, ObjectPropertyList list)
        {
            list.Add($"UOR Spawner: {Name}");
        }

        /// <summary>
        /// Adds context menu entries for the spawned creature.
        /// </summary>
        public virtual void GetSpawnContextEntries(ISpawnable spawn, Mobile m, List<ContextMenuEntry> list)
        {
            // No special context menu entries needed
        }

        /// <summary>
        /// Assigns this spawner to a creature, establishing ownership.
        /// This is the key pattern - creature.Spawner = this.
        /// </summary>
        public void Claim(BaseCreature creature, Point3D homeLocation = default)
        {
            if (creature == null || creature.Deleted)
                return;

            creature.Spawner = this;
            creature.Home = homeLocation != default ? homeLocation : creature.Location;
            creature.RangeHome = HomeRange;
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
            }
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
    internal sealed class UOR_MobSpawner : UOR_Spawner
    {
        private static UOR_MobSpawner _instance;
        private int _cachedCount;
        private DateTime _lastCacheValidation;
        private static readonly TimeSpan CacheValidationInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Singleton instance for mob spawner.
        /// </summary>
        public static UOR_MobSpawner Instance => _instance ?? (_instance = new UOR_MobSpawner());

        public override string Name => "MobSpawner";

        public override int HomeRange => 50;

        private UOR_MobSpawner() 
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
        /// Called when spawn is deleted - decrements cached count.
        /// </summary>
        public override void Remove(ISpawnable spawn)
        {
            base.Remove(spawn);
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
        /// Finds a mob spawn by serial. O(1) lookup.
        /// </summary>
        public static BaseCreature Find(Serial serial)
        {
            return Find<UOR_MobSpawner>(serial);
        }
    }

    /// <summary>
    /// Spawner for vendor group spawn (vendors, TownNPC, extras).
    /// Only cleaned up when explicitly requested (toggle off, reset, etc).
    /// Uses cached count for efficient frequent queries.
    /// </summary>
    internal sealed class UOR_VendorSpawner : UOR_Spawner
    {
        private static UOR_VendorSpawner _instance;
        private int _cachedCount;
        private DateTime _lastCacheValidation;
        private static readonly TimeSpan CacheValidationInterval = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Singleton instance for vendor spawner.
        /// </summary>
        public static UOR_VendorSpawner Instance => _instance ?? (_instance = new UOR_VendorSpawner());

        public override string Name => "VendorSpawner";

        public override int HomeRange => 15;

        /// <summary>
        /// Vendors should stay with their spawner even when "tamed" (hired).
        /// </summary>
        public override bool UnlinkOnTaming => false;

        private UOR_VendorSpawner() 
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
        /// Called when vendor is deleted - decrements cached count.
        /// </summary>
        public override void Remove(ISpawnable spawn)
        {
            base.Remove(spawn);
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
    }
}
