namespace VDB.Client.Core.Protocol
{
    /// <summary>
    /// All packet types exchanged over Valheim's ZRoutedRpc channel.
    /// Every packet is prefixed with this type so the receiver knows how to deserialise it.
    /// </summary>
    public enum PacketType : int
    {
        // Client → Server
        HandshakeRequest  = 100,   // "I have VDBClient installed, here is my Steam ID"
        CommandRequest    = 101,   // Client asks server to execute a VDB command on their behalf

        // Server → Client
        HandshakeResponse = 200,   // Server confirms auth, sends back role list + mod version
        CommandResponse   = 201,   // Server sends back the result of a CommandRequest
        KickNotice        = 202,   // Server is about to kick this peer (includes reason)
        RoleUpdate        = 203,   // Server pushes an updated role/auth state mid-session
    }
}
