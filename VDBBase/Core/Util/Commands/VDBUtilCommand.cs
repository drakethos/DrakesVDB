using System;
using Jotunn;
using UnityEngine;
using VDB.Core.DataTypes;
using VDB.Core.DataTypes.Util;
using VDB.Core.Util;

namespace VDB.Core.Util.Commands;

public class VDBUtilCommand : VdbSubcommandBase
{
    public override string Subcommand => "steamid";
    public override bool RequiresAdmin => false;

    public override string Help =>
        "Show Steam64 for a character (or yourself). Uses the live server peer list (Steam_7656… host ids). Usage: vdb steamid [playerName]";

    protected override void SafeRun(string[] args)
    {
        string targetName;
        ulong? resolved;

        if (args.Length < 1)
        {
            var local = global::Player.m_localPlayer;
            if (local == null)
            {
                VdbCli.Print("[VDB] No local player.");
                return;
            }

            targetName = local.GetPlayerName();
            resolved = Helper.getSteamId(targetName);
        }
        else
        {
            targetName = string.Join(" ", args).Trim();
            resolved = Helper.getSteamId(targetName);
        }

        if (resolved.HasValue && resolved.Value != 0)
            VdbCli.Print($"[DrakeVDB] SteamID of {targetName} is {resolved.Value}");
        else
            VdbCli.Print($"[DrakeVDB] Could not resolve Steam id for '{targetName}' (must be online on this server, or use Steamworks locally).");
    }
}

public class VDBWhoAmICommand : VdbSubcommandBase
{
    public override string Subcommand => "whoami";
    public override string Help => "Show your character name, IDs, and admin hints. Usage: vdb whoami";
    public override bool RequiresAdmin => false;

    protected override void SafeRun(string[] args)
    {
        var player = global::Player.m_localPlayer;
        if (player == null)
        {
            VdbCli.Print("[VDB] No local player found.");
            return;
        }

        string playerName = player.GetPlayerName();
        long localPlayerId = player.GetPlayerID();
        VdbCli.Print($"[VDB] Name: {playerName}");
        VdbCli.Print($"[VDB] Local Player ID (GetPlayerID): {localPlayerId}");

        string? steamIdStr = Helper.TryGetSteamIdViaSteamworks();
        if (!string.IsNullOrEmpty(steamIdStr))
            steamIdStr = Helper.TryParseSteamIdFromHostStringNullable(steamIdStr)?.ToString() ?? steamIdStr;

        if (string.IsNullOrEmpty(steamIdStr))
        {
            var fromPeers = Helper.TryGetSteamIdFromZNetPeers(playerName);
            if (fromPeers.HasValue)
                steamIdStr = fromPeers.Value.ToString();
        }

        if (string.IsNullOrEmpty(steamIdStr))
        {
            var localRaw = Helper.GetLocalSteamID();
            steamIdStr = Helper.TryParseSteamIdFromHostStringNullable(localRaw)?.ToString();
        }

        if (!string.IsNullOrEmpty(steamIdStr))
            VdbCli.Print($"[VDB] SteamID (resolved): {steamIdStr}");
        else if (VDBSession.SteamID != 0)
            VdbCli.Print($"[VDB] SteamID (session): {VDBSession.SteamID}");
        else
            VdbCli.Print("[VDB] SteamID: NOT FOUND (offline / singleplayer / not in peer list yet)");

        if (!string.IsNullOrEmpty(steamIdStr) && ulong.TryParse(steamIdStr, out var steamId))
        {
            bool nativeAdmin = ZNet.instance?.IsAdmin(steamId.ToString()) ?? false;
            VdbCli.Print($"[VDB] Native ZNet.IsAdmin: {nativeAdmin}");
        }
        else
        {
            if (ZNet.instance?.IsServer() == false && ZNet.instance.IsLocalInstance())
                VdbCli.Print("[VDB] You are host/local server — treated as admin for local testing.");
        }
    }
}
