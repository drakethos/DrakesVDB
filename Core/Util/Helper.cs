using System;
using System.Reflection;
using UnityEngine;

namespace VDB.Core.DataTypes.Util;

public static class Helper
{
    public static ulong getSteamId(String playerName)
    {
        if (ZNet.instance == null)
            return 0;

        // m_adminList example shows ZNet stores players in ZNet.m_peers (private)
        FieldInfo peersField = typeof(ZNet).GetField("m_peers", BindingFlags.NonPublic | BindingFlags.Instance);
        if (peersField == null) return 0;

        var peers = (System.Collections.IList)peersField.GetValue(ZNet.instance);
        if (peers == null)
        {
            Debug.LogError("No Peers Found");
            return 0;
        }

        foreach (var peer in peers)
        {
            // Each peer is a ZNetPeer
            var nameProp = peer.GetType().GetProperty("m_characterName", BindingFlags.Public | BindingFlags.Instance);
            var idProp = peer.GetType().GetProperty("m_uid", BindingFlags.Public | BindingFlags.Instance);

            if (nameProp == null || idProp == null) continue;

            string name = nameProp.GetValue(peer)?.ToString();
            Debug.Log($"Comparing {name} with {playerName}: ");
            if (name != null && name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
            {
                return (ulong)idProp.GetValue(peer);
            }
        }
        return 0; // not fou
    }
}