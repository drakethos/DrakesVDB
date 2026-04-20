namespace VDB.Client.Core.Protocol
{
    /// <summary>
    /// Envelope written to / read from a ZPackage for every VDB RPC message.
    /// Layout on the wire:  [int PacketType] [string Payload (JSON or plain text)]
    /// </summary>
    public sealed class VDBPacket
    {
        public PacketType Type    { get; set; }
        /// <summary>JSON-serialised inner payload. Use the static helpers to pack/unpack.</summary>
        public string     Payload { get; set; }

        // -----------------------------------------------------------------------
        // ZPackage serialisation helpers
        // -----------------------------------------------------------------------

        public void Write(ZPackage pkg)
        {
            pkg.Write((int)Type);
            pkg.Write(Payload ?? string.Empty);
        }

        public static VDBPacket Read(ZPackage pkg)
        {
            int    type    = pkg.ReadInt();
            string payload = pkg.ReadString();
            return new VDBPacket { Type = (PacketType)type, Payload = payload };
        }

        // -----------------------------------------------------------------------
        // Simple JSON helpers (no external dependency — hand-rolled for the small
        // payloads we need; swap for a real serialiser if the schema grows)
        // -----------------------------------------------------------------------

        /// <summary>Wraps an inner payload object by calling its ToJson() method.</summary>
        public static VDBPacket Pack<T>(PacketType type, T payload) where T : IVDBPayload =>
            new VDBPacket { Type = type, Payload = payload.ToJson() };
    }

    /// <summary>All concrete payload types implement this so VDBPacket can serialise them.</summary>
    public interface IVDBPayload
    {
        string ToJson();
    }
}
