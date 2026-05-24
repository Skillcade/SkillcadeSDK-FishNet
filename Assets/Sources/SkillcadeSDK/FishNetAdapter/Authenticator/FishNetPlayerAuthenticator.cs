using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using SkillcadeSDK.Connection;
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
        
        public IReadOnlyDictionary<int, SessionTokenPayload> ClientTokens => _clientTokens;
        
        private readonly Dictionary<int, SessionTokenPayload> _clientTokens = new();
#endif

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);
            
            NetworkManager.ClientManager.RegisterBroadcast<TokenResponseBroadcast>(HandleTokenResponse);
            NetworkManager.ClientManager.RegisterBroadcast<ServerKickBroadcast>(HandleServerKick);
            NetworkManager.ClientManager.OnClientConnectionState += HandleConnectionState;

            NetworkManager.ServerManager.RegisterBroadcast<TokenBroadcast>(HandleToken, false);
            Debug.Log($"[PlayerAuth] Authenticator initialized (hubIntegrated={_connectionConfig.SkillcadeHubIntegrated}, targetPlayers={_connectionConfig.TargetPlayerCount})");
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
                Debug.Log("[PlayerAuth] [PlayerDisconnect] TokenResponse Passed=false — permanent disconnect");
                _connectionController.NotifyPermanentDisconnect(DisconnectionReason.Kicked);
                return;
            }

            Debug.Log("[PlayerAuth] TokenResponse Passed=true — authentication succeeded on client");
        }

        private void HandleServerKick(ServerKickBroadcast message, Channel channel)
        {
            Debug.Log("[PlayerDisconnect] ServerKickBroadcast received — permanent disconnect");
            _connectionController.NotifyPermanentDisconnect(DisconnectionReason.Kicked);
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
                Debug.Log($"[PlayerAuth] Hub: validating join token for connection={connection.ClientId}");
                var payload = _sessionValidator.ValidateToken(message.Token);
                Debug.Log($"[PlayerAuth] Hub: token ok playerId={payload.PlayerId} sessionId={payload.GameSessionId} expires={payload.ExpiresAtUtc:O}");

                if (!string.Equals(payload.PlayerId, message.PlayerId, StringComparison.Ordinal))
                {
                    Debug.LogWarning($"[PlayerAuth] [PlayerDisconnect] Reject: token playerId={payload.PlayerId} != broadcast playerId={message.PlayerId}");
                    SetAuthenticationResult(connection, false);
                    return;
                }

                bool isReconnect = _reconnectService.IsGraceActive(payload.PlayerId);
                int pendingGrace = _reconnectService.PendingInGameCount;
                Debug.Log($"[PlayerAuth] Hub: isGraceActive({payload.PlayerId})={isReconnect} pendingGraceSlots={pendingGrace}");

                if (!isReconnect)
                {
                    bool canAccept = _authenticatedPlayerDataStore.CanAcceptPlayer(payload.PlayerId, _connectionConfig.TargetPlayerCount);
                    Debug.Log($"[PlayerAuth] Hub: CanAcceptPlayer({payload.PlayerId})={canAccept} targetCount={_connectionConfig.TargetPlayerCount}");
                    if (!canAccept)
                    {
                        Debug.LogWarning($"[PlayerDisconnect] Reject connection={connection.ClientId} player={payload.PlayerId}: lobby full or duplicate");
                        SetAuthenticationResult(connection, false);
                        return;
                    }
                }

                if (isReconnect)
                {
                    Debug.Log($"[PlayerAuth] [PlayerReconnect] Hub reconnect attempt connection={connection.ClientId} player={payload.PlayerId}");
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
                        SetAuthenticationResult(connection, false);
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
                SetAuthenticationResult(connection, false);
            }
#else
            SetAuthenticationResult(connection, true);
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
                SetAuthenticationResult(connection, false);
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
#endif

        private void SetAuthenticationResult(NetworkConnection connection, bool result)
        {
            Debug.Log($"[PlayerAuth] SetAuthenticationResult connection={connection.ClientId} passed={result}");
            var response = new TokenResponseBroadcast
            {
                Passed = result
            };
            NetworkManager.ServerManager.Broadcast(connection, response);
            Debug.Log($"[PlayerAuth] TokenResponseBroadcast sent connection={connection.ClientId} passed={result}");
            OnAuthenticationResult?.Invoke(connection, result);
        }
    }
}
