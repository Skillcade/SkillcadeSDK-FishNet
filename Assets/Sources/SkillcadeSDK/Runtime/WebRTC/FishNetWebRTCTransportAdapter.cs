using System;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using SkillcadeSDK.Connection;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using FishyWebRTCTransport = FishNet.Transporting.FishyWebRTC.FishyWebRTC;

namespace SkillcadeSDK.FishNetAdapter
{
    /// <summary>
    /// WebRTC transport adapter for the Skillcade SDK. Bridges the SDK's ITransportAdapter
    /// contract with FishNet's transport layer using FishyWebRTC.
    /// 
    /// Supports both Multipass and direct FishyWebRTC setups.
    /// Client (WebGL): Configures FishyWebRTC signaling from the ConnectionData payload.
    /// Server (headless): Reads Edgegap ARBITRIUM_PORT env vars and configures transport ports.
    /// </summary>
    public class FishNetWebRTCTransportAdapter : ITransportAdapter, IInitializable, IDisposable
    {
        public event Action OnConnected;
        public event Action<DisconnectionReason> OnDisconnected;

        public bool IsConnected => IsServer || IsClient;
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }

        [Inject] private readonly NetworkManager _networkManager;

        private FishyWebRTCTransport _webRtcTransport;
        private Multipass _multipassTransport;

        public void Initialize()
        {
            _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            _networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;

            // Cache transport references
            _multipassTransport = _networkManager.TransportManager.Transport as Multipass;
            if (_multipassTransport != null)
            {
                foreach (var t in _multipassTransport.Transports)
                {
                    if (t is FishyWebRTCTransport webrtc)
                    {
                        _webRtcTransport = webrtc;
                        break;
                    }
                }
            }
            else
            {
                _webRtcTransport = _networkManager.TransportManager.Transport as FishyWebRTCTransport;
            }
        }

        public void Dispose()
        {
            if (_networkManager == null) return;

            if (_networkManager.ClientManager != null)
                _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;

            if (_networkManager.ServerManager != null)
            {
                _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
                _networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            }
        }

        private void OnClientConnectionState(ClientConnectionStateArgs stateArgs)
        {
            Debug.Log($"[FishNetWebRTCTransportAdapter] Client connection state: {stateArgs.ConnectionState}");
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
            Debug.Log($"[FishNetWebRTCTransportAdapter] Server connection state: {stateArgs.ConnectionState}");
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

        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs stateArgs)
        {
            Debug.Log($"[FishNetWebRTCTransportAdapter] Remote Connection State for Client {connection.ClientId}: {stateArgs.ConnectionState}");
        }

        public void StartClient(ConnectionData config)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Production Flow (Edgegap WebRTC)
            var port = config.WssConnectPort;
            
            Debug.Log($"[FishNetWebRTCTransportAdapter] WebGL: Using WebRTC to {config.ServerAddress}:{port}");

            if (_webRtcTransport != null)
            {
                if (_multipassTransport != null)
                {
                    _multipassTransport.SetClientTransport<FishyWebRTCTransport>();
                }
                
                _webRtcTransport.SetSignalingAddress(config.ServerAddress);
                _webRtcTransport.SetSignalingPort(port);
            }
            else
            {
                Debug.LogError("[FishNetWebRTCTransportAdapter] FishyWebRTC transport not found on NetworkManager!");
            }
            
            _networkManager.ClientManager.StartConnection();
#else
            // Development Flow (Windows Client / local iteration)
            Debug.Log($"[FishNetWebRTCTransportAdapter] Desktop: Mapping Tugboat to {config.ServerAddress}:{config.ServerListenPort}");

            if (_multipassTransport != null)
            {
                _multipassTransport.SetClientTransport<FishNet.Transporting.Tugboat.Tugboat>();
            }

            _networkManager.ClientManager.StartConnection(config.ServerAddress, config.ServerListenPort);
#endif
        }

        public void StartServer(ConnectionData config)
        {
            ushort tugboatPort = GetEdgegapPort("GAMEPORT", config.ServerListenPort);
            ushort webrtcPort = GetEdgegapPort("WEBRTCPORT", config.WssConnectPort);

            if (_multipassTransport != null)
            {
                foreach (var t in _multipassTransport.Transports)
                {
                    if (t.GetType().Name.Contains("Tugboat"))
                        t.SetPort(tugboatPort);
                    else if (t is FishyWebRTCTransport)
                        t.SetPort(webrtcPort);
                }
            }
            else
            {
                _networkManager.TransportManager.Transport.SetPort(tugboatPort);
            }

            Debug.Log($"[FishNetWebRTCTransportAdapter] Starting server — Tugboat:{tugboatPort}, WebRTC:{webrtcPort}");
            _networkManager.ServerManager.StartConnection();
        }

        /// <summary>
        /// Reads port from Edgegap environment variable.
        /// Edgegap injects: ARBITRIUM_PORT_{NAME}_INTERNAL
        /// </summary>
        private static ushort GetEdgegapPort(string portName, ushort fallback)
        {
            string envVar = $"ARBITRIUM_PORT_{portName.ToUpper()}_INTERNAL";
            string value = System.Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(value) && ushort.TryParse(value, out ushort port))
            {
                Debug.Log($"[Edgegap] Using port from {envVar}: {port}");
                return port;
            }
            return fallback;
        }

        public void Disconnect()
        {
            if (_networkManager.IsServerStarted)
                _networkManager.ServerManager.StopConnection(true);

            if (_networkManager.IsClientStarted)
                _networkManager.ClientManager.StopConnection();
        }
    }
}
