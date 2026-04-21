using System.Diagnostics;

namespace VDB.Core.Util;

/// <summary>Logging that avoids UnityEngine when running under VDB-DBTest (fixes netstandard/Unity shim issues).</summary>
public static class VdbLog
{
    private static bool UseSystemLogging => !string.IsNullOrEmpty(VdbRuntime.HarnessPersistenceRoot);

    public static void Info(string message)
    {
        if (UseSystemLogging)
            Trace.WriteLine(message);
        else
            UnityEngine.Debug.Log(message);
    }

    public static void Warning(string message)
    {
        if (UseSystemLogging)
            Trace.WriteLine("[WARN] " + message);
        else
            UnityEngine.Debug.LogWarning(message);
    }

    public static void Error(string message)
    {
        if (UseSystemLogging)
            Trace.TraceError(message);
        else
            UnityEngine.Debug.LogError(message);
    }
}
