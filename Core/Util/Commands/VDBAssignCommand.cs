using System;
using System.Collections.Generic;
using Jotunn.Entities;
using VDB.Core.DataTypes;
using VDB.Core.Util.Commands;

namespace VDB.Core.Util.commands
{
    public class VDBAssignCommand : VDBCommandBase
    {
        public override string Name => "vdb_assign";
        public override string Help => "Assign a player to a role. Usage: vdb_assign <steamID> <roleName>";

        public override void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.instance.Print("Usage: vdb_assign <steamID> <roleName>");
                return;
            }

            string steamID = args[0];
            string roleName = args[1];

            bool success = ServerDB.AssignRole(steamID, roleName);
            Console.instance.Print(success
                ? $"[VDB] Player {steamID} assigned to role {roleName}."
                : $"[VDB] Failed to assign {steamID} to role {roleName}.");
        }

        protected override void SafeRun(string[] args)
        {
            throw new System.NotImplementedException();
        }

        public override List<string> CommandOptionList()
        {
           return ServerDB.GetRoleList();

        }
    }
}