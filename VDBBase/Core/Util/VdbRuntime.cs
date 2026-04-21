namespace VDB.Core.Util;

/// <summary>Host/runtime flags shared by the BepInEx plugin and headless harness.</summary>
public static class VdbRuntime
{
    /// <summary>When true, <see cref="VdbSubcommandBase"/> skips Player/ZNet admin checks (harness / tooling).</summary>
    public static bool TreatAsAdminForCommands { get; set; }

    /// <summary>When set, <c>vdb init</c> and harness startup use this folder like BepInEx config path (child <c>DrakesVDB</c> is created).</summary>
    public static string? HarnessPersistenceRoot { get; set; }
}
