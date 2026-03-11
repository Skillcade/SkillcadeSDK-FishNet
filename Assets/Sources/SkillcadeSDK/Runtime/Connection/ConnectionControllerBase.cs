using System;
using System.Collections;
using UnityEngine;
using VContainer.Unity;

namespace SkillcadeSDK.Connection
{
    public abstract class ConnectionControllerBase : MonoBehaviour, IInitializable, IConnectionController, IDisposable
    {
        public event Action<ConnectionState> OnStateChanged;
        public event Action<DisconnectionReason> OnDisconnected;
        
        public ConnectionState ConnectionState { get; private set; }
        public ConnectionData ActiveConfig { get; private set; }
        public abstract ITransportAdapter Transport { get; }

        private Coroutine _reconnectCoroutine;
        private WaitForSeconds _reconnectWait;

        // Which start request was received
        private bool _isStartedAsServer;
        private bool _isStartedAsClient;

        public virtual void Initialize()
        {
            Debug.Log("[ConnectionControllerBase] Initialize: subscribing to transport events");
            Transport.OnConnected += OnTransportConnected;
            Transport.OnDisconnected += OnTransportDisconnected;
        }

        public void StartServer(ConnectionData config)
        {
            if (ConnectionState != ConnectionState.Disconnected)
            {
                Debug.LogError($"[ConnectionControllerBase] Can't start server in state {ConnectionState}");
                return;
            }

            if (config == null)
            {
                Debug.LogError($"[ConnectionControllerBase] Can't start server cause config is null");
                return;
            }
            
            ActiveConfig = config;

            _isStartedAsServer = true;
            Debug.Log($"[ConnectionControllerBase] Starting server at port {config.ServerListenPort}");
            SetState(ConnectionState.Connecting);
            Transport.StartServer(config);
            Debug.Log("[ConnectionControllerBase] Transport.StartServer called");
        }

        public void StartClient(ConnectionData config)
        {
            if (ConnectionState != ConnectionState.Disconnected)
            {
                Debug.LogError($"[ConnectionControllerBase] Can't start client in state {ConnectionState}");
                return;
            }

            if (config == null)
            {
                Debug.LogError($"[ConnectionControllerBase] Can't start client cause config is null");
                return;
            }
            
            ActiveConfig = config;

            _isStartedAsClient = true;
            Debug.Log($"[ConnectionControllerBase] Starting client to {config.ServerAddress}:{config.ServerListenPort}");
            SetState(ConnectionState.Connecting);
            Transport.StartClient(config);
            Debug.Log("[ConnectionControllerBase] Transport.StartClient called");
        }

        public void StartSinglePlayer(ConnectionData config)
        {
            if (ConnectionState != ConnectionState.Disconnected)
            {
                Debug.LogError($"[ConnectionControllerBase] Can't start single player in state {ConnectionState}");
                return;
            }

            if (config == null)
            {
                Debug.LogError($"[ConnectionControllerBase] Can't start single player cause config is null");
                return;
            }
            
            ActiveConfig = config;

            SetState(ConnectionState.SinglePlayer);
            Debug.Log("[ConnectionControllerBase] Starting single player server");
            Transport.StartServer(config);
        }

        public void Disconnect()
        {
            if (ConnectionState ==  ConnectionState.Disconnected)
            {
                Debug.Log("[ConnectionControllerBase] Disconnect called but already disconnected, ignoring");
                return;
            }

            Debug.Log($"[ConnectionControllerBase] Disconnect requested (state={ConnectionState})");

            StopReconnect();

            SetState(ConnectionState.Disconnecting);
            Transport.Disconnect();

            OnDisconnected?.Invoke(DisconnectionReason.UserRequested);
            Debug.Log("[ConnectionControllerBase] Disconnect complete");
        }

