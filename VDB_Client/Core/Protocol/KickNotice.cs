namespace VDB.Client.Core.Protocol
{
    /// <summary>
    /// Sent by the server just before it calls ZNet.Disconnect on a peer.
    /// Gives the client a chance to show the player a meaningful message.
    /// </summary>
    public sealed class KickNotice : IVDBPayload
    {
        public string Reason { get; set; }

        public string ToJson() =>
            $"{{\"Reason\":\"{Esc(Reason)}\"}}";

        public static KickNotice FromJson(string json)
        {
            var k = new KickNotice();
            string search = "\"Reason\":\"";
            int start = json.IndexOf(search);
            if (start >= 0)
            {
                start += search.Length;
                int end = json.IndexOf('"', start);
                k.Reason = end >= 0 ? json.Substring(start, end - start) : "";
            }
            return k;
        }

        private static string Esc(string s) => (s ?? "").Replace("\"", "\\\"");
    }
}
