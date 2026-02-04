using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.Connection;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    public class FishNetPlayersController : NetworkBehaviour, IPlayersController<FishNetPlayerData, IDataContainer>
    {
        public event IPlayersController<FishNetPlayerData, IDataContainer>.PlayerDataEventHandler OnPlayerAdded;
        public event IPlayersController<FishNetPlayerData, IDataContainer>.PlayerDataEventHandler OnPlayerDataUpdated;
        public event IPlayersController<FishNetPlayerData, IDataContainer>.PlayerDataEventHandler OnPlayerRemoved;
        
        public int LocalPlayerId => NetworkManager.ClientManager.Connection.ClientId;

        [SerializeField] private FishNetPlayerData _playerDataPrefab;

        [Inject] private readonly WebBridge _webBridge;
        [Inject] private readonly ConnectionConfig _connectionConfig;
        
        private readonly Dictionary<int, FishNetPlayerData> _players = new();
        
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (IsServerInitialized)
                NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (IsServerInitialized)
                NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs stateArgs)
        {
            if (stateArgs.ConnectionState != RemoteConnectionState.Started)
                return;

            var instance = NetworkManager.ServerManager.InstantiateAndSpawn(_playerDataPrefab, Vector3.zero, Quaternion.identity, connection);
            RegisterPlayerData(connection.ClientId, instance);
        }

        public void RegisterPlayerData(int playerId, FishNetPlayerData playerData)
        {
            if (!_players.TryAdd(playerId, playerData))
                return;
            
            OnPlayerAdded?.Invoke(playerId, playerData);
            playerData.OnChanged += OnPlayerDataChanged;

            if (IsClientInitialized && playerId == LocalPlayerId)
            {
                if (_connectionConfig.SkillcadeHubIntegrated)
                {
                    WaitForPayloadAndSetMatchData(destroyCancellationToken);
                }
                else
                {
                    var data = new PlayerMatchData
                    {
                        Nickname = $"Player_{LocalPlayerId}",
                        PlayerId = ""
                    };
                    data.SetToPlayer(playerData);
                }
            }
        }

        public void UnregisterPlayerData(int playerId)
        {
            if (_players.Remove(playerId, out var data))
            {
                data.OnChanged -= OnPlayerDataChanged;
                OnPlayerRemoved?.Invoke(playerId, data);
            }
        }

        public bool TryGetPlayerData(int playerId, out FishNetPlayerData data)
        {
            if (_players.TryGetValue(playerId, out var playerData))
            {
                data = playerData;
                return true;
            }

            data = null;
            return false;
        }

        public IEnumerable<FishNetPlayerData> GetAllPlayersData() => _players.Values;

        private void OnPlayerDataChanged(FishNetPlayerData playerData)
        {
            OnPlayerDataUpdated?.Invoke(playerData.PlayerNetworkId, playerData);
        }

        private async Task WaitForPayloadAndSetMatchData(CancellationToken cancellationToken)
        {
            while (_webBridge.Payload == null)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!_players.TryGetValue(LocalPlayerId, out var playerData))
                return;

            var data = new PlayerMatchData
            {
                Nickname = _webBridge.Payload.Nickname,
                PlayerId = _webBridge.Payload.PlayerId
            };
            data.SetToPlayer(playerData);
        }
    }
}