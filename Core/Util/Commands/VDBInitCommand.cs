using System;
using System.Collections.Generic;
using VDB.Core.DataTypes;
using VDB.Core.Util.Commands;

namespace VDB.Core.Util.commands
{
    public class VDBInitCommand : VDBCommandBase
    {
        public override string Name => "vdb_init";
        public override string Help => "Initializes the VDB database. Usage: vdb_init [dbname]";

        protected override void SafeRun(string[] args)
        {
            try
            {
                string dbName = args.Length > 0 ? args[0] : "VDB.db";
                ServerDB.InitializeDB(dbName);
                Console.instance.Print($"[VDB] Initialized {dbName}. Default groups created.");
            }
            catch (Exception ex)
            {
                Console.instance.Print($"[VDB] Initialization failed: {ex.Message}");
            }
        }

        public override List<string> CommandOptionList() => new List<string> { "VDB.db" };
    }
}
