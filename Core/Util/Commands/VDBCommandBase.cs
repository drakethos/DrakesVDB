using System.Linq;
using Jotunn;
using Jotunn.Entities;
using VDB.Core.DataTypes;
using UnityEngine;

namespace VDB.Core.Util.Commands
{
    public abstract class VDBCommandBase : ConsoleCommand
    {
        // Override this in child Commands if they need to bypass admin check
        public virtual bool RequiresAdmin => true;

        public override void Run(string[] args)
        {
            // Validate if player is allowed
            var player = Player.m_localPlayer;
            if (RequiresAdmin && !IsAdmin(player))
            {
                Console.instance.Print("[VDB] You must be an admin to run this command.");
                return;
            }

            SafeRun(args);
        }

        // Must be implemented in children instead of Run()
        protected abstract void SafeRun(string[] args);

        private bool IsAdmin(Player player)
        {
            Debug.Log(("Checking if they are an admin"));
            if (player == null)
                return false;

            string steamID = player.GetPlayerID().ToString();
         
            Debug.Log(("Checking Zinstance"));
            // 1️⃣ Native Valheim adminlist.txt check
            if (ZNet.instance?.IsAdmin(steamID) ?? false)
            {
                Debug.Log($"{player.GetPlayerName()} identified as admin");
                return true;
            }

            // 2️⃣ Our DB-driven role check using existing ServerDB methods
            Debug.Log("Checking db for admin list");
            var roles = ServerDB.GetRoles(steamID);
            return roles.Any(r => r.Equals("Admin", System.StringComparison.OrdinalIgnoreCase));
        }
    }
}