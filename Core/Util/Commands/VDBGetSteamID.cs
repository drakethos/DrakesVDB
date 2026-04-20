using System;
using UnityEngine;
using VDB.Core.DataTypes.Util;

namespace VDB.Core.Util.Commands;

public class VDBGetSteamID : VDBCommandBase
{
    public override string Name => "getSteamID";
    public override bool RequiresAdmin => false;

    public override string Help =>
        "Reports the Steam ID (and admin status) for a player.\n" +
        "Usage: getSteamID [playerName]  (omit name to query yourself)";

    protected override void SafeRun(string[] args)
    {
        ulong steamID;
        string targetName;

        if (args.Length < 1)
        {
            if (Player.m_localPlayer == null)
            {
                Console.instance.Print("[DrakeVDB] No local player found.");
                return;
            }
            steamID    = (ulong)Player.m_localPlayer.GetPlayerID();
            targetName = Player.m_localPlayer.GetPlayerName();
        }
        else
        {
            targetName = string.Join(" ", args).Trim();
            steamID    = Helper.getSteamId(targetName);
        }

        if (steamID == 0)
        {
            Console.instance.Print($"[DrakeVDB] Player '{targetName}' not found or Steam ID unavailable.");
            return;
        }

        // Also report admin type when querying self
        string adminInfo = "";
        if (args.Length < 1 && Player.m_localPlayer != null)
        {
            AdminType adminType = GetAdminType(Player.m_localPlayer);
            adminInfo = $"  |  Admin: {adminType}";
        }

        Console.instance.Print($"[DrakeVDB] SteamID of '{targetName}': {steamID}{adminInfo}");
        Debug.Log($"[DrakeVDB] SteamID resolved: {targetName} -> {steamID}{adminInfo}");
    }
}
