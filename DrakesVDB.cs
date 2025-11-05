using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Jotunn.Managers;
using Jotunn.Utils;
using VDB.Core.DataTypes;
using VDB.Core.Util.commands;
using VDB.Core.Util.Commands;

namespace VDB
{
    [BepInPlugin(GUID, ModName, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class DrakeVDB : BaseUnityPlugin
    {
        public const string CompanyName = "DrakeMods";
        public const string ModName = "DrakeVDB";
        public const string Version = "0.0.1";
        public const string GUID = "com." + CompanyName + "." + ModName;
        public ConfigEntry<string> PublicPiecesConfig; // Config entry for public pieces list
        public static readonly char[] ConfigSeparator = { ',' }; // Separator for config entries

        private readonly Harmony harmony = new Harmony("drakesmod.Mod");

        private ConfigEntry<string> dbNameConfig;

        private void Awake()
        {
            try
            {
                Logger.LogInfo("DrakeVDB initializing...");

                // Bind config
                dbNameConfig = Config.Bind("General", "DBName", "VDB.db", "Database file name for DrakeVDB");

                // Initialize DB
                ServerDB.InitializeDB(dbNameConfig.Value);
                Logger.LogInfo($"Database initialized: {dbNameConfig.Value}");

                // Defer command registration until ZNet is ready
                GameObject.DontDestroyOnLoad(this);
                RegisterConsoleCommands();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DrakeVDB] Initialization failed: {ex}");
            }
        }

        private void RegisterConsoleCommands()
        {
            CommandManager.Instance.AddConsoleCommand(new VDBInitCommand());
            CommandManager.Instance.AddConsoleCommand(new VDBAssignCommand());
            CommandManager.Instance.AddConsoleCommand(new VDBAddAdminCommand());
            CommandManager.Instance.AddConsoleCommand(new VDBAddPlayerCommand());
            CommandManager.Instance.AddConsoleCommand(new VDBGetSteamID());
            Jotunn.Logger.LogInfo("[VDB] Console Commands registered!");
        }
    }
}