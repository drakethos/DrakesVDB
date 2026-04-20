using System;

namespace VDC.AccessExtension.Core.DataTypes
{
    /// <summary>
    /// A single player entry on one of the three access lists.
    /// Keyed by SteamID + ListType so the same player can appear on multiple lists.
    /// </summary>
    public class AccessEntry
    {
        public int    ID         { get; set; }

        /// <summary>Steam / platform ID as a string (matches VDB Player.SteamID).</summary>
        public string SteamID    { get; set; }

        /// <summary>Last known character name — informational, not used as a key.</summary>
        public string PlayerName { get; set; }

        /// <summary>Which list this entry belongs to.</summary>
        public AccessListType ListType { get; set; }

        /// <summary>
        /// When false the entry is inactive (soft-delete / temporarily lifted).
        /// The game check ignores inactive entries.
        /// </summary>
        public bool   Active     { get; set; } = true;

        /// <summary>Optional human-readable reason (e.g. ban reason).</summary>
        public string Reason     { get; set; }

        /// <summary>UTC timestamp when the entry was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// If set, the entry automatically deactivates after this UTC time.
        /// Null = permanent.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}
