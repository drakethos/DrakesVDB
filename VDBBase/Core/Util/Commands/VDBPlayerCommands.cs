using System;
using UnityEngine;
using VDB.Core.DataTypes;
using VDB.Core.DataTypes.Util;
using VDB.Core.Util;

namespace VDB.Core.Util.Commands
{
    public class VDBAddPlayerCommand : VdbSubcommandBase
    {
        public override string Subcommand => "addplayer";
        public override string Help =>
            "Allow-list a character name (optional Steam id). Usage: vdb addplayer <name> [steamId] — if online without steamId, id is resolved from the server player list (Steam_7656…).";

        protected override void SafeRun(string[] args)
        {
            if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
            {
                VdbCli.Print("Usage: vdb addplayer <name> [steamId]");
                return;
            }

            string playerName = args[0].Trim();
            string? steamNumeric = null;

            if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
            {
                var raw = args[1].Trim();
                if (!Helper.TryParseSteamIdFromHostString(raw, out var parsed))
                {
                    VdbCli.Print($"[VDB] Invalid Steam id: '{raw}' (use 7656119… or Steam_7656119…).");
                    return;
                }

                steamNumeric = parsed.ToString();
            }
            else
            {
                var resolved = Helper.getSteamId(playerName);
                if (resolved.HasValue)
                    steamNumeric = resolved.Value.ToString();
            }

            ServerDB.EnsurePlayerByCharacterName(playerName, steamNumeric);

            if (!string.IsNullOrEmpty(steamNumeric))
            {
                VdbCli.Print(
                    $"[VDB] {playerName} allow-listed (Steam {steamNumeric}). Binds automatically on join if id was omitted but name matched.");
                Debug.Log($"[DrakeVDB] addplayer: {playerName} steam={steamNumeric}");
            }
            else
            {
                VdbCli.Print(
                    $"[VDB] {playerName} allow-listed by name only (no Steam id yet). When they join online, Steam id is taken from the server list and stored.");
            }
        }
    }

    public class VDBRemovePlayerCommand : VdbSubcommandBase
    {
        public override string Subcommand => "removeplayer";
        public override string Help => "Remove a player from VDB by character name. Usage: vdb removeplayer <name>";

        protected override void SafeRun(string[] args)
        {
            if (args.Length < 1)
            {
                VdbCli.Print("Usage: vdb removeplayer <name>");
                return;
            }

            string playerName = args[0];
            var removed = ServerDB.RemovePlayer(playerName);

            if (removed)
            {
                VdbCli.Print($"{playerName} successfully removed from db.");
                Debug.Log($"[DrakeVDB] Player removed from db: {playerName}");
            }
            else
            {
                VdbCli.Print($"[VDB] Failed: player \"{playerName}\" was not in the database.");
            }
        }
    }
}
