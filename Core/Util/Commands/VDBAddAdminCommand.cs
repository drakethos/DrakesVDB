using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VDB.Core.DataTypes;

namespace VDB.Core.Util.Commands
{
    public class VDBAddAdminCommand : VDBCommandBase
    {
        private const string ADMIN = "Admin";
        public override string Name => "vdb_addadmin";
        private static FieldInfo _adminListField = typeof(ZNet).GetField("m_adminList", BindingFlags.NonPublic | BindingFlags.Instance);

        public override string Help =>
            "Adds a player to both VDB and Valheim admin lists. Usage: vdb_addadmin <steamID>";

        protected override void SafeRun(string[] args)
        {
            if (args.Length < 1)
            {
                Console.instance.Print("Usage: vdb_addadmin <steamID> <name>");
                return;
            }

            string steamID = args[0];
            string playerName = (String.IsNullOrEmpty(args[1]) ? args[1] : "Unknown");
            // Ensure player exists in DB
            var player = ServerDB.AddPlayer(steamID, playerName);

            // Assign to Admin group
            bool success = ServerDB.AssignRole(steamID, ADMIN);
            if (success)
            {
                if (ZNet.instance == null || _adminListField == null)
                {
                    Debug.LogError("[DrakeVDB] Cannot access ZNet.m_adminList.");
                    return;
                }

                // Get the admin list
                var adminList = _adminListField.GetValue(ZNet.instance);
                if (adminList == null)
                {
                    Debug.LogError("[DrakeVDB] Admin list is null!");
                    return;
                }

                // m_adminList is actually a ZRpcStringList (or similar internal class)
                // Use reflection to call Add method
                var addMethod = adminList.GetType().GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
                var saveMethod = adminList.GetType().GetMethod("Save", BindingFlags.Public | BindingFlags.Instance);

                if (addMethod != null) addMethod.Invoke(adminList, new object[] { steamID.ToString() });
                if (saveMethod != null) saveMethod.Invoke(adminList, null);

                Debug.Log($"[DrakeVDB] Added admin: {steamID}");
            }
            else
            {
                Console.instance.Print($"[VDB] Failed: Player {steamID} may already be an Admin.");
            }
        }

        public override List<string> CommandOptionList() => new List<string>();
    }
}