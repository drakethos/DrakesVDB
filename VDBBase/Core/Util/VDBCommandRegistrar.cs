using System;
using System.Linq;
using System.Reflection;
using VDB.Core.Util;
using VDB.Core.Util.Commands;

namespace VDB.Core.DataTypes.Util;

public static class VDBCommandRegistrar
{
    /// <summary>Registers subcommands from VDB-Base and installs the root <c>vdb</c> command with Jotunn.</summary>
    public static void RegisterAll()
    {
        RegisterSubcommandsInAssembly(typeof(VDBCommandRegistrar).Assembly);
        VdbRouterCommand.Instance.EnsureJotunnRegistered();
    }

    /// <summary>Registers <see cref="VdbSubcommandBase"/> types from another assembly (e.g. DrakeVDB-Auth) and ensures the root command is registered with Jotunn.</summary>
    public static void RegisterCommandsInAssembly(Assembly assembly)
    {
        RegisterSubcommandsInAssembly(assembly);
        VdbRouterCommand.Instance.EnsureJotunnRegistered();
    }

    /// <summary>Registers subcommands only (no Jotunn). Use from VDB-DBTest after Base, then extension assemblies.</summary>
    public static void RegisterSubcommandsInAssembly(Assembly assembly)
    {
        var subcommandTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(VdbSubcommandBase).IsAssignableFrom(t));

        foreach (var type in subcommandTypes)
        {
            if (Activator.CreateInstance(type) is not VdbSubcommandBase instance)
                continue;

            VdbRouterCommand.Instance.RegisterSubcommand(instance);
            VdbLog.Info($"[DrakeVDB] Registered vdb subcommand: {instance.Subcommand} ({type.Name})");
        }
    }
}
