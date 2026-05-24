#if UNITY_SERVER || UNITY_EDITOR
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Managing.Server;
using SkillcadeSDK.FishNetAdapter.Authenticator;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    /// <summary>
    /// Server-side helper: notifies client via <see cref="ServerKickBroadcast"/> then disconnects.
    /// </summary>
    public class PlayerKickService
    {
        [Inject] private readonly NetworkManager _networkManager;

        public void Kick(NetworkConnection connection, string log = null)
        {
            if (connection == null || !connection.IsValid)
            {
                Debug.LogWarning("[PlayerDisconnect] Kick ignored: invalid connection");
                return;
            }

            var message = string.IsNullOrEmpty(log)
                ? $"[PlayerDisconnect] Kicking connection {connection.ClientId}"
                : $"[PlayerDisconnect] Kicking connection {connection.ClientId}: {log}";

            Debug.Log(message);
            _networkManager.ServerManager.Broadcast(connection, new ServerKickBroadcast());
            connection.Kick(KickReason.ExploitAttempt, LoggingType.Common, message);
        }

        public void Kick(int clientId, string log = null)
        {
            if (!_networkManager.IsServerStarted)
                return;

            if (!_networkManager.ServerManager.Clients.TryGetValue(clientId, out var connection))
            {
                Debug.LogWarning($"[PlayerDisconnect] Kick ignored: client {clientId} not found");
                return;
            }

            Kick(connection, log);
        }
    }
}
#endif
