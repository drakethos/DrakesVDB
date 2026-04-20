using System;
using System.Collections.Generic;
using UnityEngine;
using VDB.Core.DataTypes;
using VDB.Core.DataTypes.Util;

namespace VDB.Core.Util.Commands
{
    public class VDBAddPlayerCommand : VDBCommandBase
    {
        public override string Name => "vdb_addplayer";
        public override string Help =>
            "Adds a player to VDB.\n" +
            "Usage: vdb_addplayer <playerName> [steamID]\n" +
            "       If steamID is omitted, it is resolved from the active peer list.";

        protected override void SafeRun(string[] args)
        {
            if (args.Length < 1)
            {
                Console.instance.Print(Help);
                return;
            }

            string playerName = args[0].Trim();
            string steamID;

            if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
            {
                steamID = args[1].Trim();
            }
            else
            {
                // Auto-resolve from the connected peer list
                ulong resolved = Helper.getSteamId(playerName);
                if (resolved == 0)
                {
                    Console.instance.Print($"[VDB] Could not resolve Steam ID for '{playerName}'. " +
                                           "Provide it explicitly: vdb_addplayer <name> <steamID>");
                    return;
                }
                steamID = resolved.ToString();
            }

            var player = ServerDB.AddPlayer(steamID, playerName);
            if (player != null)
            {
                Console.instance.Print($"[VDB] Player '{playerName}' (SteamID: {steamID}) added to DB.");
                Debug.Log($"[DrakeVDB] Player added: {playerName} / {steamID}");
            }
            else
            {
                Console.instance.Print($"[VDB] Player '{playerName}' (SteamID: {steamID}) already exists in DB.");
            }
        }

        public override List<string> CommandOptionList() => new List<string>();
    }
}
