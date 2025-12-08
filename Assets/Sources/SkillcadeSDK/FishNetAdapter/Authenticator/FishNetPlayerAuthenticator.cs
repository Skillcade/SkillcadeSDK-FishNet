using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using VContainer;

#if UNITY_SERVER
using SkillcadeSDK.ServerValidation;
#endif

namespace SkillcadeSDK.FishNetAdapter.Authenticator
{
    public class FishNetPlayerAuthenticator : FishNet.Authenticating.Authenticator
    {
        public override event Action<NetworkConnection, bool> OnAuthenticationResult;

        [Inject] private readonly WebBridge _webBridge;
        
#if UNITY_SERVER
        [Inject] private readonly SessionValidator _sessionValidator;
        [Inject] private readonly ServerPayloadController _serverPayloadController;
        
        public IReadOnlyDictionary<int, SessionTokenPayload> ClientTokens => _clientTokens;
        
        private readonly Dictionary<int, SessionTokenPayload> _clientTokens = new();
#endif

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);

            if (NetworkManager.IsClientStarted)
            {
                NetworkManager.ClientManager.RegisterBroadcast<TokenResponseBroadcast>(HandleTokenResponse);
                NetworkManager.ClientManager.OnClientConnectionState += HandleConnectionState;
            }

#if UNITY_SERVER
            if (NetworkManager.IsServerStarted)
            {
                NetworkManager.ServerManager.RegisterBroadcast<TokenBroadcast>(HandleToken);
            }
#endif
        }

        private void HandleConnectionState(ClientConnectionStateArgs stateArgs)
        {
            string token = string.Empty;
            if (_webBridge.UsePayload)
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

#if UNITY_SERVER
        private void HandleToken(NetworkConnection connection, TokenBroadcast message, Channel channel)
        {
            if (!_webBridge.UsePayload)
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
#endif
    }
}