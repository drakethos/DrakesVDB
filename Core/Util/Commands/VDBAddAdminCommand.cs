using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VDB.Core.DataTypes;
using VDB.Core.DataTypes.Util;

namespace VDB.Core.Util.Commands
{
    public class VDBAddAdminCommand : VDBCommandBase
    {
        private const string ADMIN = "Admin";
        public override string Name => "vdb_addadmin";

        private static readonly FieldInfo _adminListField =
            typeof(ZNet).GetField("m_adminList", BindingFlags.NonPublic | BindingFlags.Instance);

        public override string Help =>
            "Adds a player to both VDB and Valheim admin lists.\n" +
            "Usage: vdb_addadmin <steamID> [playerName]\n" +
            "       vdb_addadmin <playerName>   (resolves Steam ID via peer list)";

        protected override void SafeRun(string[] args)
        {
            if (args.Length < 1)
            {
                Console.instance.Print(Help);
                return;
            }

            string steamID;
            string playerName;

            // If the first arg looks like a numeric Steam ID use it directly;
            // otherwise treat it as a character name and resolve via the peer list.
            if (ulong.TryParse(args[0], out ulong parsedId))
            {
                steamID   = parsedId.ToString();
                playerName = args.Length >= 2 ? string.Join(" ", args, 1, args.Length - 1).Trim() : "Unknown";
            }
            else
            {
                playerName = string.Join(" ", args).Trim();
                ulong resolvedId = Helper.getSteamId(playerName);
                if (resolvedId == 0)
                {
                    Console.instance.Print($"[VDB] Could not resolve Steam ID for '{playerName}'. Is the player online?");
                    return;
                }
                steamID = resolvedId.ToString();
            }

            // Upsert the player record in the DB
            ServerDB.AddPlayer(steamID, playerName);

            // Assign Admin role (returns false if already assigned)
            bool success = ServerDB.AssignRole(steamID, ADMIN);
            if (!success)
            {
                // Player might already have the role — still sync to ZNet adminlist
                var existing = ServerDB.GetPlayer(steamID);
                if (existing != null)
                    Console.instance.Print($"[VDB] Note: {playerName} may already have the Admin role in VDB.");
            }

            // Sync to Valheim's runtime admin list (adminlist.txt)
            if (ZNet.instance != null && _adminListField != null)
            {
                var adminList = _adminListField.GetValue(ZNet.instance);
                if (adminList != null)
                {
                    var addMethod  = adminList.GetType().GetMethod("Add",  BindingFlags.Public | BindingFlags.Instance);
                    var saveMethod = adminList.GetType().GetMethod("Save", BindingFlags.Public | BindingFlags.Instance);

                    addMethod?.Invoke(adminList, new object[] { steamID });
                    saveMethod?.Invoke(adminList, null);

                    Console.instance.Print($"[VDB] {playerName} (SteamID: {steamID}) added as admin.");
                    Debug.Log($"[DrakeVDB] Admin added: {playerName} / {steamID}");
                }
                else
                {
                    Debug.LogError("[DrakeVDB] ZNet.m_adminList is null.");
                }
            }
            else
            {
                // No ZNet (solo session or called before ZNet init) — DB-only admin is fine
                Console.instance.Print($"[VDB] {playerName} (SteamID: {steamID}) added as DB admin (ZNet not available).");
                Debug.LogWarning("[DrakeVDB] ZNet unavailable — admin added to DB only.");
            }
        }

        public override List<string> CommandOptionList() => new List<string>();
    }
}
