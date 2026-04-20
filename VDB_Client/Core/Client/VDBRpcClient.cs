using System;
using System.Collections.Generic;
using UnityEngine;
using VDB.Client.Core.Protocol;

namespace VDB.Client.Core.Client
{
    /// <summary>
    /// Runs on the CLIENT side (and on the server so the server can talk to itself
    /// in a listen-server / solo scenario).
    ///
    /// Responsibilities:
    ///  1. Register outgoing RPC names on ZRoutedRpc.
    ///  2. Send a HandshakeRequest as soon as a server connection is detected.
    ///  3. Receive and process HandshakeResponse, CommandResponse, KickNotice.
    ///  4. Expose a simple API that other mods / commands can call to send
    ///     CommandRequests to the server.
    /// </summary>
    public static class VDBRpcClient
    {
        // Mirror the server-side names
        public const string RPC_HANDSHAKE_REQ  = "VDBClient_HandshakeReq";
        public const string RPC_COMMAND_REQ    = "VDBClient_CommandReq";
        public const string RPC_HANDSHAKE_RESP = "VDBClient_HandshakeResp";
        public const string RPC_COMMAND_RESP   = "VDBClient_CommandResp";
        public const string RPC_KICK_NOTICE    = "VDBClient_KickNotice";

        // Local session state (set after a successful handshake)
        public static bool         IsAuthenticated  { get; private set; }
        public static string       SteamID          { get; private set; }
        public static string       CharacterName    { get; private set; }
        public static List<string> Roles            { get; private set; } = new List<string>();
        public static bool         IsAdmin          =>
            Roles.Exists(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        // Pending command callbacks keyed by RequestID
        private static readonly Dictionary<int, Action<CommandResponse>> _pendingCommands =
            new Dictionary<int, Action<CommandResponse>>();
        private static int _nextRequestID = 1;

        // -----------------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------------

        public static void Register()
        {
            if (ZRoutedRpc.instance == null)
            {
                Debug.LogWarning("[VDBClient] ZRoutedRpc not ready — client RPC registration deferred.");
                return;
            }

            ZRoutedRpc.instance.Register<ZPackage>(RPC_HANDSHAKE_RESP, OnHandshakeResponse);
            ZRoutedRpc.instance.Register<ZPackage>(RPC_COMMAND_RESP,   OnCommandResponse);
            ZRoutedRpc.instance.Register<ZPackage>(RPC_KICK_NOTICE,    OnKickNotice);
            Debug.Log("[VDBClient] Client RPCs registered.");
        }

        public static void Reset()
        {
            IsAuthenticated = false;
            SteamID         = null;
            CharacterName   = null;
            Roles           = new List<string>();
            _pendingCommands.Clear();
        }

        // -----------------------------------------------------------------------
        // Outgoing: Handshake
        // -----------------------------------------------------------------------

        /// <summary>
        /// Called as soon as we detect we're connected to a server.
        /// Sends our Steam ID, character name, and mod version to the server.
        /// </summary>
        public static void SendHandshake()
        {
            if (ZRoutedRpc.instance == null || Player.m_localPlayer == null)
            {
                Debug.LogWarning("[VDBClient] Cannot send handshake — ZRoutedRpc or local player not ready.");
                return;
            }

            string steamID = Player.m_localPlayer.GetPlayerID().ToString();
            string name    = Player.m_localPlayer.GetPlayerName();

            var req = new HandshakeRequest
            {
                SteamID       = steamID,
                CharacterName = name,
                ClientVersion = VDBClientPlugin.Version
            };

            var pkg = new ZPackage();
            VDBPacket.Pack(PacketType.HandshakeRequest, req).Write(pkg);

            // Route to server (peer ID 0 = server)
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_HANDSHAKE_REQ, pkg);
            Debug.Log($"[VDBClient] Handshake sent. SteamID={steamID} Name={name}");
        }

        // -----------------------------------------------------------------------
        // Outgoing: Command
        // -----------------------------------------------------------------------

        /// <summary>
        /// Sends a command to the server and calls <paramref name="callback"/> when
        /// the response arrives.  Pass null for fire-and-forget.
        /// </summary>
        public static void SendCommand(string commandLine, Action<CommandResponse> callback = null)
        {
            if (!IsAuthenticated)
            {
                Debug.LogWarning("[VDBClient] Cannot send command — not yet authenticated.");
                callback?.Invoke(new CommandResponse { Success = false, Output = "Not authenticated." });
                return;
            }

            int id  = _nextRequestID++;
            var req = new CommandRequest { Command = commandLine, RequestID = id };

            if (callback != null)
                _pendingCommands[id] = callback;

            var pkg = new ZPackage();
            VDBPacket.Pack(PacketType.CommandRequest, req).Write(pkg);

            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_COMMAND_REQ, pkg);
            Debug.Log($"[VDBClient] Command sent (id={id}): {commandLine}");
        }

        // -----------------------------------------------------------------------
        // Incoming RPC handlers
        // -----------------------------------------------------------------------

        private static void OnHandshakeResponse(long senderID, ZPackage pkg)
        {
            var packet = VDBPacket.Read(pkg);
            if (packet.Type != PacketType.HandshakeResponse) return;

            var resp = HandshakeResponse.FromJson(packet.Payload);

            if (!resp.Accepted)
            {
                Debug.LogWarning($"[VDBClient] Handshake rejected by server: {resp.RejectReason}");
                Console.instance?.Print($"[VDBClient] Server rejected authentication: {resp.RejectReason}");
                return;
            }

            IsAuthenticated = true;
            SteamID         = resp.SteamID;
            CharacterName   = resp.CharacterName;
            Roles           = resp.Roles ?? new List<string>();

            string roleList = Roles.Count > 0 ? string.Join(", ", Roles) : "(none)";
            Debug.Log($"[VDBClient] Authenticated. Roles: {roleList}");
            Console.instance?.Print($"[VDBClient] Connected to VDB server. Roles: {roleList}");
        }

        private static void OnCommandResponse(long senderID, ZPackage pkg)
        {
            var packet = VDBPacket.Read(pkg);
            if (packet.Type != PacketType.CommandResponse) return;

            var resp = CommandResponse.FromJson(packet.Payload);

            if (_pendingCommands.TryGetValue(resp.RequestID, out var cb))
            {
                _pendingCommands.Remove(resp.RequestID);
                cb?.Invoke(resp);
            }

            // Always echo output to client console
            if (!string.IsNullOrEmpty(resp.Output))
                Console.instance?.Print($"[VDB] {resp.Output}");
        }

        private static void OnKickNotice(long senderID, ZPackage pkg)
        {
            var packet = VDBPacket.Read(pkg);
            if (packet.Type != PacketType.KickNotice) return;

            var notice = KickNotice.FromJson(packet.Payload);
            string reason = string.IsNullOrEmpty(notice.Reason)
                ? "You have been disconnected by the server."
                : notice.Reason;

            Debug.LogWarning($"[VDBClient] Kick notice: {reason}");

            // Display prominently to the player
            Console.instance?.Print($"[VDBClient] DISCONNECTING: {reason}");

            // Optionally show in-game message box via Jotunn / MessageHud
            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, reason);
        }
    }
}
