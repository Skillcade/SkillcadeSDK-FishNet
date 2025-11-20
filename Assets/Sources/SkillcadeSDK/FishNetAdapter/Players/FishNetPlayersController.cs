using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using SkillcadeSDK.Common.Players;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    /// <summary>
    /// Контроллер управления данными всех игроков.
    /// Автоматически создает FishNetPlayerData для каждого подключающегося игрока.
    /// </summary>
    /// <remarks>
    /// Используйте PlayersHelper для упрощенных операций с коллекцией игроков.
    /// </remarks>
    public class FishNetPlayersController : NetworkBehaviour, IPlayersController
    {
        /// <summary>
        /// Событие вызывается когда игрок подключается
        /// </summary>
        public event IPlayersController.PlayerDataEventHandler OnPlayerAdded;

        /// <summary>
        /// Событие вызывается когда данные игрока изменяются
        /// </summary>
        public event IPlayersController.PlayerDataEventHandler OnPlayerDataUpdated;

        /// <summary>
        /// Событие вызывается когда игрок отключается
        /// </summary>
        public event IPlayersController.PlayerDataEventHandler OnPlayerRemoved;

        /// <summary>
        /// Получает сетевой ID локального игрока
        /// </summary>
        public int LocalPlayerId => NetworkManager.ClientManager.Connection.ClientId;

        [SerializeField] private FishNetPlayerData _playerDataPrefab;
        
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

            var instance = Instantiate(_playerDataPrefab);
            NetworkManager.ServerManager.Spawn(instance.GetComponent<NetworkObject>(), connection);
            RegisterPlayerData(connection.ClientId, instance);
        }

        public void RegisterPlayerData(int playerId, FishNetPlayerData playerData)
        {
            _players[playerId] = playerData;
            OnPlayerAdded?.Invoke(playerId, playerData);
            playerData.OnChanged += OnPlayerDataChanged;
        }

        public void UnregisterPlayerData(int playerId)
        {
            if (_players.Remove(playerId, out var data))
            {
                data.OnChanged -= OnPlayerDataChanged;
                OnPlayerRemoved?.Invoke(playerId, data);
            }
        }

        public bool TryGetPlayerData(int playerId, out IPlayerData data)
        {
            if (_players.TryGetValue(playerId, out var playerData))
            {
                data = playerData;
                return true;
            }

            data = null;
            return false;
        }

        public IEnumerable<IPlayerData> GetAllPlayersData()
        {
            foreach (var data in _players.Values)
            {
                yield return data;
            }
        }

        private void OnPlayerDataChanged(IPlayerData playerData)
        {
            OnPlayerDataUpdated?.Invoke(playerData.PlayerNetworkId, playerData);
        }
    }
}