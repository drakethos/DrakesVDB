using System;
using System.Reflection;
using UnityEngine;

namespace VDB.Core.DataTypes.Util;

public static class Helper
{
    /// <summary>
    /// Returns the Steam/platform ID for a peer by character name.
    /// Checks the local player first, then walks ZNet.m_peers.
    /// Returns 0 if not found.
    /// </summary>
    public static ulong getSteamId(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return 0;

        // Check local player first (works on both client and solo)
        if (Player.m_localPlayer != null &&
            Player.m_localPlayer.GetPlayerName().Equals(playerName, StringComparison.OrdinalIgnoreCase))
        {
            return (ulong)Player.m_localPlayer.GetPlayerID();
        }

        if (ZNet.instance == null)
            return 0;

        // Walk connected peers (server or client side)
        FieldInfo peersField = typeof(ZNet).GetField("m_peers", BindingFlags.NonPublic | BindingFlags.Instance);
        if (peersField == null)
        {
            Debug.LogError("[DrakeVDB] Could not find ZNet.m_peers field.");
            return 0;
        }

        var peers = peersField.GetValue(ZNet.instance) as System.Collections.IList;
        if (peers == null)
        {
            Debug.LogError("[DrakeVDB] ZNet.m_peers is null or not a list.");
            return 0;
        }

        foreach (var peer in peers)
        {
            if (peer == null) continue;
            Type peerType = peer.GetType();

            // ZNetPeer exposes m_characterName and m_uid as fields, not properties.
            string name = GetMemberValue<string>(peerType, peer, "m_characterName");
            if (name == null || !name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                continue;

            // m_uid is a ZDO peer ID stored as long; also try ulong.
            ulong uid = GetUidFromPeer(peerType, peer);
            if (uid != 0)
            {
                Debug.Log($"[DrakeVDB] Resolved SteamID {uid} for player '{playerName}'.");
                return uid;
            }
        }

        Debug.LogWarning($"[DrakeVDB] Player '{playerName}' not found in peer list.");
        return 0;
    }

    /// <summary>
    /// Reads a field or property by name from an object using reflection.
    /// </summary>
    private static T GetMemberValue<T>(Type type, object obj, string memberName)
    {
        // Try field first (public + non-public)
        FieldInfo field = type.GetField(memberName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            try { return (T)field.GetValue(obj); }
            catch { /* type mismatch — fall through */ }
        }

        // Try property
        PropertyInfo prop = type.GetProperty(memberName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null)
        {
            try { return (T)prop.GetValue(obj); }
            catch { /* type mismatch — fall through */ }
        }

        return default;
    }

    /// <summary>
    /// Extracts the peer UID handling the long / ulong variance across Valheim builds.
    /// </summary>
    private static ulong GetUidFromPeer(Type peerType, object peer)
    {
        const string fieldName = "m_uid";

        FieldInfo field = peerType.GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            object raw = field.GetValue(peer);
            if (raw is ulong ul) return ul;
            if (raw is long l)   return (ulong)l;
            // Some Valheim builds wrap it in a struct with a .id field
            if (raw != null)
            {
                FieldInfo inner = raw.GetType().GetField("id",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (inner != null)
                {
                    object innerVal = inner.GetValue(raw);
                    if (innerVal is ulong iul) return iul;
                    if (innerVal is long il)   return (ulong)il;
                }
            }
        }

        // Fallback: try property
        PropertyInfo prop = peerType.GetProperty(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null)
        {
            object raw = prop.GetValue(peer);
            if (raw is ulong ul) return ul;
            if (raw is long l)   return (ulong)l;
        }

        return 0;
    }
}
