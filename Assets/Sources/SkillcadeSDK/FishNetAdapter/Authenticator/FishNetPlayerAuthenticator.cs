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
        }

        private void HandleConnectionState(ClientConnectionStateArgs stateArgs)
        {
            if (stateArgs.ConnectionState != LocalConnectionState.Started)
                return;
            
            string token = "empty";
            if (_connectionConfig.SkillcadeHubIntegrated)
            {
                if (_webBridge.Payload == null || string.IsNullOrEmpty(_webBridge.Payload.JoinToken))
                    Debug.LogError("[FishNetPlayerAuthenticator] No payload on connection");
                else
                    token = _webBridge.Payload.JoinToken;
            }

            var message = new TokenBroadcast
            {
                Token = token,
                PlayerId = _webBridge.Payload?.PlayerId,
                Nickname = _webBridge.Payload?.Nickname,
            };
            NetworkManager.ClientManager.Broadcast(message);
        }

        private void HandleTokenResponse(TokenResponseBroadcast message, Channel channel)
        {
            if (!message.Passed)
            {
                Debug.Log("[PlayerDisconnect] Authentication rejected by server (TokenResponse Passed=false)");
                _connectionController.NotifyPermanentDisconnect(DisconnectionReason.Kicked);
                return;
            }

            Debug.Log("[FishNetPlayerAuthenticator] Token validation passed");
        }

        private void HandleServerKick(ServerKickBroadcast message, Channel channel)
        {
            Debug.Log("[PlayerDisconnect] Server kick broadcast received");
            _connectionController.NotifyPermanentDisconnect(DisconnectionReason.Kicked);
        }

        private void HandleToken(NetworkConnection connection, TokenBroadcast message, Channel channel)
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (!_connectionConfig.SkillcadeHubIntegrated)
            {
                HandleNonHubAuth(connection, message);
                return;
            }
            
            try
            {
                var payload = _sessionValidator.ValidateToken(message.Token);
                if (!string.Equals(payload.PlayerId, message.PlayerId, StringComparison.Ordinal))
                {
                    Debug.Log($"[PlayerDisconnect] Rejecting player {payload.PlayerId} - auth player id does not match signed join token.");
                    SetAuthenticationResult(connection, false);
                    return;
                }

                bool isReconnect = _reconnectService.IsGraceActive(payload.PlayerId);
                if (!isReconnect && !_authenticatedPlayerDataStore.CanAcceptPlayer(payload.PlayerId, _connectionConfig.TargetPlayerCount))
                {
                    Debug.LogWarning($"[PlayerDisconnect] Rejecting player {connection.ClientId} - {payload.PlayerId}: target player count reached");
                    SetAuthenticationResult(connection, false);
                    return;
                }

                if (isReconnect)
                {
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
                        Debug.LogWarning($"[FishNetPlayerAuthenticator] [PlayerReconnect] Rejecting reconnect for player {payload.PlayerId}: {rejectReason}");
                        SetAuthenticationResult(connection, false);
                        return;
                    }
                    Debug.Log($"[FishNetPlayerAuthenticator] [PlayerReconnect] Reconnect accepted for player {payload.PlayerId} on new connection {connection.ClientId}");
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
                Debug.Log($"[FishNetPlayerAuthenticator] Authenticate player {connection.ClientId} - {payload.PlayerId} - {message.Nickname}, character: {characterName}, reconnect={isReconnect}");
                SetAuthenticationResult(connection, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerDisconnect] Error on validating client token: {e}");
                SetAuthenticationResult(connection, false);
            }
#else
            SetAuthenticationResult(connection, true);
#endif
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void HandleNonHubAuth(NetworkConnection connection, TokenBroadcast message)
        {
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
                Debug.Log($"[FishNetPlayerAuthenticator] [PlayerReconnect] Non-hub reconnect: connection={connection.ClientId} took grace slot for player {slot.PlayerId}");
                SetAuthenticationResult(connection, true);
                return;
            }

            string playerId = connection.ClientId.ToString();
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

            Debug.Log($"[FishNetPlayerAuthenticator] Authenticate player {connection.ClientId}");
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
            var response = new TokenResponseBroadcast
            {
                Passed = result
            };
            NetworkManager.ServerManager.Broadcast(connection, response);
            OnAuthenticationResult?.Invoke(connection, result);
        }
    }
}
