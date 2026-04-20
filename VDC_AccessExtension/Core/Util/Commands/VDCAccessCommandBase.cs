using Jotunn.Entities;
using UnityEngine;
using VDB.Core.DataTypes.Util;
using VDB.Core.Util.Commands;

namespace VDC.AccessExtension.Core.Util.Commands
{
    /// <summary>
    /// Base class for all VDC Access commands.
    /// Reuses VDB's admin detection so VDC commands respect the same admin gate.
    /// </summary>
    public abstract class VDCAccessCommandBase : ConsoleCommand
    {
        public virtual bool RequiresAdmin => true;

        public override void Run(string[] args)
        {
            var player = Player.m_localPlayer;
            if (RequiresAdmin && !VDBCommandBase.IsAdmin(player))
            {
                Console.instance.Print("[VDCAccess] You must be an admin to run this command.");
                return;
            }

            SafeRun(args);
        }

        protected abstract void SafeRun(string[] args);

        /// <summary>
        /// Resolves a Steam ID string from user input.
        /// Accepts a raw numeric ID or a character name (resolved via ZNet peer list).
        /// Returns null and prints an error if unresolvable.
        /// </summary>
        protected static string ResolveID(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            if (ulong.TryParse(input, out _))
                return input.Trim();

            // Try resolving as character name
            string resolved = VDBCommandBase.GetSteamIDByName(input.Trim());
            if (resolved == null)
                Console.instance.Print($"[VDCAccess] Cannot resolve Steam ID for '{input}'. Is the player online?");

            return resolved;
        }
    }
}
