namespace VDB.Client.Core.Protocol
{
    /// <summary>
    /// Server's reply to a CommandRequest. Carries success/failure and any output text.
    /// </summary>
    public sealed class CommandResponse : IVDBPayload
    {
        public int    RequestID { get; set; }
        public bool   Success   { get; set; }
        /// <summary>Console output or error message from the command execution.</summary>
        public string Output    { get; set; }

        public string ToJson() =>
            $"{{\"RequestID\":{RequestID}," +
            $"\"Success\":{(Success ? "true" : "false")}," +
            $"\"Output\":\"{Esc(Output)}\"}}";

        public static CommandResponse FromJson(string json)
        {
            var r = new CommandResponse();
            r.RequestID = int.TryParse(JsonNumField(json, "RequestID"), out int rid) ? rid : 0;
            r.Success   = json.Contains("\"Success\":true");
            r.Output    = JsonStrField(json, "Output");
            return r;
        }

        private static string Esc(string s) => (s ?? "").Replace("\"", "\\\"").Replace("\n", "\\n");

        private static string JsonStrField(string json, string key)
        {
            string search = $"\"{key}\":\"";
            int start = json.IndexOf(search);
            if (start < 0) return "";
            start += search.Length;
            int end = json.IndexOf('"', start);
            return end < 0 ? "" : json.Substring(start, end - start);
        }

        private static string JsonNumField(string json, string key)
        {
            string search = $"\"{key}\":";
            int start = json.IndexOf(search);
            if (start < 0) return "";
            start += search.Length;
            int end = json.IndexOfAny(new[] { ',', '}' }, start);
            return end < 0 ? "" : json.Substring(start, end - start).Trim();
        }
    }
}
