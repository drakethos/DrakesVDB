using System.Collections.Generic;
using VDC.AccessExtension.Core.DataTypes;
using VDC.AccessExtension.Core.Patches;

namespace VDC.AccessExtension.Core.Util.Commands
{
    /// <summary>
    /// vdc_admin &lt;steamID|playerName&gt; [reason]
    /// Adds a player to the VDC adminlist (and native adminlist if sync is on).
    /// </summary>
    public class VDCAdminCommand : VDCAccessCommandBase
    {
        public override string Name => "vdc_admin";
        public override string Help =>
            "Add a player to the VDC adminlist.\n" +
            "Usage: vdc_admin <steamID|playerName> [reason]";

        protected override void SafeRun(string[] args)
        {
            if (args.Length < 1) { Console.instance.Print(Help); return; }

            string steamID = ResolveID(args[0]);
            if (steamID == null) return;

            string reason = args.Length >= 2 ? string.Join(" ", args, 1, args.Length - 1) : null;
            AccessDB.AddEntry(steamID, args[0], AccessListType.Adminlist, reason);

            var cfg = AccessDB.GetListConfig(AccessListType.Adminlist);
            if (cfg?.SyncToNative == true)
                ZNetPatches.SyncAddToNative(steamID, AccessListType.Adminlist);

            Console.instance.Print($"[VDCAccess] {steamID} added to adminlist.");
        }

        public override List<string> CommandOptionList() => new List<string>();
    }
}
