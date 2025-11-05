using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VDB.Core.DataTypes;
using VDB.Core.DataTypes.Util;

namespace VDB.Core.Util.Commands
{
    public class VDBAddPlayerCommand : VDBCommandBase
    {
        public override string Name => "vdb_addplayer";
        public override string Help =>
            "Adds a player to both VDB and Valheim white lists. Usage: vdb_addplayer <playername, steamID>";

        protected override void SafeRun(string[] args)
        {
            if (args.Length < 1)
            {
                Console.instance.Print("Usage: vdb_addplayer<name> <steamName>");
                return;
            }

            string steamID = args[1];
            string playerName = args[0];
            if (String.IsNullOrEmpty(steamID))
            {
                steamID = Helper.getSteamId(playerName).ToString();
            }
            
            // Ensure player exists in DB
            var player = ServerDB.AddPlayer(steamID, playerName);

            // Assign to Admin group
            if (player != null)
            { 
                Console.instance.Print($"{playerName} Player successfully added to db.");
                Debug.Log($"[DrakeVDB] Player added to db : {steamID}");
            }
            else
            {
                Console.instance.Print($"[VDB] Failed: Player {steamID} may already be an Admin.");
            }
        }

        public override List<string> CommandOptionList() => new List<string>();
    }
}