using System;
using FishNet.Managing;
using FishNet.Transporting;
using SkillcadeSDK.Connection;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter
{
    public class FishNetSinglePlayerTransportAdapter : ITransportAdapter, IInitializable, IDisposable
    {
        public event Action OnConnected;
        public event Action<DisconnectionReason> OnDisconnected;

        public bool IsConnected => IsServer || IsClient;
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }

        [Inject] private NetworkManager _networkManager;
        [Inject] private SinglePlayerOfflineTransport _transport;

        public void Initialize()
        {
            _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        }

        public void Dispose()
        {
            _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
            _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        }

        private void OnClientConnectionState(ClientConnectionStateArgs stateArgs)
        {
            Debug.Log($"[FishNetTransportAdapter] Client connection state: {stateArgs.ConnectionState}");
            if (stateArgs.ConnectionState == LocalConnectionState.Started)
            {
                IsClient = true;
                OnConnected?.Invoke();
            }
            else if (stateArgs.ConnectionState == LocalConnectionState.Stopped)
            {
                IsClient = false;
                OnDisconnected?.Invoke(GetDisconnectReason());
            }
        }

        private void OnServerConnectionState(ServerConnectionStateArgs stateArgs)
        {
            Debug.Log($"[FishNetTransportAdapter] Server connection state: {stateArgs.ConnectionState}");
            if (stateArgs.ConnectionState == LocalConnectionState.Started)
            {
                IsServer = true;
                OnConnected?.Invoke();
            }
            else if (stateArgs.ConnectionState == LocalConnectionState.Stopped)
            {
                IsServer = false;
                OnDisconnected?.Invoke(DisconnectionReason.ServerStopped);
            }
        }

        public void StartClient(ConnectionData config)
        {
            var port = config.UseEncryption ? config.WssConnectPort : config.ServerListenPort;

            Debug.Log($"[FishNetTransportAdapter] Connecting to {config.ServerAddress}:{port}, secure: {config.UseEncryption}");
            
            _transport.SetPort(port);
            _transport.SetClientAddress(config.ServerAddress);
            _networkManager.ClientManager.StartConnection();
        }

        public void StartServer(ConnectionData config)
        {
            Debug.Log($"[FishNetTransportAdapter] Starting server at port {config.ServerListenPort}");
            _transport.SetPort(config.ServerListenPort);
            
            _networkManager.ServerManager.StartConnection();
        }

        public void Disconnect()
        {
            if (_networkManager.IsServerStarted)
                _networkManager.ServerManager.StopConnection(true);

            if (_networkManager.IsClientStarted)
                _networkManager.ClientManager.StopConnection();
        }

        private DisconnectionReason GetDisconnectReason()
        {
            return DisconnectionReason.ConnectionLost; // TODO: check last state and set reason
        }
    }
}