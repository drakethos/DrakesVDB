
using System.Collections.Generic;

namespace VDB.Core
{
    public static class VDBSession
    {
        public static ulong SteamID { get; internal set; }
        public static string PlayerName { get; internal set; }
        public static List<string> Roles { get; internal set; } = new();

        public static void Initialize(ulong steamId, string playerName, List<string> roles)
        {
            SteamID = steamId;
            PlayerName = playerName;
            Roles = roles ?? new();
            Jotunn.Logger.LogInfo($"[VDB] Session initialized for {playerName} ({steamId}), roles: {string.Join(", ", Roles)}");
        }
    }
}
