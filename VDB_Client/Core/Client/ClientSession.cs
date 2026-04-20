using System;
using System.Collections.Generic;

namespace VDB.Client.Core.Client
{
    /// <summary>
    /// State the server keeps for each connected peer that has completed (or is
    /// pending) the VDBClient handshake.
    /// </summary>
    public sealed class ClientSession
    {
        public long         PeerID        { get; set; }
        public string       SteamID       { get; set; }
        public string       CharacterName { get; set; }
        public string       ClientVersion { get; set; }

        /// <summary>Roles resolved from VDB at handshake time.</summary>
        public List<string> Roles         { get; set; } = new List<string>();

        public SessionState State         { get; set; } = SessionState.Pending;

        /// <summary>UTC time the peer connected; used to enforce the handshake timeout.</summary>
        public DateTime     ConnectedAt   { get; set; } = DateTime.UtcNow;

        // Convenience helpers
        public bool IsAdmin =>
            Roles.Exists(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase));
        public bool HasRole(string role) =>
            Roles.Exists(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }

    public enum SessionState
    {
        /// <summary>Connected but handshake not yet received.</summary>
        Pending,
        /// <summary>Handshake received and accepted; player is in good standing.</summary>
        Authenticated,
        /// <summary>Handshake rejected or timed out; kick is in progress.</summary>
        Rejected
    }
}
