using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VDB.Auth;
using VDB.Core;
using VDB.Core.DataTypes.Util;
using VDB.Core.Util;
using VDB.Core.Util.Commands;

namespace VDBDbTest;

internal static class Program
{
    private static int Main(string[] args)
    {
        var dataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VDB-DBTest");
        var dbFileName = "VDB.db";
        var commandTail = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                return 0;
            }

            if (a.Equals("--data", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    System.Console.Error.WriteLine("--data requires a path.");
                    return 1;
                }

                dataRoot = Path.GetFullPath(args[++i]);
                continue;
            }

            if (a.Equals("--db", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    System.Console.Error.WriteLine("--db requires a file name.");
                    return 1;
                }

                dbFileName = args[++i];
                continue;
            }

            commandTail.AddRange(args.Skip(i));
            break;
        }

        Directory.CreateDirectory(dataRoot);
        VdbRuntime.HarnessPersistenceRoot = dataRoot;
        VdbRuntime.TreatAsAdminForCommands = true;
        VdbCli.PrintOverride = static s => System.Console.WriteLine(s);

        ServerDB.InitializeDB(dbFileName, dataRoot);
        if (!ServerDB.IsInitialized)
        {
            System.Console.Error.WriteLine("Failed to open database (see log).");
            return 2;
        }

        VDBCommandRegistrar.RegisterSubcommandsInAssembly(typeof(VDBCommandRegistrar).Assembly);
        VDBCommandRegistrar.RegisterSubcommandsInAssembly(typeof(DrakesVdbAuth).Assembly);

        if (commandTail.Count > 0)
        {
            var argv = commandTail.ToArray();
            if (argv.Length > 0 && argv[0].Equals("vdb", StringComparison.OrdinalIgnoreCase))
                argv = argv.Skip(1).ToArray();

            VdbRouterCommand.Instance.Dispatch(argv);
            return 0;
        }

        System.Console.WriteLine("VDB-DBTest — type 'vdb …' or a subcommand directly. 'exit' to quit.");
        System.Console.WriteLine($"Data root: {dataRoot}  (DB: {dbFileName})");

        while (true)
        {
            System.Console.Write("vdb> ");
            var line = System.Console.ReadLine();
            if (line == null)
                break;

            line = line.Trim();
            if (line.Length == 0)
                continue;
            if (line.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("quit", StringComparison.OrdinalIgnoreCase))
                break;

            var parts = SplitArgs(line);
            if (parts.Count == 0)
                continue;

            if (parts[0].Equals("vdb", StringComparison.OrdinalIgnoreCase))
                parts.RemoveAt(0);

            if (parts.Count == 0)
                continue;

            VdbRouterCommand.Instance.Dispatch(parts.ToArray());
        }

        return 0;
    }

    private static void PrintHelp()
    {
        System.Console.WriteLine("VDB-DBTest — Valheim-free harness for VDB commands.");
        System.Console.WriteLine();
        System.Console.WriteLine("  --data <path>   Folder used like BepInEx config root (DrakesVDB created inside).");
        System.Console.WriteLine($"                  Default: %LocalAppData%\\VDB-DBTest");
        System.Console.WriteLine("  --db <file>     Database file name (default: VDB.db).");
        System.Console.WriteLine("  --help          Show this help.");
        System.Console.WriteLine();
        System.Console.WriteLine("With no further arguments, starts a REPL. Otherwise runs one command, e.g.:");
        System.Console.WriteLine("  VDB-DBTest.exe --data C:\\Temp\\vdb vdb help");
    }

    private static List<string> SplitArgs(string line) =>
        line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
}
