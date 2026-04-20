using System.Collections.Generic;
using System.Text;
using Jotunn.Entities;
using VDB.Client.Core.Client;
using VDB.Client.Core.Server;

namespace VDB.Client.Core.Commands
{
    /// <summary>
    /// vdb_client_status
    /// Prints the local auth state (client side) and, when run on a server,
    /// lists all connected peer sessions.
    /// </summary>
    public class VDBClientStatusCommand : ConsoleCommand
    {
        public override string Name => "vdb_client_status";
        public override string Help =>
            "Show VDBClient authentication status and connected sessions.\n" +
            "Usage: vdb_client_status";

        public override void Run(string[] args)
        {
            var sb = new StringBuilder();

            // ---- Local client state ----
            sb.AppendLine("=== VDBClient — Local Session ===");
            if (VDBRpcClient.IsAuthenticated)
            {
                sb.AppendLine($"  Status    : Authenticated");
                sb.AppendLine($"  SteamID   : {VDBRpcClient.SteamID}");
                sb.AppendLine($"  Character : {VDBRpcClient.CharacterName}");
                sb.AppendLine($"  Roles     : {(VDBRpcClient.Roles.Count > 0 ? string.Join(", ", VDBRpcClient.Roles) : "(none)")}");
                sb.AppendLine($"  IsAdmin   : {VDBRpcClient.IsAdmin}");
            }
            else
            {
                sb.AppendLine("  Status : Not authenticated (handshake pending or failed)");
            }

            // ---- Server session table (only meaningful on the server) ----
            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                sb.AppendLine();
                sb.AppendLine("=== Connected Peer Sessions ===");
                int count = 0;
                foreach (var session in VDBRpcServer.GetAllSessions())
                {
                    sb.AppendLine($"  [{session.State}] {session.PeerID}" +
                                  $"  {session.CharacterName ?? "?"}" +
                                  $"  SteamID={session.SteamID ?? "?"}" +
                                  $"  Roles=[{string.Join(", ", session.Roles)}]");
                    count++;
                }
                if (count == 0) sb.AppendLine("  (no peers)");
            }

            Console.instance.Print(sb.ToString().TrimEnd());
        }

        public override List<string> CommandOptionList() => new List<string>();
    }

    /// <summary>
    /// vdb_runcmd &lt;command string&gt;
    /// Sends a VDB command to the server for execution.
    /// The server will validate the caller's role before running it.
    /// Requires the local client to be authenticated.
    /// </summary>
    public class VDBRunCmdCommand : ConsoleCommand
    {
        public override string Name => "vdb_runcmd";
        public override string Help =>
            "Send a VDB command to the server for execution (requires authentication).\n" +
            "Usage: vdb_runcmd <command and args>";

        public override void Run(string[] args)
        {
            if (args.Length < 1)
            {
                Console.instance.Print(Help);
                return;
            }

            if (!VDBRpcClient.IsAuthenticated)
            {
                Console.instance.Print("[VDBClient] Not authenticated with server yet.");
                return;
            }

            string commandLine = string.Join(" ", args);
            Console.instance.Print($"[VDBClient] Sending command to server: {commandLine}");

            VDBRpcClient.SendCommand(commandLine, resp =>
            {
                if (!resp.Success)
                    Console.instance.Print($"[VDBClient] Command failed: {resp.Output}");
                // Successful output is already echoed inside VDBRpcClient.OnCommandResponse
            });
        }

        public override List<string> CommandOptionList() => new List<string>();
    }

    /// <summary>
    /// vdb_kick &lt;steamID|characterName&gt; [reason]
    /// Server-side command to kick a peer by Steam ID or character name.
    /// Requires the caller to be authenticated as Admin.
    /// </summary>
    public class VDBKickCommand : ConsoleCommand
    {
        public override string Name => "vdb_kick";
        public override string Help =>
            "Kick a player from the server (admin only).\n" +
            "Usage: vdb_kick <steamID|characterName> [reason]";

        public override void Run(string[] args)
        {
            if (args.Length < 1) { Console.instance.Print(Help); return; }

            if (!VDBRpcClient.IsAuthenticated || !VDBRpcClient.IsAdmin)
            {
                Console.instance.Print("[VDBClient] Admin authentication required.");
                return;
            }

            string target = args[0];
            string reason = args.Length >= 2
                ? string.Join(" ", args, 1, args.Length - 1)
                : "Kicked by admin.";

            // Try to find the session by SteamID or character name
            ClientSession session = null;
            foreach (var s in VDBRpcServer.GetAllSessions())
            {
                if (s.SteamID == target ||
                    (s.CharacterName != null &&
                     s.CharacterName.Equals(target, System.StringComparison.OrdinalIgnoreCase)))
                {
                    session = s;
                    break;
                }
            }

            if (session == null)
            {
                Console.instance.Print($"[VDBClient] No active session found for '{target}'.");
                return;
            }

            VDBRpcServer.KickPeer(session.PeerID, reason);
            Console.instance.Print($"[VDBClient] Kicked {session.CharacterName} ({session.SteamID}). Reason: {reason}");
        }

        public override List<string> CommandOptionList() => new List<string>();
    }
}
