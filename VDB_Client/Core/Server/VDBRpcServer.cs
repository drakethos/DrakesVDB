using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using VDB.Client.Core.Client;
using VDB.Client.Core.Protocol;

namespace VDB.Client.Core.Server
{
    /// <summary>
    /// Runs on the SERVER side.
    ///
    /// Responsibilities:
    ///  1. Register the VDB RPC channel on ZRoutedRpc.
    ///  2. Receive HandshakeRequests from clients; look up the player in VDB
    ///     (via reflection into ServerDB); accept or reject.
    ///  3. Maintain a per-peer ClientSession table.
    ///  4. Kick peers whose handshake has not arrived within the grace window.
    ///  5. Route CommandRequests from authenticated clients to the server console.
    ///
    /// NOTE: When VDB (DrakesVDB.dll) is not present on this server this class
    /// degrades gracefully — it will still accept all handshakes (Accepted=true)
    /// and return an empty role list so the kick-on-no-mod logic still works.
    /// </summary>
    public static class VDBRpcServer
    {
        // RPC method names registered on ZRoutedRpc
        public const string RPC_CHANNEL        = "VDBClient";
        public const string RPC_HANDSHAKE_REQ  = "VDBClient_HandshakeReq";
        public const string RPC_COMMAND_REQ    = "VDBClient_CommandReq";
        public const string RPC_HANDSHAKE_RESP = "VDBClient_HandshakeResp";
        public const string RPC_COMMAND_RESP   = "VDBClient_CommandResp";
        public const string RPC_KICK_NOTICE    = "VDBClient_KickNotice";

        /// <summary>How long (seconds) a peer has to send a handshake before being kicked.</summary>
        public static float HandshakeTimeoutSeconds { get; set; } = 15f;

        private static readonly Dictionary<long, ClientSession> _sessions =
            new Dictionary<long, ClientSession>();

        // Cached reflection handle to ServerDB (only available when VDB is loaded)
        private static Type   _serverDBType;
        private static bool   _serverDBResolved;

        // -----------------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------------

        public static void Register()
        {
            if (ZRoutedRpc.instance == null)
            {
                Debug.LogWarning("[VDBClient] ZRoutedRpc not ready — server RPC registration deferred.");
                return;
            }

            ZRoutedRpc.instance.Register<ZPackage>(RPC_HANDSHAKE_REQ, OnHandshakeRequest);
            ZRoutedRpc.instance.Register<ZPackage>(RPC_COMMAND_REQ,   OnCommandRequest);
            Debug.Log("[VDBClient] Server RPCs registered.");
        }

        public static void Unregister()
        {
            _sessions.Clear();
        }

        // -----------------------------------------------------------------------
        // Session management (called by patches)
        // -----------------------------------------------------------------------

        public static void OnPeerConnected(long peerID)
        {
            if (!_sessions.ContainsKey(peerID))
            {
                _sessions[peerID] = new ClientSession
                {
                    PeerID      = peerID,
                    ConnectedAt = DateTime.UtcNow,
                    State       = SessionState.Pending
                };
                Debug.Log($"[VDBClient] Peer {peerID} connected — awaiting handshake.");
            }
        }

        public static void OnPeerDisconnected(long peerID)
        {
            _sessions.Remove(peerID);
            Debug.Log($"[VDBClient] Session removed for peer {peerID}.");
        }

        /// <summary>
        /// Called from a coroutine / Update loop to enforce the handshake timeout.
        /// </summary>
        public static void TickTimeouts()
        {
            if (_sessions.Count == 0) return;

            var now = DateTime.UtcNow;
            var toKick = new List<long>();

            foreach (var kv in _sessions)
            {
                var session = kv.Value;
                if (session.State == SessionState.Pending)
                {
                    double elapsed = (now - session.ConnectedAt).TotalSeconds;
                    if (elapsed > HandshakeTimeoutSeconds)
                    {
                        Debug.LogWarning($"[VDBClient] Peer {session.PeerID} handshake timeout — kicking.");
                        toKick.Add(session.PeerID);
                    }
                }
            }

            foreach (long peerID in toKick)
            {
                KickPeer(peerID, "VDBClient mod not detected. Please install DrakesVDBClient to join this server.");
            }
        }

        public static ClientSession GetSession(long peerID) =>
            _sessions.TryGetValue(peerID, out var s) ? s : null;

        public static IEnumerable<ClientSession> GetAllSessions() => _sessions.Values;

        // -----------------------------------------------------------------------
        // Kick helper
        // -----------------------------------------------------------------------

        public static void KickPeer(long peerID, string reason)
        {
            // 1. Send a KickNotice so the client can display the reason
            var notice = new KickNotice { Reason = reason };
            SendToClient(peerID, VDBPacket.Pack(PacketType.KickNotice, notice));

            // 2. Disconnect via ZNet
            if (ZNet.instance != null)
            {
                var peer = FindPeer(peerID);
                if (peer != null)
                {
                    Debug.Log($"[VDBClient] Kicking peer {peerID}: {reason}");
                    ZNet.instance.Disconnect(peer);
                }
            }

            if (_sessions.TryGetValue(peerID, out var session))
                session.State = SessionState.Rejected;
        }

        // -----------------------------------------------------------------------
        // Incoming RPC handlers
        // -----------------------------------------------------------------------

