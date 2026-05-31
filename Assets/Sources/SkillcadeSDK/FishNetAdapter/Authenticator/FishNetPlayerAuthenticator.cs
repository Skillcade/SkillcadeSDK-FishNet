using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Managing.Server;
using FishNet.Transporting;
using SkillcadeSDK.Connection;
using SkillcadeSDK.FishNetAdapter;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

#if UNITY_SERVER || UNITY_EDITOR
using SkillcadeSDK.ServerValidation;
#endif

namespace SkillcadeSDK.FishNetAdapter.Authenticator
{
    public class FishNetPlayerAuthenticator : FishNet.Authenticating.Authenticator
    {
        public override event Action<NetworkConnection, bool> OnAuthenticationResult;

        [Inject] private readonly ConnectionConfig _connectionConfig;
        [Inject] private readonly WebBridge _webBridge;
        [Inject] private readonly AuthenticatedPlayerDataStore _authenticatedPlayerDataStore;
        [Inject] private readonly PlayerReconnectService _reconnectService;
        [Inject] private readonly IConnectionController _connectionController;

#if UNITY_SERVER || UNITY_EDITOR
        [Inject] private readonly SessionValidator _sessionValidator;
        [Inject] private readonly ServerPayloadController _serverPayloadController;
        [Inject] private readonly IServerPlayerDisconnectService _disconnectService;
        [Inject] private readonly FishNetPlayersController _playersController;
        [Inject] private readonly SkillcadeGameStateMachine _gameStateMachine;
        
        public IReadOnlyDictionary<int, SessionTokenPayload> ClientTokens => _clientTokens;
        
        private readonly Dictionary<int, SessionTokenPayload> _clientTokens = new();
#endif

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);
            
            NetworkManager.ClientManager.RegisterBroadcast<TokenResponseBroadcast>(HandleTokenResponse);
            NetworkManager.ClientManager.RegisterBroadcast<ServerKickBroadcast>(HandleServerKick);
            NetworkManager.ClientManager.RegisterBroadcast<ServerDisconnectBroadcast>(HandleServerDisconnect);
            NetworkManager.ClientManager.OnClientConnectionState += HandleConnectionState;

