using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using SkillcadeSDK.Connection;
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
            Debug.Log($"[FishNetPlayerAuthenticator] Token validation passed: {message.Passed}");
        }

        private void HandleToken(NetworkConnection connection, TokenBroadcast message, Channel channel)
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (!_connectionConfig.SkillcadeHubIntegrated)
            {
                string playerId = connection.ClientId.ToString();
                if (!_authenticatedPlayerDataStore.CanAcceptPlayer(playerId, _connectionConfig.TargetPlayerCount))
                {
                    Debug.LogWarning($"[FishNetPlayerAuthenticator] Rejecting client {connection.ClientId}: target player count reached");
                    SetAuthenticationResult(connection, false);
                    return;
                }

                _authenticatedPlayerDataStore.Store(new AuthenticatedPlayerData
                {
                    ClientId = connection.ClientId,
                    PlayerId = playerId,
                    Nickname = $"Player_{connection.ClientId}"
                });
                
                SetAuthenticationResult(connection, true);
                return;
            }
            
            try
            {
                var payload = _sessionValidator.ValidateToken(message.Token);
                if (!string.Equals(payload.PlayerId, message.PlayerId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Auth player id does not match signed join token.");

                if (!_authenticatedPlayerDataStore.CanAcceptPlayer(payload.PlayerId, _connectionConfig.TargetPlayerCount))
                {
                    Debug.LogWarning($"[FishNetPlayerAuthenticator] Rejecting player {connection.ClientId} - {payload.PlayerId}: target player count reached");
                    SetAuthenticationResult(connection, false);
                    return;
                }

                _clientTokens[connection.ClientId] = payload;
                _authenticatedPlayerDataStore.Store(new AuthenticatedPlayerData
                {
                    ClientId = connection.ClientId,
                    PlayerId = payload.PlayerId,
                    Nickname = message.Nickname,
                    CharacterName = GetCharacterName(payload.PlayerId)
                });
                SetAuthenticationResult(connection, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FishNetPlayerAuthenticator] Error on validating client token: {e}");
                SetAuthenticationResult(connection, false);
            }
#else
            SetAuthenticationResult(connection, true);
#endif
        }

#if UNITY_SERVER || UNITY_EDITOR
        private string GetCharacterName(string playerId)
        {
            Debug.Log($"[FishNetPlayerAuthenticator] Requesting character name for {playerId}");
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
                    Debug.Log($"[FishNetPlayerAuthenticator] got {characterContainer.CharacterName}");
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