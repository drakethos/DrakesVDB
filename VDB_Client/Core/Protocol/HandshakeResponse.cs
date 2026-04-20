using System.Collections.Generic;

namespace VDB.Client.Core.Protocol
{
    /// <summary>
    /// Server's reply to a HandshakeRequest.
    /// Carries the player's DB record summary and resolved role list.
    /// </summary>
    public sealed class HandshakeResponse : IVDBPayload
    {
        public bool         Accepted      { get; set; }
        /// <summary>Human-readable reason when Accepted == false.</summary>
        public string       RejectReason  { get; set; }
        /// <summary>The player's DB primary key — used by the client to correlate future messages.</summary>
        public int          PlayerDBID    { get; set; }
        public string       SteamID       { get; set; }
        public string       CharacterName { get; set; }
        /// <summary>All VDB role names assigned to this player (e.g. "Admin", "GroupA").</summary>
        public List<string> Roles         { get; set; } = new List<string>();
        /// <summary>Server-side VDBClient mod version for compatibility checks.</summary>
        public string       ServerVersion { get; set; }

        public string ToJson()
        {
            string roles = "[" + string.Join(",", Roles.ConvertAll(r => $"\"{Esc(r)}\"")) + "]";
            return $"{{\"Accepted\":{(Accepted ? "true" : "false")}," +
                   $"\"RejectReason\":\"{Esc(RejectReason)}\"," +
                   $"\"PlayerDBID\":{PlayerDBID}," +
                   $"\"SteamID\":\"{Esc(SteamID)}\"," +
                   $"\"CharacterName\":\"{Esc(CharacterName)}\"," +
                   $"\"Roles\":{roles}," +
                   $"\"ServerVersion\":\"{Esc(ServerVersion)}\"}}";
        }

        public static HandshakeResponse FromJson(string json)
        {
            var r = new HandshakeResponse();
            r.Accepted      = json.Contains("\"Accepted\":true");
            r.RejectReason  = JsonField(json, "RejectReason");
            r.PlayerDBID    = int.TryParse(JsonField(json, "PlayerDBID"), out int id) ? id : 0;
            r.SteamID       = JsonField(json, "SteamID");
            r.CharacterName = JsonField(json, "CharacterName");
            r.ServerVersion = JsonField(json, "ServerVersion");
            r.Roles         = ParseStringArray(json, "Roles");
            return r;
        }

        private static string Esc(string s) => (s ?? "").Replace("\"", "\\\"");

        private static string JsonField(string json, string key)
        {
            string search = $"\"{key}\":\"";
            int start = json.IndexOf(search);
            if (start < 0) return "";
            start += search.Length;
            int end = json.IndexOf('"', start);
            return end < 0 ? "" : json.Substring(start, end - start);
        }

        private static List<string> ParseStringArray(string json, string key)
        {
            var result = new List<string>();
            string search = $"\"{key}\":[";
            int start = json.IndexOf(search);
            if (start < 0) return result;
            start += search.Length;
            int end = json.IndexOf(']', start);
            if (end < 0) return result;
            string inner = json.Substring(start, end - start);
            foreach (string part in inner.Split(','))
            {
                string clean = part.Trim().Trim('"');
                if (!string.IsNullOrEmpty(clean)) result.Add(clean);
            }
            return result;
        }
    }
}
