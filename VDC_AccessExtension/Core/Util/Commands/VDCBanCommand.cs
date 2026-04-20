using System.Collections.Generic;
using VDC.AccessExtension.Core.DataTypes;
using VDC.AccessExtension.Core.Patches;

namespace VDC.AccessExtension.Core.Util.Commands
{
    /// <summary>
    /// vdc_ban &lt;steamID|playerName&gt; [reason]
    /// Adds a player to the VDC banlist (and native bannedlist if sync is on).
    /// </summary>
    public class VDCBanCommand : VDCAccessCommandBase
    {
        public override string Name => "vdc_ban";
        public override string Help =>
            "Add a player to the VDC banlist.\n" +
            "Usage: vdc_ban <steamID|playerName> [reason]";

        protected override void SafeRun(string[] args)
        {
            if (args.Length < 1) { Console.instance.Print(Help); return; }

            string steamID = ResolveID(args[0]);
            if (steamID == null) return;

            string reason = args.Length >= 2 ? string.Join(" ", args, 1, args.Length - 1) : null;
            AccessDB.AddEntry(steamID, args[0], AccessListType.Banlist, reason);

            var cfg = AccessDB.GetListConfig(AccessListType.Banlist);
            if (cfg?.SyncToNative == true)
                ZNetPatches.SyncAddToNative(steamID, AccessListType.Banlist);

            Console.instance.Print($"[VDCAccess] {steamID} added to banlist." +
                                   (reason != null ? $" Reason: {reason}" : ""));
        }

        public override List<string> CommandOptionList() => new List<string>();
    }
}
