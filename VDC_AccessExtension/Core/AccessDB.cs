using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using LiteDB;
using UnityEngine;
using VDC.AccessExtension.Core.DataTypes;

namespace VDC.AccessExtension.Core
{
    /// <summary>
    /// Manages the VDC Access Extension LiteDB collections.
    /// Uses a separate DB file from VDB so the two mods remain independently deployable.
    /// </summary>
    public static class AccessDB
    {
        private static LiteDatabase                    _db;
        private static ILiteCollection<AccessEntry>    _entries;
        private static ILiteCollection<AccessList>     _lists;
        private static ILiteCollection<AccessLog>      _logs;

        private const string DefaultDBName = "VDCAccess.db";

        // -----------------------------------------------------------------------
        // Initialisation
        // -----------------------------------------------------------------------

        public static void Initialize(string dbName = DefaultDBName)
        {
            try
            {
                string folder = Path.Combine(Paths.PluginPath, "DrakeMods-DrakesVDCAccess");
                Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, dbName);
                _db      = new LiteDatabase(path);
                _entries = _db.GetCollection<AccessEntry>("AccessEntries");
                _lists   = _db.GetCollection<AccessList>("AccessLists");
                _logs    = _db.GetCollection<AccessLog>("AccessLogs");

                // Composite unique index: one row per (SteamID, ListType)
                _entries.EnsureIndex(x => x.SteamID);
                _entries.EnsureIndex(x => x.ListType);

                _lists.EnsureIndex(x => x.ListType, true);
                _logs.EnsureIndex(x => x.Timestamp);

                SeedListConfig();
                Debug.Log($"[VDCAccess] Database initialised at: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VDCAccess] Failed to initialise database: {ex}");
            }
        }

        /// <summary>
        /// Ensures a config row exists for every list type.
        /// Existing rows are left untouched so admin changes persist.
        /// </summary>
        private static void SeedListConfig()
        {
            foreach (AccessListType t in Enum.GetValues(typeof(AccessListType)))
            {
                if (_lists.FindOne(l => l.ListType == t) == null)
                {
                    _lists.Insert(new AccessList { ListType = t, Enabled = true, SyncToNative = true });
                }
            }
        }

        // -----------------------------------------------------------------------
        // List config
        // -----------------------------------------------------------------------

        public static AccessList GetListConfig(AccessListType type) =>
            _lists.FindOne(l => l.ListType == type);

        public static void SetListConfig(AccessListType type, bool enabled, bool syncToNative)
        {
            var cfg = _lists.FindOne(l => l.ListType == type);
            if (cfg == null) return;
            cfg.Enabled       = enabled;
            cfg.SyncToNative  = syncToNative;
            _lists.Update(cfg);
        }

        // -----------------------------------------------------------------------
        // Entry CRUD
        // -----------------------------------------------------------------------

        /// <summary>
        /// Adds or re-activates an entry on the given list.
        /// Returns the entry (new or existing).
        /// </summary>
        public static AccessEntry AddEntry(string steamID, string playerName,
                                           AccessListType listType, string reason = null,
                                           DateTime? expiresAt = null)
        {
            var existing = _entries.FindOne(e => e.SteamID == steamID && e.ListType == listType);
            if (existing != null)
            {
                // Re-activate if it was deactivated
                existing.Active      = true;
                existing.PlayerName  = playerName ?? existing.PlayerName;
                existing.Reason      = reason     ?? existing.Reason;
                existing.ExpiresAt   = expiresAt  ?? existing.ExpiresAt;
                _entries.Update(existing);
                return existing;
            }

            var entry = new AccessEntry
            {
                SteamID    = steamID,
                PlayerName = playerName ?? "Unknown",
                ListType   = listType,
                Active     = true,
                Reason     = reason,
                ExpiresAt  = expiresAt,
                CreatedAt  = DateTime.UtcNow
            };
            _entries.Insert(entry);
            return entry;
        }

        /// <summary>
        /// Deactivates (soft-deletes) an entry. Returns false if not found.
        /// </summary>
        public static bool RemoveEntry(string steamID, AccessListType listType)
        {
            var entry = _entries.FindOne(e => e.SteamID == steamID && e.ListType == listType);
            if (entry == null) return false;
            entry.Active = false;
            _entries.Update(entry);
            return true;
        }

        /// <summary>
        /// Returns true when the player has an active, non-expired entry on the given list.
        /// </summary>
        public static bool IsOnList(string steamID, AccessListType listType)
        {
            var entry = _entries.FindOne(e => e.SteamID == steamID &&
                                              e.ListType == listType &&
                                              e.Active   == true);
            if (entry == null) return false;

            // Honour expiry
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
            {
                entry.Active = false;
                _entries.Update(entry);
                return false;
            }

            return true;
        }

        public static IEnumerable<AccessEntry> GetAllEntries(AccessListType listType) =>
            _entries.Find(e => e.ListType == listType && e.Active == true);

        public static IEnumerable<AccessEntry> GetAllActiveEntries() =>
            _entries.Find(e => e.Active == true);

        // -----------------------------------------------------------------------
        // Audit log
        // -----------------------------------------------------------------------

        public static void LogDecision(string steamID, string playerName,
                                       AccessListType listType, bool granted, string source)
        {
            _logs.Insert(new AccessLog
            {
                SteamID    = steamID,
                PlayerName = playerName,
                ListType   = listType,
                Granted    = granted,
                Source     = source,
                Timestamp  = DateTime.UtcNow
            });
        }

        public static IEnumerable<AccessLog> GetRecentLogs(int count = 50) =>
            _logs.FindAll().OrderByDescending(l => l.Timestamp).Take(count);
    }
}
