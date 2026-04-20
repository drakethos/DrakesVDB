using System.Reflection;
using HarmonyLib;
using UnityEngine;
using VDC.AccessExtension.Core.DataTypes;

namespace VDC.AccessExtension.Core.Patches
{
    /// <summary>
    /// Harmony patches that intercept ZNet's three access-control checks so VDC
    /// lists are consulted first.  Each patch uses a Prefix that can short-circuit
    /// the original method, then a Postfix to optionally sync back to the native list.
    ///
    /// Valheim method signatures (publicised):
    ///   bool ZNet.IsAllowed(string hostName)   — whitelist check
    ///   bool ZNet.IsBlocked(string hostName)   — banlist check
    ///   bool ZNet.IsAdmin(string hostName)     — adminlist check
    ///
    /// "hostName" is the string representation of the peer's ZDOID / SteamID.
    /// </summary>
    public static class ZNetPatches
    {
        // -----------------------------------------------------------------------
        // Whitelist — ZNet.IsAllowed
        // -----------------------------------------------------------------------

        [HarmonyPatch(typeof(ZNet), "IsAllowed")]
        public static class Patch_IsAllowed
        {
            /// <summary>
            /// If VDC whitelist is enabled, check it first.
            /// • Found + active entry  → grant access, skip native check.
            /// • Not in VDC list       → fall through to native check.
            /// </summary>
            [HarmonyPrefix]
            public static bool Prefix(string hostName, ref bool __result)
            {
                var cfg = AccessDB.GetListConfig(AccessListType.Whitelist);
                if (cfg == null || !cfg.Enabled) return true; // run original

                if (AccessDB.IsOnList(hostName, AccessListType.Whitelist))
                {
                    Debug.Log($"[VDCAccess] {hostName} allowed via VDC whitelist.");
                    AccessDB.LogDecision(hostName, null, AccessListType.Whitelist, true, "VDCWhitelist");
                    __result = true;
                    return false; // skip original
                }

                // Not in VDC list — let the original (native file) decide
                return true;
            }
        }

        // -----------------------------------------------------------------------
        // Banlist — ZNet.IsBlocked
        // -----------------------------------------------------------------------

        [HarmonyPatch(typeof(ZNet), "IsBlocked")]
        public static class Patch_IsBlocked
        {
            /// <summary>
            /// If VDC banlist is enabled, check it first.
            /// • Found + active ban  → block immediately, skip native check.
            /// • Not in VDC list    → fall through to native check.
            /// </summary>
            [HarmonyPrefix]
            public static bool Prefix(string hostName, ref bool __result)
            {
                var cfg = AccessDB.GetListConfig(AccessListType.Banlist);
                if (cfg == null || !cfg.Enabled) return true;

                if (AccessDB.IsOnList(hostName, AccessListType.Banlist))
                {
                    Debug.Log($"[VDCAccess] {hostName} blocked via VDC banlist.");
                    AccessDB.LogDecision(hostName, null, AccessListType.Banlist, false, "VDCBanlist");
                    __result = true;  // IsBlocked returning true = player is banned
                    return false;
                }

                return true;
            }
        }

        // -----------------------------------------------------------------------
        // Adminlist — ZNet.IsAdmin
        // -----------------------------------------------------------------------

        [HarmonyPatch(typeof(ZNet), "IsAdmin")]
        public static class Patch_IsAdmin
        {
            /// <summary>
            /// If VDC adminlist is enabled, check it first.
            /// • Found + active admin entry → grant admin, skip native check.
            /// • Not in VDC list            → fall through to native adminlist.txt.
            /// </summary>
            [HarmonyPrefix]
            public static bool Prefix(string hostName, ref bool __result)
            {
                var cfg = AccessDB.GetListConfig(AccessListType.Adminlist);
                if (cfg == null || !cfg.Enabled) return true;

                if (AccessDB.IsOnList(hostName, AccessListType.Adminlist))
                {
                    Debug.Log($"[VDCAccess] {hostName} is admin via VDC adminlist.");
                    AccessDB.LogDecision(hostName, null, AccessListType.Adminlist, true, "VDCAdminlist");
                    __result = true;
                    return false;
                }

                return true;
            }
        }

        // -----------------------------------------------------------------------
        // Native list sync helper
        // -----------------------------------------------------------------------

        private static readonly FieldInfo _permittedListField =
            typeof(ZNet).GetField("m_permittedList", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _bannedListField =
            typeof(ZNet).GetField("m_bannedList",    BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _adminListField =
            typeof(ZNet).GetField("m_adminList",     BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Adds a Steam ID to the appropriate native Valheim list and saves it to disk,
        /// if ZNet is available.  Used by commands when SyncToNative is enabled.
        /// </summary>
        public static void SyncAddToNative(string steamID, AccessListType listType)
        {
            if (ZNet.instance == null) return;

            FieldInfo field = listType switch
            {
                AccessListType.Whitelist  => _permittedListField,
                AccessListType.Banlist    => _bannedListField,
                AccessListType.Adminlist  => _adminListField,
                _                         => null
            };

            if (field == null) return;
            InvokeListMethod(field, "Add",  steamID);
            InvokeListMethod(field, "Save", null);
        }

        /// <summary>
        /// Removes a Steam ID from the appropriate native Valheim list and saves.
        /// </summary>
        public static void SyncRemoveFromNative(string steamID, AccessListType listType)
        {
            if (ZNet.instance == null) return;

            FieldInfo field = listType switch
            {
                AccessListType.Whitelist  => _permittedListField,
                AccessListType.Banlist    => _bannedListField,
                AccessListType.Adminlist  => _adminListField,
                _                         => null
            };

            if (field == null) return;
            InvokeListMethod(field, "Remove", steamID);
            InvokeListMethod(field, "Save",   null);
        }

        private static void InvokeListMethod(FieldInfo listField, string methodName, string arg)
        {
            var list = listField.GetValue(ZNet.instance);
            if (list == null) return;

            var method = list.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method == null) return;

            if (arg != null)
                method.Invoke(list, new object[] { arg });
            else
                method.Invoke(list, null);
        }
    }
}
