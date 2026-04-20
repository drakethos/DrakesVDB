using System.Collections.Generic;
using System.Text;
using VDC.AccessExtension.Core.DataTypes;

namespace VDC.AccessExtension.Core.Util.Commands
{
    /// <summary>
    /// vdc_listaccess [whitelist|banlist|adminlist]
    /// Prints all active entries on the specified list (or all lists if omitted).
    /// </summary>
    public class VDCListAccessCommand : VDCAccessCommandBase
    {
        public override string Name => "vdc_listaccess";
        public override string Help =>
            "List active VDC access entries.\n" +
            "Usage: vdc_listaccess [whitelist|banlist|adminlist]";

        protected override void SafeRun(string[] args)
        {
            if (args.Length >= 1)
            {
                if (!TryParseListType(args[0], out AccessListType listType))
                {
                    Console.instance.Print("[VDCAccess] Unknown list. Use: whitelist | banlist | adminlist");
                    return;
                }
                PrintList(listType);
            }
            else
            {
                PrintList(AccessListType.Whitelist);
                PrintList(AccessListType.Banlist);
                PrintList(AccessListType.Adminlist);
            }
        }

        private static void PrintList(AccessListType listType)
        {
            var entries = AccessDB.GetAllEntries(listType);
            var sb = new StringBuilder();
            sb.AppendLine($"--- {listType} ---");

            int count = 0;
            foreach (var e in entries)
            {
                sb.AppendLine($"  {e.SteamID}  ({e.PlayerName})" +
                              (e.Reason    != null ? $"  Reason: {e.Reason}"         : "") +
                              (e.ExpiresAt != null ? $"  Expires: {e.ExpiresAt:u}"   : ""));
                count++;
            }

            if (count == 0) sb.AppendLine("  (empty)");

            Console.instance.Print(sb.ToString().TrimEnd());
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
