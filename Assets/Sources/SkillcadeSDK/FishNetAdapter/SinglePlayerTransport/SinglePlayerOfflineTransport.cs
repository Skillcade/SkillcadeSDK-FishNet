using System;
using System.Collections.Generic;
using FishNet.Transporting;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter
{
    [AddComponentMenu("FishNet/Transport/SinglePlayerOfflineTransport")]
    public class SinglePlayerOfflineTransport : Transport
    {
        #region Serialized
        
        [SerializeField]
        private int _mtu = 1200;
        
        #endregion

        #region Private
        
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        
        // Фиксированный ID для локального клиента (как в UnityTransport ClientHost)
        private const int LocalConnectionId = 1;
        private const ulong LocalTransportId = 0;
        
        // Маппинг (для совместимости с FishNet API)
        private readonly Dictionary<int, ulong> _connectionIdToTransportId = new();
        private readonly Dictionary<ulong, int> _transportIdToConnectionId = new();
        
        // In-memory очереди вместо сокетов
        private readonly Queue<QueuedMessage> _clientToServerQueue = new();
        private readonly Queue<QueuedMessage> _serverToClientQueue = new();
        
        private readonly struct QueuedMessage
        {
            public readonly Channel Channel;
            public readonly ArraySegment<byte> Data;

            public QueuedMessage(Channel channel, ArraySegment<byte> segment)
            {
                Channel = channel;
                // Копируем данные — FishNet переиспользует буферы
                var copy = new byte[segment.Count];
                Buffer.BlockCopy(segment.Array!, segment.Offset, copy, 0, segment.Count);
                Data = new ArraySegment<byte>(copy);
            }
        }
        
        #endregion

        #region Events
        
        public override event Action<ClientConnectionStateArgs> OnClientConnectionState;
        public override event Action<ServerConnectionStateArgs> OnServerConnectionState;
        public override event Action<RemoteConnectionStateArgs> OnRemoteConnectionState;
        public override event Action<ClientReceivedDataArgs> OnClientReceivedData;
        public override event Action<ServerReceivedDataArgs> OnServerReceivedData;
        
        #endregion

        #region Initialization

        public override void Shutdown()
        {
            StopConnection(false);
            StopConnection(true);
        }
        
        #endregion

        #region Connection State
        
        public override LocalConnectionState GetConnectionState(bool server)
        {
            return server ? _serverState : _clientState;
        }

        public override RemoteConnectionState GetConnectionState(int connectionId)
        {
            if (connectionId == LocalConnectionId && 
                _serverState == LocalConnectionState.Started &&
                _clientState == LocalConnectionState.Started)
            {
                return RemoteConnectionState.Started;
            }
            return RemoteConnectionState.Stopped;
        }

        public override string GetConnectionAddress(int connectionId)
        {
            return "localhost";
        }
        
        #endregion

        #region Start / Stop Connection
        
        public override bool StartConnection(bool server)
        {
            if (server)
                return StartServer();
            else
                return StartClient();
        }

        public override bool StopConnection(bool server)
        {
            if (server)
                return StopServer();
            else
                return StopClient();
        }

        public override bool StopConnection(int connectionId, bool immediately)
        {
            if (connectionId == LocalConnectionId)
            {
                return StopClient();
            }
            return false;
        }

        private bool StartServer()
        {
            if (_serverState != LocalConnectionState.Stopped)
                return false;

            SetServerState(LocalConnectionState.Starting);
            SetServerState(LocalConnectionState.Started);
            
            // Если клиент уже ждёт подключения — подключаем его
            if (_clientState == LocalConnectionState.Starting)
            {
                CompleteClientConnection();
            }
            
            return true;
        }

        private bool StopServer()
        {
            if (_serverState == LocalConnectionState.Stopped)
                return false;

            // Сначала отключаем клиента
            if (_clientState == LocalConnectionState.Started)
            {
                HandleRemoteConnectionState(new RemoteConnectionStateArgs(
                    RemoteConnectionState.Stopped, LocalConnectionId, Index));
                StopClient();
            }

            SetServerState(LocalConnectionState.Stopping);
            
            ClearQueues();
            ClearMappings();
            
            SetServerState(LocalConnectionState.Stopped);
            return true;
        }

        private bool StartClient()
        {
            if (_clientState != LocalConnectionState.Stopped)
                return false;

            SetClientState(LocalConnectionState.Starting);
            
            // Если сервер уже запущен — сразу подключаемся
            if (_serverState == LocalConnectionState.Started)
            {
                CompleteClientConnection();
            }
            // Иначе ждём запуска сервера (StartServer вызовет CompleteClientConnection)
            
            return true;
        }

        private void CompleteClientConnection()
        {
            // Регистрируем маппинг
            _connectionIdToTransportId[LocalConnectionId] = LocalTransportId;
            _transportIdToConnectionId[LocalTransportId] = LocalConnectionId;
            
            // Уведомляем сервер о новом подключении
            HandleRemoteConnectionState(new RemoteConnectionStateArgs(
                RemoteConnectionState.Started, LocalConnectionId, Index));
            
            // Завершаем подключение клиента
            SetClientState(LocalConnectionState.Started);
        }

        private bool StopClient()
        {
            if (_clientState == LocalConnectionState.Stopped)
                return false;

            bool wasConnected = _clientState == LocalConnectionState.Started;
            
            SetClientState(LocalConnectionState.Stopping);
            
            // Уведомляем сервер об отключении (если сервер ещё работает и клиент был подключён)
            if (_serverState == LocalConnectionState.Started && wasConnected)
            {
                HandleRemoteConnectionState(new RemoteConnectionStateArgs(
                    RemoteConnectionState.Stopped, LocalConnectionId, Index));
            }
            
            ClearQueues();
            ClearMappings();
            
            SetClientState(LocalConnectionState.Stopped);
            return true;
        }

        private void SetServerState(LocalConnectionState state)
        {
            if (_serverState == state) return;
            _serverState = state;
            HandleServerConnectionState(new ServerConnectionStateArgs(state, Index));
        }

        private void SetClientState(LocalConnectionState state)
        {
            if (_clientState == state) return;
            _clientState = state;
            HandleClientConnectionState(new ClientConnectionStateArgs(state, Index));
        }

        private void ClearQueues()
        {
            _clientToServerQueue.Clear();
            _serverToClientQueue.Clear();
        }

        private void ClearMappings()
        {
            _connectionIdToTransportId.Clear();
            _transportIdToConnectionId.Clear();
        }
        
        #endregion

        #region Send
        
        public override void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            if (_clientState != LocalConnectionState.Started) return;
            if (_serverState != LocalConnectionState.Started) return;
            
            _clientToServerQueue.Enqueue(new QueuedMessage((Channel)channelId, segment));
        }

        public override void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
        {
            if (_serverState != LocalConnectionState.Started) return;
            if (connectionId != LocalConnectionId) return;
            if (_clientState != LocalConnectionState.Started) return;
            
            _serverToClientQueue.Enqueue(new QueuedMessage((Channel)channelId, segment));
        }
        
        #endregion

        #region Iteration
        
        public override void IterateIncoming(bool server)
        {
            if (server)
                ProcessServerIncoming();
            else
                ProcessClientIncoming();
        }

        public override void IterateOutgoing(bool server)
        {
            // In-memory — отправка мгновенная, ничего делать не нужно
        }

        private void ProcessServerIncoming()
        {
            if (_serverState != LocalConnectionState.Started) return;
            
            while (_clientToServerQueue.Count > 0)
            {
                var msg = _clientToServerQueue.Dequeue();
                HandleServerReceivedDataArgs(new ServerReceivedDataArgs(
                    msg.Data, 
                    msg.Channel, 
                    LocalConnectionId, 
                    Index));
            }
        }

        private void ProcessClientIncoming()
        {
            if (_clientState != LocalConnectionState.Started) return;
            
            while (_serverToClientQueue.Count > 0)
            {
                var msg = _serverToClientQueue.Dequeue();
                HandleClientReceivedDataArgs(new ClientReceivedDataArgs(
                    msg.Data, 
                    msg.Channel, 
                    Index));
            }
        }
        
        #endregion

        #region Event Handlers
        
        public override void HandleClientConnectionState(ClientConnectionStateArgs connectionStateArgs)
        {
            OnClientConnectionState?.Invoke(connectionStateArgs);
        }

        public override void HandleServerConnectionState(ServerConnectionStateArgs connectionStateArgs)
        {
            OnServerConnectionState?.Invoke(connectionStateArgs);
        }

        public override void HandleRemoteConnectionState(RemoteConnectionStateArgs connectionStateArgs)
        {
            OnRemoteConnectionState?.Invoke(connectionStateArgs);
        }

        public override void HandleClientReceivedDataArgs(ClientReceivedDataArgs receivedDataArgs)
        {
            OnClientReceivedData?.Invoke(receivedDataArgs);
        }

        public override void HandleServerReceivedDataArgs(ServerReceivedDataArgs receivedDataArgs)
        {
            OnServerReceivedData?.Invoke(receivedDataArgs);
        }
        
        #endregion

        #region Configuration
        
        public override int GetMTU(byte channel) => _mtu;
        
        public override void SetClientAddress(string address) { }
        public override string GetClientAddress() => "localhost";
        
        public override void SetPort(ushort port) { }
        public override ushort GetPort() => 0;
        
        public override void SetMaximumClients(int value) { }
        public override int GetMaximumClients() => 1;
        
        #endregion
    }
}