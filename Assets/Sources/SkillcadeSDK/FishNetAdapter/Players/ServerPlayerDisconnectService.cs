#if UNITY_SERVER || UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Managing.Server;
using SkillcadeSDK.Connection;
using SkillcadeSDK.FishNetAdapter.Authenticator;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    public class ServerPlayerDisconnectService : IServerPlayerDisconnectService
    {
        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly ConnectionConfig _connectionConfig;

        private readonly HashSet<int> _pendingDisconnectClientIds = new();

        public void DisconnectAfterAuthFailure(NetworkConnection connection, ServerDisconnectReason reason, string message = null)
        {
            if (!TryBeginDisconnect(connection, out var clientId))
                return;

            var response = new TokenResponseBroadcast { Passed = false };
            _networkManager.ServerManager.Broadcast(connection, response);
            Debug.Log($"[PlayerDisconnect] Auth failure notify connection={clientId} reason={reason} message={message}");

            BeginDisconnectRoutine(connection, clientId, reason, message, includeLegacyKickBroadcast: true);
        }

        public void Disconnect(NetworkConnection connection, ServerDisconnectReason reason, string message = null)
        {
            if (!TryBeginDisconnect(connection, out var clientId))
                return;

            BeginDisconnectRoutine(connection, clientId, reason, message, includeLegacyKickBroadcast: true);
        }

        public void Disconnect(int clientId, ServerDisconnectReason reason, string message = null)
        {
            if (!_networkManager.IsServerStarted)
            {
                Debug.LogWarning($"[PlayerDisconnect] Disconnect ignored clientId={clientId}: server not started");
                return;
            }

            if (!_networkManager.ServerManager.Clients.TryGetValue(clientId, out var connection))
            {
                Debug.LogWarning($"[PlayerDisconnect] Disconnect ignored clientId={clientId}: not found");
                return;
            }

            Disconnect(connection, reason, message);
        }

        private bool TryBeginDisconnect(NetworkConnection connection, out int clientId)
        {
            clientId = -1;
            if (connection == null || !connection.IsValid)
            {
                Debug.LogWarning("[PlayerDisconnect] Disconnect ignored: invalid connection");
                return false;
            }

            clientId = connection.ClientId;
            if (!_pendingDisconnectClientIds.Add(clientId))
            {
                Debug.Log($"[PlayerDisconnect] Disconnect already pending for connection={clientId}, skipping duplicate");
                return false;
            }

            return true;
        }

        private void BeginDisconnectRoutine(
            NetworkConnection connection,
            int clientId,
            ServerDisconnectReason reason,
            string message,
            bool includeLegacyKickBroadcast)
        {
            float delay = _connectionConfig != null ? _connectionConfig.DisconnectNotifyDelaySeconds : 0.15f;
            _networkManager.StartCoroutine(DisconnectRoutine(connection, clientId, reason, message, delay, includeLegacyKickBroadcast));
        }

        private IEnumerator DisconnectRoutine(
            NetworkConnection connection,
            int clientId,
            ServerDisconnectReason reason,
            string message,
            float delaySeconds,
            bool includeLegacyKickBroadcast)
        {
            string resolvedMessage = string.IsNullOrEmpty(message) ? reason.ToString() : message;

            Debug.Log($"[PlayerDisconnect] Notify connection={clientId} reason={reason} message={resolvedMessage} delay={delaySeconds}s");

            if (connection.IsValid)
            {
                _networkManager.ServerManager.Broadcast(connection, new ServerDisconnectBroadcast
                {
                    Message = resolvedMessage
                });

                if (includeLegacyKickBroadcast)
                    _networkManager.ServerManager.Broadcast(connection, new ServerKickBroadcast());
            }

            yield return null;

            if (delaySeconds > 0f)
                yield return new WaitForSecondsRealtime(delaySeconds);
            else
                yield return null;

            if (connection.IsValid)
            {
                var kickReason = MapKickReason(reason);
                var log = $"[PlayerDisconnect] Disconnect connection={clientId} reason={reason}: {resolvedMessage}";
                Debug.Log(log);
                connection.Kick(kickReason, LoggingType.Common, log);
            }
            else
            {
                Debug.Log($"[PlayerDisconnect] Connection={clientId} already inactive after notify delay");
            }

            _pendingDisconnectClientIds.Remove(clientId);
        }

        private static KickReason MapKickReason(ServerDisconnectReason reason)
        {
            return reason switch
            {
                ServerDisconnectReason.Anticheat => KickReason.ExploitAttempt,
                ServerDisconnectReason.AuthRejected => KickReason.UnusualActivity,
                ServerDisconnectReason.DuplicatePlayer => KickReason.UnusualActivity,
                ServerDisconnectReason.LobbyFull => KickReason.UnexpectedProblem,
                ServerDisconnectReason.ReconnectRejected => KickReason.UnusualActivity,
                ServerDisconnectReason.AdminKick => KickReason.UnexpectedProblem,
                _ => KickReason.UnexpectedProblem
            };
        }
    }
}
#endif
