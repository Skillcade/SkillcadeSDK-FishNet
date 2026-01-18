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
                SetAuthenticationResult(connection, true);
                return;
            }
            
            try
            {
                var payload = _sessionValidator.ValidateToken(message.Token);
                _clientTokens[connection.ClientId] = payload;
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