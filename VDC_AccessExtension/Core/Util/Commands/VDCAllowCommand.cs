using System.Collections.Generic;
using VDC.AccessExtension.Core.DataTypes;
using VDC.AccessExtension.Core.Patches;

namespace VDC.AccessExtension.Core.Util.Commands
{
    /// <summary>
    /// vdc_allow &lt;steamID|playerName&gt; [reason]
    /// Adds a player to the VDC whitelist (and native permittedlist if sync is on).
    /// </summary>
    public class VDCAllowCommand : VDCAccessCommandBase
    {
        public override string Name => "vdc_allow";
        public override string Help =>
            "Add a player to the VDC whitelist.\n" +
            "Usage: vdc_allow <steamID|playerName> [reason]";

        protected override void SafeRun(string[] args)
        {
            if (args.Length < 1) { Console.instance.Print(Help); return; }

            string steamID = ResolveID(args[0]);
            if (steamID == null) return;

            string reason = args.Length >= 2 ? string.Join(" ", args, 1, args.Length - 1) : null;
            var entry = AccessDB.AddEntry(steamID, args[0], AccessListType.Whitelist, reason);

            var cfg = AccessDB.GetListConfig(AccessListType.Whitelist);
            if (cfg?.SyncToNative == true)
                ZNetPatches.SyncAddToNative(steamID, AccessListType.Whitelist);

            Console.instance.Print($"[VDCAccess] {steamID} added to whitelist.");
        }

        public override List<string> CommandOptionList() => new List<string>();
    }
}
