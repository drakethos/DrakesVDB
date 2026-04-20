namespace VDB.Client.Core.Protocol
{
    /// <summary>
    /// Sent by an authenticated client to ask the server to execute a VDB command.
    /// Only commands the player is authorised to run will be accepted server-side.
    /// </summary>
    public sealed class CommandRequest : IVDBPayload
    {
        /// <summary>The full command string including arguments, e.g. "vdb_addplayer Drake 76561197960000001".</summary>
        public string Command     { get; set; }
        /// <summary>Monotonically incrementing ID so responses can be correlated.</summary>
        public int    RequestID   { get; set; }

        public string ToJson() =>
            $"{{\"Command\":\"{Esc(Command)}\",\"RequestID\":{RequestID}}}";

        public static CommandRequest FromJson(string json)
        {
            var r = new CommandRequest();
            r.Command   = JsonField(json, "Command");
            r.RequestID = int.TryParse(JsonField(json, "RequestID"), out int rid) ? rid : 0;
            return r;
        }

        private static string Esc(string s) => (s ?? "").Replace("\"", "\\\"");
        private static string JsonField(string json, string key)
        {
            string search = $"\"{key}\":\"";
            int start = json.IndexOf(search);
            if (start < 0)
            {
                // Try unquoted (numeric)
                search = $"\"{key}\":";
                start  = json.IndexOf(search);
                if (start < 0) return "";
                start += search.Length;
                int endNum = json.IndexOfAny(new[] { ',', '}' }, start);
                return endNum < 0 ? "" : json.Substring(start, endNum - start).Trim();
            }
            start += search.Length;
            int end = json.IndexOf('"', start);
            return end < 0 ? "" : json.Substring(start, end - start);
        }
    }
}
