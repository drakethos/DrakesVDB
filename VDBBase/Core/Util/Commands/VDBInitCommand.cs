using System;
using System.Collections.Generic;
using VDB.Core;
using VDB.Core.Util;

namespace VDB.Core.Util.Commands
{
    public class VDBInitCommand : VdbSubcommandBase
    {
        public override string Subcommand => "init";
        public override string Help => "Initialize the VDB database. Usage: vdb init [dbname]";

        protected override void SafeRun(string[] args)
        {
            try
            {
                string dbName = args.Length > 0 ? args[0] : "VDB.db";
                if (!string.IsNullOrEmpty(VdbRuntime.HarnessPersistenceRoot))
                    ServerDB.InitializeDB(dbName, VdbRuntime.HarnessPersistenceRoot);
                else
                    ServerDB.InitializeDB(dbName);

                VdbCli.Print($"[VDB] Initialized {dbName}. Default groups created.");
            }
            catch (Exception ex)
            {
                VdbCli.Print($"[VDB] Initialization failed: {ex.Message}");
            }
        }

        public override List<string> TabOptions() => new List<string> { "VDB.db" };
    }
}
