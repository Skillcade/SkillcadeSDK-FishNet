#if UNITY_SERVER || UNITY_EDITOR
using FishNet.Connection;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    /// <summary>
    /// Server-side API: notify client, wait, then disconnect. Used for auth reject, kicks, anticheat.
    /// </summary>
    public interface IServerPlayerDisconnectService
    {
        void Disconnect(NetworkConnection connection, ServerDisconnectReason reason, string message = null);

        void Disconnect(int clientId, ServerDisconnectReason reason, string message = null);

        /// <summary>
        /// Auth failure: sends TokenResponse Passed=false, then <see cref="Disconnect"/>.
        /// Does not invoke FishNet OnAuthenticationResult immediately (avoids transport race on WebGL).
        /// </summary>
        void DisconnectAfterAuthFailure(NetworkConnection connection, ServerDisconnectReason reason, string message = null);
    }
}
#endif
