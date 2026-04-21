using System.Reflection;
using HarmonyLib;
using UnityEngine;
using VDB.Core;
using VDB.Core.DataTypes.Util;

namespace VDB.Core.Util;

/// <summary>Server: when a peer connects, bind their real Steam id to a name-only allow-list row (character name).</summary>
[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
public static class VdbJoinSteamBindPatch
{
    static void Postfix(ZNet __instance, ZNetPeer peer)
    {
        try
        {
            if (peer?.m_socket == null || __instance == null || !__instance.IsServer()) return;
            if (!ServerDB.IsInitialized) return;

            if (!Helper.TryParseSteamIdFromHostString(peer.m_socket.GetHostName(), out var steamId))
                return;

            var characterName = PeerPreferredCharacterName(peer);
            if (string.IsNullOrEmpty(characterName)) return;

            if (ServerDB.TryBindSteamIdForCharacterName(characterName, steamId))
                Debug.Log($"[DrakeVDB] Bound Steam {steamId} to allow-listed character '{characterName}'.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[DrakeVDB] JoinSteamBindPatch: {ex}");
        }
    }

    private static string? PeerPreferredCharacterName(ZNetPeer peer)
    {
        var charName = ReadPeerField(peer, "m_characterName");
        if (!string.IsNullOrEmpty(charName)) return charName;
        return ReadPeerField(peer, "m_playerName");
    }

    private static string? ReadPeerField(ZNetPeer peer, string fieldName)
    {
        var f = typeof(ZNetPeer).GetField(fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return f?.GetValue(peer)?.ToString();
    }
}