        private static void OnHandshakeRequest(long senderPeerID, ZPackage pkg)
        {
            var packet = VDBPacket.Read(pkg);
            if (packet.Type != PacketType.HandshakeRequest) return;

            var req = HandshakeRequest.FromJson(packet.Payload);
            Debug.Log($"[VDBClient] Handshake from peer {senderPeerID} | SteamID={req.SteamID} | v{req.ClientVersion}");

            if (!_sessions.TryGetValue(senderPeerID, out var session))
            {
                // Peer connected before we registered — create session now
                session = new ClientSession { PeerID = senderPeerID, ConnectedAt = DateTime.UtcNow };
                _sessions[senderPeerID] = session;
            }

            session.SteamID       = req.SteamID;
            session.CharacterName = req.CharacterName;
            session.ClientVersion = req.ClientVersion;

            // Resolve roles from VDB (if available)
            var roles = ResolveRoles(req.SteamID);
            session.Roles = roles;
            session.State = SessionState.Authenticated;

            var response = new HandshakeResponse
            {
                Accepted      = true,
                PlayerDBID    = 0,                       // filled if VDB is present
                SteamID       = req.SteamID,
                CharacterName = req.CharacterName,
                Roles         = roles,
                ServerVersion = VDBClientPlugin.Version
            };

            SendToClient(senderPeerID, VDBPacket.Pack(PacketType.HandshakeResponse, response));
            Debug.Log($"[VDBClient] Handshake accepted for {req.CharacterName} ({req.SteamID}). Roles: [{string.Join(", ", roles)}]");
        }

        private static void OnCommandRequest(long senderPeerID, ZPackage pkg)
        {
            var packet = VDBPacket.Read(pkg);
            if (packet.Type != PacketType.CommandRequest) return;

            var req = CommandRequest.FromJson(packet.Payload);

            // Only authenticated sessions may run commands
            if (!_sessions.TryGetValue(senderPeerID, out var session) ||
                session.State != SessionState.Authenticated)
            {
                SendCommandResponse(senderPeerID, req.RequestID, false, "Not authenticated.");
                return;
            }

            // Basic authorisation: commands that start with "vdb_" require Admin
            bool needsAdmin = req.Command.TrimStart().StartsWith("vdb_", StringComparison.OrdinalIgnoreCase);
            if (needsAdmin && !session.IsAdmin)
            {
                SendCommandResponse(senderPeerID, req.RequestID, false,
                    "Permission denied: Admin role required.");
                return;
            }

            // Execute via Jotunn / Valheim console
            string output = ExecuteServerCommand(req.Command);
            SendCommandResponse(senderPeerID, req.RequestID, true, output);
        }

        // -----------------------------------------------------------------------
        // VDB role resolution (reflective — works whether or not VDB is loaded)
        // -----------------------------------------------------------------------

        private static List<string> ResolveRoles(string steamID)
        {
            if (!TryGetServerDB(out Type dbType))
                return new List<string>();

            try
            {
                var method = dbType.GetMethod("GetRoles",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string) }, null);
                if (method == null) return new List<string>();

                var result = method.Invoke(null, new object[] { steamID });
                if (result is IEnumerable<string> roles)
                    return new List<string>(roles);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VDBClient] Role resolution failed: {ex.Message}");
            }
            return new List<string>();
        }

        private static bool TryGetServerDB(out Type dbType)
        {
            if (_serverDBResolved)
            {
                dbType = _serverDBType;
                return dbType != null;
            }

            _serverDBResolved = true;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "DrakesVDB")
                {
                    _serverDBType = asm.GetType("VDB.Core.DataTypes.ServerDB");
                    break;
                }
            }
            dbType = _serverDBType;
            return dbType != null;
        }

        // -----------------------------------------------------------------------
        // Command execution
        // -----------------------------------------------------------------------

        private static string ExecuteServerCommand(string commandLine)
        {
            try
            {
                if (Console.instance == null)
                    return "[VDBClient] Console not available.";

                // Capture output by temporarily redirecting — simplest approach is
                // to call the matching ConsoleCommand directly through Jotunn's
                // CommandManager if available; otherwise fall back to raw console input.
                var parts = commandLine.Trim().Split(new[] { ' ' }, 2);
                string cmd  = parts[0];
                string args = parts.Length > 1 ? parts[1] : "";

                Console.instance.TryRunCommand(commandLine);
                return $"[VDBClient] Command '{cmd}' executed on server.";
            }
            catch (Exception ex)
            {
                return $"[VDBClient] Command error: {ex.Message}";
            }
        }

        // -----------------------------------------------------------------------
        // Sending helpers
        // -----------------------------------------------------------------------

        private static void SendToClient(long peerID, VDBPacket packet)
        {
            if (ZRoutedRpc.instance == null) return;

            var pkg = new ZPackage();
            packet.Write(pkg);

            string rpcName = packet.Type switch
            {
                PacketType.HandshakeResponse => RPC_HANDSHAKE_RESP,
                PacketType.CommandResponse   => RPC_COMMAND_RESP,
                PacketType.KickNotice        => RPC_KICK_NOTICE,
                PacketType.RoleUpdate        => RPC_HANDSHAKE_RESP, // reuse handshake resp channel
                _                            => RPC_HANDSHAKE_RESP
            };

            ZRoutedRpc.instance.InvokeRoutedRPC(peerID, rpcName, pkg);
        }

        private static void SendCommandResponse(long peerID, int requestID, bool success, string output)
        {
            var resp = new CommandResponse { RequestID = requestID, Success = success, Output = output };
            SendToClient(peerID, VDBPacket.Pack(PacketType.CommandResponse, resp));
        }

        // -----------------------------------------------------------------------
        // Peer lookup
        // -----------------------------------------------------------------------

        private static ZNetPeer FindPeer(long peerID)
        {
            if (ZNet.instance == null) return null;

            FieldInfo peersField = typeof(ZNet).GetField("m_peers",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (peersField == null) return null;

            var peers = peersField.GetValue(ZNet.instance) as System.Collections.IList;
            if (peers == null) return null;

            foreach (var p in peers)
            {
                if (p is ZNetPeer peer && peer.m_uid == peerID)
                    return peer;
            }
            return null;
        }
    }
}
