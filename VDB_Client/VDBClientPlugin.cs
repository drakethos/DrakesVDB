using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using VDB.Client.Core.Client;
using VDB.Client.Core.Commands;
using VDB.Client.Core.Patches;
using VDB.Client.Core.Server;

namespace VDB.Client
{
    /// <summary>
    /// BepInEx entry point for DrakesVDBClient.
    ///
    /// This mod runs on BOTH server and client:
    ///   • Server:  registers ZRoutedRpc RPCs, manages the session table,
    ///              kicks peers that don't send a handshake in time.
    ///   • Client:  sends handshake on connect, stores the local auth state,
    ///              provides the vdb_runcmd / vdb_client_status console API.
    ///
    /// VDB (DrakesVDB) is NOT a hard dependency — if it isn't installed the server
    /// will still require this mod on all clients but won't query the VDB database.
    /// </summary>
    [BepInPlugin(GUID, ModName, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class VDBClientPlugin : BaseUnityPlugin
    {
        public const string CompanyName = "DrakeMods";
        public const string ModName     = "DrakesVDBClient";
        public const string Version     = "0.0.1";
        public const string GUID        = "com." + CompanyName + "." + ModName;

        private readonly Harmony _harmony = new Harmony(GUID);

        // -----------------------------------------------------------------------
        // Config
        // -----------------------------------------------------------------------
        private ConfigEntry<bool>  _requireClientConfig;
        private ConfigEntry<float> _handshakeTimeoutConfig;

        /// <summary>
        /// When true (default), players without this mod are kicked after the
        /// handshake timeout.  Set false to make the client optional (e.g. during
        /// initial rollout).
        /// </summary>
        public static bool RequireClient { get; private set; } = true;

        private void Awake()
        {
            try
            {
                Logger.LogInfo($"[VDBClient] Initialising {ModName} v{Version}...");

                // Bind config
                _requireClientConfig = Config.Bind(
                    "General", "RequireClient", true,
                    "When true, players without VDBClient installed will be kicked after the handshake timeout.");

                _handshakeTimeoutConfig = Config.Bind(
                    "General", "HandshakeTimeoutSeconds", 15f,
                    "Seconds the server waits for a client handshake before kicking the peer.");

                RequireClient                          = _requireClientConfig.Value;
                VDBRpcServer.HandshakeTimeoutSeconds   = _handshakeTimeoutConfig.Value;

                // Apply Harmony patches
                _harmony.PatchAll(typeof(ZNetPeerPatches));
                Logger.LogInfo("[VDBClient] Harmony patches applied.");

                // Register console commands (available on both client and server)
                RegisterCommands();

                // Start the server-side timeout ticker
                StartCoroutine(HandshakeTimeoutTicker());

                GameObject.DontDestroyOnLoad(this);
                Logger.LogInfo("[VDBClient] Ready.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[VDBClient] Initialisation failed: {ex}");
            }
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
            StopAllCoroutines();
        }

        // -----------------------------------------------------------------------
        // Commands
        // -----------------------------------------------------------------------

        private void RegisterCommands()
        {
            CommandManager.Instance.AddConsoleCommand(new VDBClientStatusCommand());
            CommandManager.Instance.AddConsoleCommand(new VDBRunCmdCommand());
            CommandManager.Instance.AddConsoleCommand(new VDBKickCommand());
            Logger.LogInfo("[VDBClient] Console commands registered.");
        }

        // -----------------------------------------------------------------------
        // Handshake timeout ticker (server side only, harmless on client)
        // -----------------------------------------------------------------------

        private IEnumerator HandshakeTimeoutTicker()
        {
            // Poll every 3 seconds — cheap and sufficient
            var wait = new WaitForSeconds(3f);
            while (true)
            {
                yield return wait;

                if (ZNet.instance != null && ZNet.instance.IsServer() && RequireClient)
                    VDBRpcServer.TickTimeouts();
            }
        }
    }
}
