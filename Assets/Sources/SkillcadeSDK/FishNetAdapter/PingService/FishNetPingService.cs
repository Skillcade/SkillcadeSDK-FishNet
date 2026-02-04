using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using SkillcadeSDK.Connection;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter.PingService
{
    public class FishNetPingService : IInitializable, ITickable, IDisposable
    {
        private const float PingIntervalSeconds = 1f;
        
        private struct PlayerRequestInfo
        {
            public bool Requested;
            public double RequestedTime;
        }
        
        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly IConnectionController _connectionController;
        [Inject] private readonly FishNetPlayersController _playersController;

        private bool _subscribed;
        private Dictionary<int, PlayerRequestInfo> _playerRequests;
        
        public void Initialize()
        {
            _connectionController.OnStateChanged += OnConnectionStateChanged;
            if (_connectionController.ConnectionState.IsConnectedOrHosting())
            {
                Subscribe();
                
                if (_connectionController.ConnectionState == ConnectionState.Hosting)
                    _playerRequests = new Dictionary<int, PlayerRequestInfo>();
            }
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            if (state.IsConnectedOrHosting())
            {
                Subscribe();
                
                if (state == ConnectionState.Hosting)
                    _playerRequests = new Dictionary<int, PlayerRequestInfo>();
            }
            else
            {
                Unsubscribe();
                _playerRequests = null;
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;
            
            if (_networkManager.ServerManager != null)
                _networkManager.ServerManager.RegisterBroadcast<PingResponseBroadcast>(OnPingResponse);
                
            if (_networkManager.ClientManager != null)
                _networkManager.ClientManager.RegisterBroadcast<PingRequestBroadcast>(OnPingRequest);
                
            _subscribed = true;
        }

        public void Dispose()
        {
            _connectionController.OnStateChanged -= OnConnectionStateChanged;
            _playerRequests = null;
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;
            
            if (_networkManager.ServerManager != null)
                _networkManager.ServerManager.UnregisterBroadcast<PingResponseBroadcast>(OnPingResponse);
            
            if (_networkManager.ClientManager != null)
                _networkManager.ClientManager.UnregisterBroadcast<PingRequestBroadcast>(OnPingRequest);
            
            _subscribed = false;
        }

        public void Tick()
        {
            if (!_networkManager.IsServerStarted || _playerRequests == null)
                return;

            if (_networkManager.ServerManager.Clients.Count == 0)
                return;

            foreach (var client in _networkManager.ServerManager.Clients)
            {
                if (!_playerRequests.TryGetValue(client.Key, out var playerRequestInfo))
                {
                    playerRequestInfo = new PlayerRequestInfo
                    {
                        Requested = false
                    };
                }

                if (playerRequestInfo.Requested)
                {
                    var passedTime = Time.timeAsDouble - playerRequestInfo.RequestedTime;
                    if (passedTime > PingIntervalSeconds)
                        playerRequestInfo.Requested = false;
                }

                if (!playerRequestInfo.Requested)
                {
                    _networkManager.ServerManager.Broadcast(client.Value, new PingRequestBroadcast());
                    playerRequestInfo.Requested = true;
                    playerRequestInfo.RequestedTime = Time.timeAsDouble;
                }

                _playerRequests[client.Key] = playerRequestInfo;
            }
        }

        private void OnPingRequest(PingRequestBroadcast broadcast, Channel channel)
        {
            if (_networkManager.IsClientStarted)
                _networkManager.ClientManager.Broadcast(new PingResponseBroadcast());
        }

        private void OnPingResponse(NetworkConnection connection, PingResponseBroadcast broadcast, Channel channel)
        {
            if (!_networkManager.IsServerStarted || _playerRequests == null)
                return;
            
            if (!_playerRequests.TryGetValue(connection.ClientId, out var playerRequestInfo))
                return;
            
            var currentTime = Time.timeAsDouble;
            var ping = currentTime - playerRequestInfo.RequestedTime;
            var pingInMs = Mathf.CeilToInt((float)ping * 1000f);

            if (_playersController.TryGetPlayerData(connection.ClientId, out var playerData))
            {
                if (!PlayerPingData.TryGetFromPlayer(playerData, out var pingData))
                    pingData = new PlayerPingData();
                
                pingData.PingInMs = pingInMs;
                pingData.SetToPlayer(playerData);
            }
            
            playerRequestInfo.Requested = false;
        }
    }
}