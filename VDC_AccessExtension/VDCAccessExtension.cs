using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using VDC.AccessExtension.Core;
using VDC.AccessExtension.Core.Patches;
using VDC.AccessExtension.Core.Util.Commands;

namespace VDC.AccessExtension
{
    [BepInPlugin(GUID, ModName, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("com.DrakeMods.DrakeVDB")]   // requires DrakesVDB to be loaded first
    public class VDCAccessExtension : BaseUnityPlugin
    {
        public const string CompanyName = "DrakeMods";
        public const string ModName     = "DrakesVDCAccess";
        public const string Version     = "0.0.1";
        public const string GUID        = "com." + CompanyName + "." + ModName;

        private readonly Harmony _harmony = new Harmony(GUID);

        private ConfigEntry<string> _dbNameConfig;
        private ConfigEntry<bool>   _whitelistEnabledConfig;
        private ConfigEntry<bool>   _banlistEnabledConfig;
        private ConfigEntry<bool>   _adminlistEnabledConfig;
        private ConfigEntry<bool>   _syncToNativeConfig;

        private void Awake()
        {
            try
            {
                Logger.LogInfo("[VDCAccess] Initialising VDC Access Extension...");

                // Bind config
                _dbNameConfig = Config.Bind("General", "DBName", "VDCAccess.db",
                    "Database file name for VDC Access Extension.");

                _whitelistEnabledConfig = Config.Bind("Lists", "WhitelistEnabled", true,
                    "Check VDC whitelist before Valheim's native permittedlist.");
                _banlistEnabledConfig = Config.Bind("Lists", "BanlistEnabled", true,
                    "Check VDC banlist before Valheim's native bannedlist.");
                _adminlistEnabledConfig = Config.Bind("Lists", "AdminlistEnabled", true,
                    "Check VDC adminlist before Valheim's native adminlist.");

                _syncToNativeConfig = Config.Bind("Lists", "SyncToNative", true,
                    "When true, adding/removing entries via commands also updates the native Valheim lists.");

                // Initialise DB
                AccessDB.Initialize(_dbNameConfig.Value);

                // Apply config values to DB (honours any DB overrides already saved)
                ApplyConfigToDB();

                // Apply Harmony patches
                _harmony.PatchAll(typeof(ZNetPatches));
                Logger.LogInfo("[VDCAccess] Harmony patches applied.");

                // Register console commands
                RegisterCommands();

                GameObject.DontDestroyOnLoad(this);
                Logger.LogInfo("[VDCAccess] Ready.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[VDCAccess] Initialisation failed: {ex}");
            }
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }

        private void ApplyConfigToDB()
        {
            AccessDB.SetListConfig(Core.DataTypes.AccessListType.Whitelist,
                _whitelistEnabledConfig.Value, _syncToNativeConfig.Value);
            AccessDB.SetListConfig(Core.DataTypes.AccessListType.Banlist,
                _banlistEnabledConfig.Value,   _syncToNativeConfig.Value);
            AccessDB.SetListConfig(Core.DataTypes.AccessListType.Adminlist,
                _adminlistEnabledConfig.Value, _syncToNativeConfig.Value);
        }

        private void RegisterCommands()
        {
            CommandManager.Instance.AddConsoleCommand(new VDCAllowCommand());
            CommandManager.Instance.AddConsoleCommand(new VDCBanCommand());
            CommandManager.Instance.AddConsoleCommand(new VDCAdminCommand());
            CommandManager.Instance.AddConsoleCommand(new VDCRemoveAccessCommand());
            CommandManager.Instance.AddConsoleCommand(new VDCListAccessCommand());
            Logger.LogInfo("[VDCAccess] Console commands registered.");
        }
    }
}