            NetworkManager.ServerManager.RegisterBroadcast<TokenBroadcast>(HandleToken, false);
        }

        private void HandleConnectionState(ClientConnectionStateArgs stateArgs)
        {
            Debug.Log($"[PlayerAuth] Client connection state={stateArgs.ConnectionState} transportIndex={stateArgs.TransportIndex}");
            if (stateArgs.ConnectionState != LocalConnectionState.Started)
                return;

            bool hub = _connectionConfig.SkillcadeHubIntegrated;
            bool hasPayload = _webBridge.Payload != null;
            bool hasToken = hasPayload && !string.IsNullOrEmpty(_webBridge.Payload.JoinToken);

            string token = "empty";
            if (hub)
            {
                if (!hasToken)
                    Debug.LogError($"[PlayerAuth] Hub client connected but JoinToken missing (hasPayload={hasPayload})");
                else
                    token = _webBridge.Payload.JoinToken;
            }

            var message = new TokenBroadcast
            {
                Token = token,
                PlayerId = _webBridge.Payload?.PlayerId,
                Nickname = _webBridge.Payload?.Nickname,
            };

            Debug.Log($"[PlayerAuth] Sending TokenBroadcast hub={hub} playerId={message.PlayerId} nickname={message.Nickname} tokenLen={token?.Length ?? 0}");
            NetworkManager.ClientManager.Broadcast(message);
        }

        private void HandleTokenResponse(TokenResponseBroadcast message, Channel channel)
        {
            if (!message.Passed)
            {
                Debug.Log($"[PlayerAuth] [PlayerDisconnect] TokenResponse Passed=false message={message.Message} — permanent disconnect");
                NotifyClientKicked(message.Message);
                return;
            }

            Debug.Log("[PlayerAuth] TokenResponse Passed=true — authentication succeeded on client");
            _connectionController.NotifyClientAuthenticated();
        }

        private void HandleServerKick(ServerKickBroadcast message, Channel channel)
        {
            Debug.Log("[PlayerDisconnect] ServerKickBroadcast received — permanent disconnect");
            NotifyClientKicked(null);
        }

        private void HandleServerDisconnect(ServerDisconnectBroadcast message, Channel channel)
        {
            Debug.Log($"[PlayerDisconnect] ServerDisconnectBroadcast received message={message.Message}");
            NotifyClientKicked(message.Message);
        }

        private void NotifyClientKicked(string serverMessage)
        {
            _connectionController.NotifyPermanentDisconnect(DisconnectionReason.Kicked, serverMessage);
        }

        private void HandleToken(NetworkConnection connection, TokenBroadcast message, Channel channel)
        {
#if UNITY_SERVER || UNITY_EDITOR
            Debug.Log($"[PlayerAuth] HandleToken connection={connection.ClientId} hub={_connectionConfig.SkillcadeHubIntegrated} broadcastPlayerId={message.PlayerId} nickname={message.Nickname} tokenLen={message.Token?.Length ?? 0}");
            if (!_connectionConfig.SkillcadeHubIntegrated)
            {
                HandleNonHubAuth(connection, message);
                return;
            }
            
            try
            {
                Debug.Log($"[PlayerAuth] Hub: validating join token for connection={connection.ClientId} ");
                var payload = _sessionValidator.ValidateToken(message.Token);
                Debug.Log($"[PlayerAuth] Hub: token ok playerId={payload.PlayerId} sessionId={payload.GameSessionId} expires={payload.ExpiresAtUtc:O}");

                if (!string.Equals(payload.PlayerId, message.PlayerId, StringComparison.Ordinal))
                {
                    Debug.LogWarning($"[PlayerAuth] [PlayerDisconnect] Reject: token playerId={payload.PlayerId} != broadcast playerId={message.PlayerId}");
                    RejectAuth(connection, ServerDisconnectReason.AuthRejected, "Player identity does not match join token");
                    return;
                }

                bool isReconnect = _reconnectService.IsGraceActive(payload.PlayerId);
                int pendingGrace = _reconnectService.PendingInGameCount;
                Debug.Log($"[PlayerAuth] Hub: isGraceActive({payload.PlayerId})={isReconnect} pendingGraceSlots={pendingGrace}");

                if (!isReconnect)
                {
                    bool canAccept = _authenticatedPlayerDataStore.CanAcceptPlayer(payload.PlayerId, _connectionConfig.TargetPlayerCount);

                    if (!canAccept && _authenticatedPlayerDataStore.IsPlayerKnown(payload.PlayerId))
                    {
                        if (TryResolveStaleDuplicatePlayer(connection, payload.PlayerId, out bool readyForReconnect))
                        {
                            if (readyForReconnect)
                                isReconnect = true;
                            else
                                canAccept = _authenticatedPlayerDataStore.CanAcceptPlayer(payload.PlayerId, _connectionConfig.TargetPlayerCount);
                        }
                    }

                    Debug.Log($"[PlayerAuth] Hub: CanAcceptPlayer({payload.PlayerId})={canAccept} targetCount={_connectionConfig.TargetPlayerCount} isReconnect={isReconnect}");
                    if (!isReconnect && !canAccept)
                    {
                        bool duplicate = _authenticatedPlayerDataStore.IsPlayerKnown(payload.PlayerId);
                        var reason = duplicate ? ServerDisconnectReason.DuplicatePlayer : ServerDisconnectReason.LobbyFull;
                        var rejectMessage = duplicate
                            ? "Player is already connected to this match"
                            : "Match is full";
                        Debug.LogWarning($"[PlayerDisconnect] Reject connection={connection.ClientId} player={payload.PlayerId}: {rejectMessage}");
                        RejectAuth(connection, reason, rejectMessage);
                        return;
                    }
                }
                else
                {
                    Debug.Log($"[PlayerAuth] Hub: CanAcceptPlayer skipped — grace reconnect for {payload.PlayerId}");
                }

                if (isReconnect)
                {
                    Debug.Log($"[PlayerAuth] [PlayerReconnect] Hub reconnect attempt connection={connection.ClientId} player={payload.PlayerId} (see [reconnect-tokens] logs for join token comparison)");
                    if (!_reconnectService.TryAcceptReconnect(
                            connection.ClientId,
                            message.PlayerId,
                            message.Nickname,
                            message.Token,
                            payload,
                            hubIntegrated: true,
                            out _,
                            out var rejectReason))
                    {
                        Debug.LogWarning($"[PlayerAuth] [PlayerReconnect] Reject reconnect player={payload.PlayerId} connection={connection.ClientId}: {rejectReason ?? "unknown"}");
                        RejectAuth(connection, ServerDisconnectReason.ReconnectRejected, rejectReason ?? "Reconnect rejected");
                        return;
                    }
                    Debug.Log($"[PlayerAuth] [PlayerReconnect] Hub reconnect accepted player={payload.PlayerId} connection={connection.ClientId}");
                }

                string characterName = GetCharacterName(payload.PlayerId);
                _clientTokens[connection.ClientId] = payload;
                _reconnectService.RememberAuthToken(connection.ClientId, message.Token, payload);
                _authenticatedPlayerDataStore.Store(new AuthenticatedPlayerData
                {
                    ClientId = connection.ClientId,
                    PlayerId = payload.PlayerId,
                    Nickname = message.Nickname,
                    CharacterName = characterName
                });
                Debug.Log($"[PlayerAuth] Hub auth success connection={connection.ClientId} player={payload.PlayerId} nickname={message.Nickname} character={characterName} isReconnect={isReconnect}");
                SetAuthenticationResult(connection, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerAuth] [PlayerDisconnect] Token validation failed connection={connection.ClientId}: {e}");
                RejectAuth(connection, ServerDisconnectReason.AuthRejected, "Invalid or expired join token");
            }
#else
            OnAuthenticationResult?.Invoke(connection, true);
#endif
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void HandleNonHubAuth(NetworkConnection connection, TokenBroadcast message)
        {
            Debug.Log($"[PlayerAuth] Non-hub auth connection={connection.ClientId} pendingGrace={_reconnectService.PendingInGameCount}");
            // Without a signed token we cannot prove "same player" across reconnects.
            // If there is at least one active grace slot, hand it to this connection as a
            // best-effort reconnect; otherwise fall through to the regular target-count check.
            if (_reconnectService.PendingInGameCount > 0
                && _reconnectService.TryAcceptReconnect(
                    connection.ClientId,
                    message.PlayerId,
                    message.Nickname,
                    joinToken: null,
                    payload: null,
                    hubIntegrated: false,
                    out var slot,
                    out _))
            {
                _authenticatedPlayerDataStore.Store(new AuthenticatedPlayerData
                {
                    ClientId = connection.ClientId,
                    PlayerId = slot.PlayerId,
                    Nickname = slot.Nickname ?? $"Player_{connection.ClientId}",
                    CharacterName = slot.CharacterName
                });
                Debug.Log($"[PlayerAuth] [PlayerReconnect] Non-hub reconnect ok connection={connection.ClientId} slotPlayer={slot.PlayerId}");
                SetAuthenticationResult(connection, true);
                return;
            }

            string playerId = connection.ClientId.ToString();
            Debug.Log($"[PlayerAuth] Non-hub new player path connection={connection.ClientId} syntheticPlayerId={playerId}");
            if (!_authenticatedPlayerDataStore.CanAcceptPlayer(playerId, _connectionConfig.TargetPlayerCount))
            {
                Debug.LogWarning($"[PlayerDisconnect] Rejecting client {connection.ClientId}: target player count reached");
                RejectAuth(connection, ServerDisconnectReason.LobbyFull, "Match is full");
                return;
            }

            _authenticatedPlayerDataStore.Store(new AuthenticatedPlayerData
            {
                ClientId = connection.ClientId,
                PlayerId = playerId,
                Nickname = $"Player_{connection.ClientId}"
            });

            Debug.Log($"[PlayerAuth] Non-hub auth success connection={connection.ClientId}");
            SetAuthenticationResult(connection, true);
        }

        private string GetCharacterName(string playerId)
        {
            var characterByPlayerIds = _serverPayloadController.Payload?.CharacterByPlayerIds;
            if (characterByPlayerIds == null)
            {
                Debug.Log("[FishNetPlayerAuthenticator] No characters config");
                return null;
            }

            foreach (var characterContainer in characterByPlayerIds)
            {
                if (characterContainer.PlayerId == playerId)
                {
                    return characterContainer.CharacterName;
                }
            }

            Debug.Log($"[FishNetPlayerAuthenticator] Not found character name for player {playerId}");
            return null;
        }

        private void RejectAuth(NetworkConnection connection, ServerDisconnectReason reason, string message)
        {
            Debug.Log($"[PlayerAuth] RejectAuth connection={connection.ClientId} reason={reason} message={message}");
            _disconnectService.DisconnectAfterAuthFailure(connection, reason, message);
        }

        private void SetAuthenticationResult(NetworkConnection connection, bool result)
        {
            Debug.Log($"[PlayerAuth] SetAuthenticationResult connection={connection.ClientId} passed={result}");
            if (!result)
            {
                RejectAuth(connection, ServerDisconnectReason.AuthRejected, "Authentication rejected");
                return;
            }

            var response = new TokenResponseBroadcast { Passed = true };
            NetworkManager.ServerManager.Broadcast(connection, response, requireAuthenticated: false);
            Debug.Log($"[PlayerAuth] TokenResponseBroadcast sent connection={connection.ClientId} passed=true");
            OnAuthenticationResult?.Invoke(connection, true);
        }

        /// <summary>
        /// Same playerId reconnecting while the old FishNet connection is dead or still registered
        /// in auth store (race with OnPlayerRemoved). Starts grace when in-game, or clears stale auth.
        /// </summary>
        private bool TryResolveStaleDuplicatePlayer(
            NetworkConnection newConnection,
            string playerId,
            out bool readyForReconnect)
        {
            readyForReconnect = false;

            if (!_authenticatedPlayerDataStore.TryGetByPlayerId(playerId, out var existingAuth))
                return false;

            int oldClientId = existingAuth.ClientId;
            if (oldClientId == newConnection.ClientId)
                return false;

            Debug.Log($"[PlayerAuth] [PlayerReconnect] Stale duplicate player={playerId} oldConnection={oldClientId} newConnection={newConnection.ClientId} state={_gameStateMachine.CurrentStateType}");

            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (!PlayerMatchData.TryGetFromPlayer(playerData, out var match) || match.PlayerId != playerId)
                    continue;

                int connId = playerData.ServerConnectionClientId >= 0
                    ? playerData.ServerConnectionClientId
                    : playerData.OwnerId;
                bool graceStarted = _reconnectService.TryBeginGracePeriod(connId, playerData, _gameStateMachine.CurrentStateType);
                Debug.Log($"[PlayerAuth] [PlayerReconnect] TryBeginGracePeriod from stale duplicate: started={graceStarted}");
                break;
            }

            if (NetworkManager.ServerManager.Clients.TryGetValue(oldClientId, out var oldConn) && oldConn.IsActive)
            {
                Debug.Log($"[PlayerAuth] [PlayerReconnect] Kicking stale connection={oldClientId} for player={playerId}");
                oldConn.Kick(
                    KickReason.UnusualActivity,
                    LoggingType.Common,
                    $"Replaced by reconnect from connection {newConnection.ClientId}");
            }
            else
            {
                _authenticatedPlayerDataStore.RemoveClient(oldClientId);
            }

            readyForReconnect = _reconnectService.IsGraceActive(playerId);
            return true;
        }
#endif
    }
}
