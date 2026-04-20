namespace VDB.Client.Core.Protocol
{
    /// <summary>
    /// Sent by the client immediately after ZNet connection is established.
    /// The server uses the SteamID to look up the player in VDB and determine roles.
    /// </summary>
    public sealed class HandshakeRequest : IVDBPayload
    {
        public string SteamID      { get; set; }
        public string CharacterName { get; set; }
        /// <summary>Semver string of the VDBClient mod the player has installed.</summary>
        public string ClientVersion { get; set; }

        public string ToJson() =>
            $"{{\"SteamID\":\"{Esc(SteamID)}\"," +
            $"\"CharacterName\":\"{Esc(CharacterName)}\"," +
            $"\"ClientVersion\":\"{Esc(ClientVersion)}\"}}";

        public static HandshakeRequest FromJson(string json)
        {
            var r = new HandshakeRequest();
            r.SteamID       = JsonField(json, "SteamID");
            r.CharacterName = JsonField(json, "CharacterName");
            r.ClientVersion = JsonField(json, "ClientVersion");
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
    }
}
