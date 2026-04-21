using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VDB.Core.DataTypes.Util;

public static class Helper
{
    /// <summary>Valheim / Steam sockets often expose IDs as <c>Steam_76561198012345678</c>. Extracts the 64-bit Steam account id.</summary>
    public static bool TryParseSteamIdFromHostString(string? raw, out ulong steamId)
    {
        steamId = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var s = raw.Trim();
        if (s.StartsWith("Steam_", StringComparison.OrdinalIgnoreCase))
            s = s.Substring("Steam_".Length);

        return ulong.TryParse(s, out steamId) && steamId > 0;
    }

    public static ulong? TryParseSteamIdFromHostStringNullable(string? raw) =>
        TryParseSteamIdFromHostString(raw, out var id) ? id : null;

    public static ulong? getSteamId(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return null;

        var fromPeers = TryGetSteamIdFromZNetPeers(playerName);
        if (fromPeers.HasValue) return fromPeers;

        var local = global::Player.m_localPlayer;
        if (local == null) return null;
        if (!local.GetPlayerName().Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase))
            return null;

        var sw = TryGetSteamIdViaSteamworks();
        if (TryParseSteamIdFromHostString(sw, out var fromSw)) return fromSw;

        var localStr = GetLocalSteamID();
        return TryParseSteamIdFromHostStringNullable(localStr);
    }

    public static string TryGetSteamIdViaSteamworks()
    {
        try
        {
            var steamUserType = Type.GetType("Steamworks.SteamUser, Assembly-CSharp") ??
                                Type.GetType("Steamworks.SteamUser");
            if (steamUserType != null)
            {
                var getSteamIDMethod = steamUserType.GetMethod("GetSteamID", BindingFlags.Public | BindingFlags.Static);
                if (getSteamIDMethod != null)
                {
                    var csteamId = getSteamIDMethod.Invoke(null, null);
                    if (csteamId != null)
                    {
                        var toStringMethod = csteamId.GetType().GetMethod("ToString", Type.EmptyTypes);
                        if (toStringMethod != null)
                            return toStringMethod.Invoke(csteamId, null)?.ToString();

                        var m_val = csteamId.GetType().GetField("m_SteamID",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (m_val != null)
                            return m_val.GetValue(csteamId)?.ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"[VDB] Steamworks reflection failed: {ex}");
        }

        return null;
    }

    /// <summary>Match <paramref name="characterOrPlayerName"/> against peer character or platform name, then parse <see cref="ISocket.GetHostName"/> (e.g. <c>Steam_7656119…</c>).</summary>
    public static ulong? TryGetSteamIdFromZNetPeers(string characterOrPlayerName)
    {
        try
        {
            if (ZNet.instance == null) return null;

            var peersField = typeof(ZNet).GetField("m_peers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (peersField == null) return null;

            var peers = peersField.GetValue(ZNet.instance) as IList<ZNetPeer>;
            if (peers == null) return null;

            foreach (var peer in peers)
            {
                if (peer == null || peer.m_socket == null) continue;

                var charName = ReadPeerStringField(peer, "m_characterName");
                var playerName = ReadPeerStringField(peer, "m_playerName");
                if (!NameMatches(characterOrPlayerName, charName) && !NameMatches(characterOrPlayerName, playerName))
                    continue;

                var hostName = peer.m_socket.GetHostName();
                if (TryParseSteamIdFromHostString(hostName, out var steamId))
                    return steamId;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VDB] Error retrieving SteamID from ZNet peers: {ex}");
        }

        return null;
    }

    private static bool NameMatches(string needle, string? hay)
    {
        if (string.IsNullOrEmpty(hay)) return false;
        return hay.Equals(needle.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadPeerStringField(ZNetPeer peer, string fieldName)
    {
        var f = typeof(ZNetPeer).GetField(fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return f?.GetValue(peer)?.ToString();
    }

    public static string? GetLocalSteamID()
    {
        try
        {
            var peersField = typeof(ZNet).GetField("m_peers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (peersField == null) return null;

            var peers = peersField.GetValue(ZNet.instance) as IList;
            if (peers == null || peers.Count == 0) return null;

            foreach (var peer in peers)
            {
                var nameField = peer.GetType().GetField("m_playerName", BindingFlags.NonPublic | BindingFlags.Instance);
                var name = nameField?.GetValue(peer)?.ToString();
                if (name != global::Player.m_localPlayer.GetPlayerName()) continue;

                var rpcField = peer.GetType().GetField("m_rpc", BindingFlags.NonPublic | BindingFlags.Instance);
                var rpc = rpcField?.GetValue(peer);
                if (rpc == null) continue;

                var socketField = rpc.GetType().GetField("m_socket", BindingFlags.NonPublic | BindingFlags.Instance);
                var socket = socketField?.GetValue(rpc);
                if (socket == null) continue;

                var peerIDField = socket.GetType().GetField("m_peerID",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var peerID = peerIDField?.GetValue(socket);
                if (peerID == null) continue;

                var raw = peerID.ToString();
                if (TryParseSteamIdFromHostString(raw, out var sid))
                    return sid.ToString();
                return raw;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[VDB] SteamHelper.GetLocalSteamID failed: {ex}");
        }

        return null;
    }
}