        private void OnTransportConnected()
        {
            Debug.Log($"[ConnectionControllerBase] OnTransportConnected (IsServer={Transport.IsServer}, IsClient={Transport.IsClient}, state={ConnectionState})");

            if (Transport.IsServer && !Transport.IsClient)
            {
                if (ConnectionState == ConnectionState.SinglePlayer)
                {
                    Debug.Log("[ConnectionControllerBase] Server started in single player mode, starting local client");
                    Transport.StartClient(ActiveConfig);
                }
                else
                {
                    Debug.Log("[ConnectionControllerBase] Server started, entering Hosting state");
                    SetState(ConnectionState.Hosting);
                }
            }
            else if (Transport.IsClient)
            {
                Debug.Log($"[ConnectionControllerBase] Client connected successfully");
                if (ConnectionState != ConnectionState.SinglePlayer)
                    SetState(ConnectionState.Connected);
            }
        }

        private void OnTransportDisconnected(DisconnectionReason reason)
        {
            Debug.Log($"[ConnectionControllerBase] OnTransportDisconnected (reason={reason}, IsServer={Transport.IsServer}, IsClient={Transport.IsClient}, state={ConnectionState})");

            if (_isStartedAsServer)
            {
                Debug.Log("[ConnectionControllerBase] Server stopped");
                SetState(ConnectionState.Disconnected);
            }
            else if (_isStartedAsClient)
            {
                Debug.Log($"[ConnectionControllerBase] Client disconnected: reason={reason}");
                OnDisconnected?.Invoke(reason);
                SetState(ConnectionState.Disconnected);
                StartReconnect();
            }
        }

        private void StartReconnect()
        {
            Debug.Log($"[ConnectionControllerBase] StartReconnect (coroutineActive={_reconnectCoroutine != null})");

            if (_reconnectCoroutine != null)
            {
                Debug.Log("[ConnectionControllerBase] Reconnect coroutine already running, skipping");
                return;
            }

            Debug.Log($"[ConnectionControllerBase] Starting reconnect coroutine (delay={ActiveConfig.ReconnectDelay}s)");
            _reconnectCoroutine = StartCoroutine(Reconnect());
        }

        private void StopReconnect()
        {
            Debug.Log($"[ConnectionControllerBase] StopReconnect (coroutineActive={_reconnectCoroutine != null})");

            if (_reconnectCoroutine != null)
            {
                StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }
        }

        private IEnumerator Reconnect()
        {
            Debug.Log($"[ConnectionControllerBase] Reconnect coroutine started (delay={ActiveConfig.ReconnectDelay}s)");

            int attempt = 0;
            while (true)
            {
                attempt++;
                Debug.Log($"[ConnectionControllerBase] Reconnect attempt {attempt} (waiting {ActiveConfig.ReconnectDelay}s)");

                _reconnectWait ??= new WaitForSeconds(ActiveConfig.ReconnectDelay);
                yield return _reconnectWait;

                Debug.Log($"[ConnectionControllerBase] Delay elapsed, calling StartClient (state={ConnectionState})");
                StartClient(ActiveConfig);

                Debug.Log("[ConnectionControllerBase] Waiting for connection result...");
                while (ConnectionState == ConnectionState.Connecting)
                    yield return null;

                Debug.Log($"[ConnectionControllerBase] Connection attempt finished (state={ConnectionState})");

                if (ConnectionState == ConnectionState.Connected)
                {
                    Debug.Log("[ConnectionControllerBase] Reconnect successful, exiting reconnect loop");
                    break;
                }
            }

            if (ConnectionState != ConnectionState.Connected)
                Debug.Log($"[ConnectionControllerBase] Reconnect loop ended without success (state={ConnectionState})");

            _reconnectCoroutine = null;
        }

        private void SetState(ConnectionState state)
        {
            if (ConnectionState == state)
                return;

            Debug.Log($"[ConnectionControllerBase] Change connection state from {ConnectionState} to {state}");
            ConnectionState = state;
            OnStateChanged?.Invoke(state);
        }

        public virtual void Dispose()
        {
            Debug.Log("[ConnectionControllerBase] Dispose called");
            Disconnect();
        }
    }
}