using System.Collections.Generic;
using VDB.Core.Util;
using VDB.Core.Util.Commands;

namespace VDB.Auth.Core.Util.Commands;

public class VDBAssignCommand : VdbSubcommandBase
{
    public override string Subcommand => "assign";
    public override string Help => "Assign a player to a role. Usage: vdb assign <steamID> <roleName>";

    protected override void SafeRun(string[] args)
    {
        if (args.Length < 2)
        {
            VdbCli.Print("Usage: vdb assign <steamID> <roleName>");
            return;
        }

        VdbCli.Print(
            "[VDB] assign: role assignment API is not enabled in this build (ServerDB.AssignRole is commented out).");
    }

    public override List<string> TabOptions() => new List<string>();
}

public class VDBPlayerRemoveRoleCommand : VdbSubcommandBase
{
    public override string Subcommand => "unassign";
    public override string Help => "Remove a player from a role. Usage: vdb unassign <steamID> <roleName>";

    protected override void SafeRun(string[] args)
    {
        if (args.Length < 2)
        {
            VdbCli.Print("Usage: vdb unassign <steamID> <roleName>");
            return;
        }

        VdbCli.Print(
            "[VDB] unassign: remove-role API is not enabled in this build (ServerDB.RemovePlayerRole is commented out).");
    }

    public override List<string> TabOptions() => new List<string>();
}
