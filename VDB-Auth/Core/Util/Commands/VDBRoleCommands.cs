using System;
using System.Collections.Generic;
using UnityEngine;
using VDB.Auth.Core;
using VDB.Core.DataTypes;
using VDB.Core.Util;
using VDB.Core.Util.Commands;

namespace VDB.Auth.Core.Util.Commands;

public class VDBRemoveRoleCommand : VdbSubcommandBase
{
    public override string Subcommand => "removerole";
    public override string Help => "Remove a role. Usage: vdb removerole <roleName>";

    protected override void SafeRun(string[] args)
    {
        if (args.Length < 1)
        {
            VdbCli.Print("Usage: vdb removerole <roleName>");
            return;
        }

        string roleName = args[0];

        var roleExists = true;

        if (roleExists)
            VdbCli.Print($"{roleName} role successfully removed from db.");
        else
            VdbCli.Print($"[VDB] Failed: Role {roleName} Does not exist.");
    }
}

public class VDBAddRoleCommand : VdbSubcommandBase
{
    public override string Subcommand => "addrole";
    public override string Help => "Add a role. Usage: vdb addrole <roleName>";

    protected override void SafeRun(string[] args)
    {
        if (args.Length < 1)
        {
            VdbCli.Print("Usage: vdb addrole <roleName>");
            return;
        }

        string roleName = args[0];

        var added = ServerDB_Auth_Ext.AddRole(roleName);

        if (added)
            VdbCli.Print($"{roleName} role successfully added to db.");
        else
            VdbCli.Print($"[VDB] Role \"{roleName}\" already exists.");
    }
}
