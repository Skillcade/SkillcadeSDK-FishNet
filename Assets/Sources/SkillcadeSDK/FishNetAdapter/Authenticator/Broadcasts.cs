using FishNet.Broadcast;

namespace SkillcadeSDK.FishNetAdapter.Authenticator
{
    public struct TokenBroadcast : IBroadcast
    {
        public string Token;
        public string PlayerId;
        public string Nickname;
    }

    public struct TokenResponseBroadcast : IBroadcast
    {
        public bool Passed;
    }

    /// <summary>
    /// Empty marker: server permanently disconnects this client (anticheat, admin kick, etc.).
    /// </summary>
    public struct ServerKickBroadcast : IBroadcast { }

    /// <summary>
    /// Server-initiated permanent disconnect with optional user-facing message.
    /// </summary>
    public struct ServerDisconnectBroadcast : IBroadcast
    {
        public string Message;
    }
}