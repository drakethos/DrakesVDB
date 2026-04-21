using System;

namespace VDB.Core.Util;

/// <summary>Prints VDB command output to Valheim console in-game, or to a harness override (e.g. stdout).</summary>
public static class VdbCli
{
    /// <summary>When set (e.g. by VDB-DBTest), all <see cref="Print"/> calls go here instead of Valheim's console.</summary>
    public static Action<string>? PrintOverride { get; set; }

    public static void Print(string message)
    {
        if (PrintOverride != null)
        {
            PrintOverride(message);
            return;
        }

        if (global::Console.instance != null)
            global::Console.instance.Print(message);
    }
}
