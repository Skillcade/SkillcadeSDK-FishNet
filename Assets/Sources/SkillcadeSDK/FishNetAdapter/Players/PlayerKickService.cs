#if UNITY_SERVER || UNITY_EDITOR
using FishNet.Connection;
using SkillcadeSDK.FishNetAdapter.Players;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    /// <summary>
    /// Convenience wrapper for anticheat / admin kicks during a match.
    /// </summary>
    public class PlayerKickService
    {
        [Inject] private readonly IServerPlayerDisconnectService _disconnectService;

        public void Kick(NetworkConnection connection, string message = null)
        {
            _disconnectService.Disconnect(connection, ServerDisconnectReason.Anticheat, message);
        }

        public void Kick(int clientId, string message = null)
        {
            _disconnectService.Disconnect(clientId, ServerDisconnectReason.Anticheat, message);
        }
    }
}
#endif
