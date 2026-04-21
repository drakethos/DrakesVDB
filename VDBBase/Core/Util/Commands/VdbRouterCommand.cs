using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using UnityEngine;
using VDB.Core.Util;

namespace VDB.Core.Util.Commands;

/// <summary>Root console command: <c>vdb &lt;subcommand&gt; [args...]</c></summary>
public sealed class VdbRouterCommand : ConsoleCommand
{
    private static VdbRouterCommand? _instance;
    private readonly Dictionary<string, VdbSubcommandBase> _handlers =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _registeredWithJotunn;

    public static VdbRouterCommand Instance => _instance ??= new VdbRouterCommand();

    private VdbRouterCommand()
    {
    }

    public override string Name => "vdb";

    public override string Help =>
        "VDB database commands. Usage: vdb help | vdb <subcommand> [args…]. Example: vdb addplayer MyChar 7656119…";

    internal void RegisterSubcommand(VdbSubcommandBase handler)
    {
        var key = handler.Subcommand?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(key))
            return;

        if (_handlers.ContainsKey(key))
            VdbLog.Warning($"[DrakeVDB] Duplicate vdb subcommand '{key}'; replacing previous handler.");

        _handlers[key] = handler;
    }

    internal void EnsureJotunnRegistered()
    {
        if (_registeredWithJotunn) return;
        Jotunn.Managers.CommandManager.Instance.AddConsoleCommand(this);
        _registeredWithJotunn = true;
        VdbLog.Info("[DrakeVDB] Registered root console command: vdb");
    }

    public override void Run(string[] args) => Dispatch(args);

    /// <summary>Shared routing for in-game console and headless harness.</summary>
    public void Dispatch(string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            VdbCli.Print("[VDB] Usage: vdb help  —  or  vdb <subcommand> [args…]");
            return;
        }

        var verb = args[0].Trim();
        var tail = args.Length > 1 ? args.Skip(1).ToArray() : Array.Empty<string>();

        if (verb.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            RunHelp(tail);
            return;
        }

        if (!_handlers.TryGetValue(verb, out var handler))
        {
            VdbCli.Print($"[VDB] Unknown subcommand '{verb}'. Type: vdb help");
            return;
        }

        handler.TryRun(tail);
    }

    private void RunHelp(string[] tail)
    {
        if (tail.Length == 0)
        {
            VdbCli.Print("[VDB] Subcommands:");
            foreach (var name in _handlers.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                if (_handlers.TryGetValue(name, out var h))
                    VdbCli.Print($"  {name,-14} {h.Help}");
            }

            VdbCli.Print("[VDB] Detail: vdb help <subcommand>");
            return;
        }

        var topic = string.Join(" ", tail).Trim();
        if (!_handlers.TryGetValue(topic, out var sub))
        {
            VdbCli.Print($"[VDB] No help for '{topic}'. Try: vdb help");
            return;
        }

        VdbCli.Print($"[VDB] vdb {topic}");
        VdbCli.Print(sub.Help);
    }

    public override List<string> CommandOptionList()
    {
        return _handlers.Keys
            .Where(k => !k.Equals("help", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .Concat(new[] { "help" })
            .ToList();
    }
}
