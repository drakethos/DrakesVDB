using System;
using System.Reflection;
using UnityEngine;
using VDB.Core.DataTypes.Util;

namespace VDB.Core.Util.Commands;

public class VDBGetSteamID : VDBCommandBase
{
    public override string Name => "getSteamID";
    public override bool RequiresAdmin => false;

    public override string Help =>
        "Give a character name reports the Steam ID. Usage: getSteamID <playerName>";

    protected override void SafeRun(string[] args)
    {
        ulong steamID;
        string targetName;
        if (args.Length < 1)
        {
            steamID = (ulong)Player.m_localPlayer.GetPlayerID();
            targetName = Player.m_localPlayer.GetPlayerName();
        }
        else
        {
            targetName = string.Join(" ", args).Trim();
            steamID = Helper.getSteamId(targetName);
        }

        if (steamID != 0)
            Console.instance.Print($"[DrakeVDB] SteamID of {targetName} is {steamID}");
        else
            Console.instance.Print($"[DrakeVDB] Player '{targetName}' not found.");
    }
}