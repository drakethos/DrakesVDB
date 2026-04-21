using System;
using System.Linq;
using VDB.Core;
using VDB.Core.DataTypes.Util;

using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;


namespace VDB.Auth.Patch;

public class AuthPatch
{

[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
public static class VDB_PlayerBootstrap_Patch
{
    static void Postfix(ZNet __instance, ZNetPeer peer)
    {
        try
        {
            if (peer?.m_socket == null) return;
            
            string hostName = peer.m_socket.GetHostName();

            if (Helper.TryParseSteamIdFromHostString(hostName, out var steamId))
            {
                string name = peer.m_playerName ?? "Unknown";

                // Pull roles from DB
             //   var roles = ServerDB.GetRoles(steamId.ToString()).ToList();

                // Set session info
             //   VDBSession.Initialize(steamId, name, roles);
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"[VDB] Failed to bootstrap player: {ex}");
        }
    }
}

}