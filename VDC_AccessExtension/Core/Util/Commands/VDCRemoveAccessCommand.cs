using System.Collections.Generic;
using VDC.AccessExtension.Core.DataTypes;
using VDC.AccessExtension.Core.Patches;

namespace VDC.AccessExtension.Core.Util.Commands
{
    /// <summary>
    /// vdc_remove &lt;whitelist|banlist|adminlist&gt; &lt;steamID|playerName&gt;
    /// Deactivates a VDC access entry (soft-delete) and optionally removes from native list.
    /// </summary>
    public class VDCRemoveAccessCommand : VDCAccessCommandBase
    {
        public override string Name => "vdc_remove";
        public override string Help =>
            "Remove a player from a VDC access list.\n" +
            "Usage: vdc_remove <whitelist|banlist|adminlist> <steamID|playerName>";

        protected override void SafeRun(string[] args)
        {
            if (args.Length < 2) { Console.instance.Print(Help); return; }

            if (!TryParseListType(args[0], out AccessListType listType))
            {
                Console.instance.Print("[VDCAccess] Unknown list type. Use: whitelist | banlist | adminlist");
                return;
            }

            string steamID = ResolveID(args[1]);
            if (steamID == null) return;

            bool removed = AccessDB.RemoveEntry(steamID, listType);

            if (removed)
            {
                var cfg = AccessDB.GetListConfig(listType);
                if (cfg?.SyncToNative == true)
                    ZNetPatches.SyncRemoveFromNative(steamID, listType);

                Console.instance.Print($"[VDCAccess] {steamID} removed from {listType}.");
            }
            else
            {
                Console.instance.Print($"[VDCAccess] {steamID} was not found on {listType}.");
            }
        }

        private static bool TryParseListType(string input, out AccessListType result)
        {
            switch (input.ToLowerInvariant())
            {
                case "whitelist":  result = AccessListType.Whitelist;  return true;
                case "banlist":    result = AccessListType.Banlist;    return true;
                case "adminlist":  result = AccessListType.Adminlist;  return true;
                default:           result = default;                   return false;
            }
        }

        public override List<string> CommandOptionList() =>
            new List<string> { "whitelist", "banlist", "adminlist" };
    }
}
