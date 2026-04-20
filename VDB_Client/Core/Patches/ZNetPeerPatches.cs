using HarmonyLib;
using UnityEngine;
using VDB.Client.Core.Client;
using VDB.Client.Core.Server;

namespace VDB.Client.Core.Patches
{
    /// <summary>
    /// Harmony patches on ZNet to hook peer lifecycle events.
    ///
    ///  • Server patches: fire OnPeerConnected / OnPeerDisconnected in VDBRpcServer.
    ///  • Client patches: trigger handshake send once the local player spawns.
    ///  • ZRoutedRpc.SetupRouting patch: register RPCs as soon as ZRoutedRpc is ready
    ///    (fires earlier than Awake/Start on the server).
    /// </summary>
    public static class ZNetPeerPatches
    {
        // -----------------------------------------------------------------------
        // ZRoutedRpc ready — register RPCs immediately
        // -----------------------------------------------------------------------

        [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.AddPeer))]
        public static class Patch_ZRoutedRpc_AddPeer
        {
            [HarmonyPostfix]
            public static void Postfix(ZNetPeer peer)
            {
                if (ZNet.instance == null) return;

                // Register server RPCs once (idempotent — Valheim ignores duplicate Register calls)
                if (ZNet.instance.IsServer())
                {
                    VDBRpcServer.Register();
                    VDBRpcServer.OnPeerConnected(peer.m_uid);
                    Debug.Log($"[VDBClient] Peer connected (server): {peer.m_uid} / {peer.m_playerName}");
                }
            }
        }

        // -----------------------------------------------------------------------
        // Peer disconnected (server side)
        // -----------------------------------------------------------------------

        [HarmonyPatch(typeof(ZNet), "Disconnect")]
        public static class Patch_ZNet_Disconnect
        {
            [HarmonyPrefix]
            public static void Prefix(ZNetPeer peer)
            {
                if (ZNet.instance == null || !ZNet.instance.IsServer()) return;
                if (peer != null)
                    VDBRpcServer.OnPeerDisconnected(peer.m_uid);
            }
        }

        // -----------------------------------------------------------------------
        // Local player spawned — send handshake (client side)
        // -----------------------------------------------------------------------

        [HarmonyPatch(typeof(Player), "OnSpawned")]
        public static class Patch_Player_OnSpawned
        {
            [HarmonyPostfix]
            public static void Postfix(Player __instance)
            {
                // Only fire for the local player, and only when connected to a server
                if (__instance != Player.m_localPlayer) return;
                if (ZNet.instance == null)              return;

                // Register client-side RPCs now (ZRoutedRpc is definitely ready)
                VDBRpcClient.Register();
                VDBRpcClient.SendHandshake();
            }
        }

        // -----------------------------------------------------------------------
        // Game disconnected / quit — clean up client state
        // -----------------------------------------------------------------------

        [HarmonyPatch(typeof(ZNet), "OnDestroy")]
        public static class Patch_ZNet_OnDestroy
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                VDBRpcClient.Reset();
                VDBRpcServer.Unregister();
                Debug.Log("[VDBClient] ZNet destroyed — sessions cleared.");
            }
        }
    }
}
