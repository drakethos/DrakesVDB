using System.Linq;
using Jotunn.Entities;
using VDB.Core.DataTypes;
using VDB.Core.DataTypes.Util;
using UnityEngine;

namespace VDB.Core.Util.Commands
{
    /// <summary>
    /// Describes where the admin privilege was sourced from.
    /// </summary>
    public enum AdminType
    {
        None,
        /// <summary>Single-player / offline session — the local player is always implicitly admin.</summary>
        LocalAdmin,
        /// <summary>Listed in the server's adminlist.txt (checked via ZNet.IsAdmin).</summary>
        ServerAdmin,
        /// <summary>Assigned the Admin role in the VDB database.</summary>
        DBAdmin
    }

    public abstract class VDBCommandBase : ConsoleCommand
    {
        // Override to false in child commands that should be accessible to all players.
        public virtual bool RequiresAdmin => true;

        public override void Run(string[] args)
        {
            var player = Player.m_localPlayer;
            if (RequiresAdmin && GetAdminType(player) == AdminType.None)
            {
                Console.instance.Print("[VDB] You must be an admin to run this command.");
                return;
            }

            SafeRun(args);
        }

        // Children implement this instead of Run().
        protected abstract void SafeRun(string[] args);

        // -----------------------------------------------------------------------
        // Admin detection helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns the AdminType for the given player, or AdminType.None if they
        /// are not an admin.  Checks in order: local → server list → VDB DB.
        /// </summary>
        public static AdminType GetAdminType(Player player)
        {
            if (player == null)
                return AdminType.None;

            // 1. Local / solo session: server is running locally and is NOT a dedicated server.
            //    ZNet.IsServer() = true in both solo and as the host; IsDedicated() = true only for
            //    a dedicated server.  Solo/local-host sessions implicitly trust the local player.
            if (ZNet.instance.IsServer() && !ZNet.instance.IsDedicated())
            {
                Debug.Log("[DrakeVDB] Local/hosted session detected — treating player as LocalAdmin.");
                return AdminType.LocalAdmin;
            }

            string steamID = GetSteamID(player);
            if (string.IsNullOrEmpty(steamID))
            {
                Debug.LogWarning("[DrakeVDB] Could not resolve SteamID for admin check.");
                return AdminType.None;
            }

            // 2. Server adminlist.txt (native Valheim check)
            if (ZNet.instance.IsAdmin(steamID))
            {
                Debug.Log($"[DrakeVDB] {player.GetPlayerName()} is a ServerAdmin (adminlist.txt). SteamID={steamID}");
                return AdminType.ServerAdmin;
            }

            // 3. VDB database Admin role
            var roles = ServerDB.GetRoles(steamID);
            if (roles.Any(r => r.Equals("Admin", System.StringComparison.OrdinalIgnoreCase)))
            {
                Debug.Log($"[DrakeVDB] {player.GetPlayerName()} is a DBAdmin (VDB role). SteamID={steamID}");
                return AdminType.DBAdmin;
            }

            Debug.Log($"[DrakeVDB] {player.GetPlayerName()} has no admin privileges. SteamID={steamID}");
            return AdminType.None;
        }

        /// <summary>
        /// Convenience overload — returns true if the player has any admin type.
        /// </summary>
        public static bool IsAdmin(Player player) => GetAdminType(player) != AdminType.None;

        // -----------------------------------------------------------------------
        // Steam ID resolution
        // -----------------------------------------------------------------------

        /// <summary>
        /// Resolves the current player's Steam/platform ID as a string.
        /// Uses GetPlayerID() which is reliable for the local player; for remote
        /// players on a server use Helper.getSteamId(name) instead.
        /// </summary>
        public static string GetSteamID(Player player)
        {
            if (player == null) return null;
            long rawId = player.GetPlayerID();
            if (rawId == 0) return null;
            return rawId.ToString();
        }

        /// <summary>
        /// Resolves a Steam ID for a named remote player via ZNet peer list.
        /// Returns null if not found.
        /// </summary>
        public static string GetSteamIDByName(string playerName)
        {
            ulong id = Helper.getSteamId(playerName);
            return id != 0 ? id.ToString() : null;
        }
    }
}
