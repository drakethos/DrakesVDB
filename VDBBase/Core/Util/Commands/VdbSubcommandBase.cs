using System;
using System.Collections.Generic;
using System.Reflection;
using Jotunn;
using UnityEngine;
using VDB.Core.Util;

namespace VDB.Core.Util.Commands;

/// <summary>Single subcommand for the unified <c>vdb</c> console command.</summary>
public abstract class VdbSubcommandBase
{
    /// <summary>First token after <c>vdb</c> (e.g. <c>addplayer</c>).</summary>
    public abstract string Subcommand { get; }

    public virtual bool RequiresAdmin => true;

    public abstract string Help { get; }

    /// <summary>Optional tab-completion strings for arguments (used by router when supported).</summary>
    public virtual List<string> TabOptions() => new List<string>();

    public bool TryRun(string[] args)
    {
        var player = Player.m_localPlayer;
        if (RequiresAdmin && !VdbRuntime.TreatAsAdminForCommands && !IsAdmin(player))
        {
            VdbCli.Print("[VDB] You must be an admin to run this command.");
            return false;
        }

        SafeRun(args);
        return true;
    }

    protected abstract void SafeRun(string[] args);

    private static bool IsAdmin(Player player)
    {
        if (player == null) return false;

        long steamID = player.GetPlayerID();

        if (ZNet.instance?.IsServer() ?? false)
        {
            Debug.Log($"{player.GetPlayerName()} is local host -> auto-admin");
            return true;
        }

        if (ZNet.instance?.IsAdmin(steamID) ?? false) return true;

        var field = typeof(ZNet).GetField("m_adminList", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Debug.LogWarning("Could not find m_adminList field!");
        }
        else
        {
            var adminList = field.GetValue(ZNet.instance) as HashSet<ulong>;
            if (adminList == null)
                Debug.LogWarning("m_adminList is null or wrong type!");
            else if (adminList.Contains((ulong)steamID))
                return true;
        }

        return false;
    }
}
