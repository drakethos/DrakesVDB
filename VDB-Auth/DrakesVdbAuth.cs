using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Jotunn.Managers;
using Jotunn.Utils;
using VDB;
using VDB.Core;
using VDB.Core.DataTypes;
using VDB.Core.DataTypes.Util;
using VDB.Core.Util.Commands;

namespace VDB.Auth;

[BepInPlugin(GUID, ModName, Version)]
[BepInDependency(Jotunn.Main.ModGuid)]
[BepInDependency(DrakeVDB.GUID)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public class DrakesVdbAuth : BaseUnityPlugin
{
    public const string CompanyName = "DrakeMods";
    public const string ModName = "DrakeVDB-Auth";
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
            Logger.LogInfo("DrakeVDB-Auth initializing...");

            // Bind config (same key as base; base mod opens DB first via BepInDependency order)
            dbNameConfig = Config.Bind("General", "DBName", "VDB.db", "Database file name for DrakeVDB");
            if (!ServerDB.IsInitialized)
                ServerDB.InitializeDB(dbNameConfig.Value);
            Logger.LogInfo(ServerDB.IsInitialized ? $"Using VDB database: {dbNameConfig.Value}" : "VDB database not initialized");

            GameObject.DontDestroyOnLoad(this);
            RegisterConsoleCommands();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DrakeVDB-Auth] Initialization failed: {ex}");
        }
    }

    private void RegisterConsoleCommands()
    {
        VDBCommandRegistrar.RegisterCommandsInAssembly(typeof(DrakesVdbAuth).Assembly);
    }
}