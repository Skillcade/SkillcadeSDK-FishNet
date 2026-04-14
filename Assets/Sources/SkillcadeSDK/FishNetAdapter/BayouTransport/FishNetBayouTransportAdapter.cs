using System;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Bayou;
using SkillcadeSDK.Connection;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter.BayouTransport
{
    public class FishNetBayouTransportAdapter : ITransportAdapter, IInitializable, IDisposable
    {
        public event Action OnConnected;
        public event Action<DisconnectionReason> OnDisconnected;

        public bool IsConnected => IsServer || IsClient;
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }
        
        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly Bayou _bayouTransport;

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

        public void StartServer(ConnectionData config)
        {
            _bayouTransport.SetServerBindAddress("127.0.0.1", IPAddressType.IPv4);
            _bayouTransport.SetPort(config.ServerListenPort);
            _bayouTransport.SetUseWSS(false);
            _networkManager.ServerManager.StartConnection();
        }

        public void StartClient(ConnectionData config)
        {
            var port = config.UseEncryption ? config.WssConnectPort : config.ServerListenPort;
            _bayouTransport.SetUseWSS(config.UseEncryption);
            _bayouTransport.SetPort(port);
            _bayouTransport.SetClientAddress(config.ServerAddress);
            _networkManager.ClientManager.StartConnection();
        }

        public void Disconnect()
        {
            if (_networkManager.IsServerStarted)
                _networkManager.ServerManager.StopConnection(true);

            if (_networkManager.IsClientStarted)
                _networkManager.ClientManager.StopConnection();
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
                OnDisconnected?.Invoke(DisconnectionReason.ConnectionLost);
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
    }
}