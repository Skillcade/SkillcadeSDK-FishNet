using System;
using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Object;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkObject _prefab;

        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly IPlayersController _playersController;

        private Dictionary<int, NetworkObject> _spawnedPlayers;

        private void Start()
        {
            _spawnedPlayers = new Dictionary<int, NetworkObject>();
            _playersController.OnPlayerRemoved += OnPlayerRemoved;
        }

        public void SpawnAllInGamePlayers()
        {
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (!playerData.TryGetData(PlayerDataConst.InGame, out bool inGame) || !inGame)
                    continue;

                if (!_networkManager.ServerManager.Clients.TryGetValue(playerData.PlayerNetworkId, out var connection))
                {
                    Debug.LogError($"[PlayerSpawner] Can't get InGame player {playerData.PlayerNetworkId} connection");
                    continue;
                }

                if (_spawnedPlayers.ContainsKey(playerData.PlayerNetworkId))
                    continue;
                
                try
                {
                    var instance = _networkManager.ServerManager.InstantiateAndSpawn(_prefab, Vector3.zero, Quaternion.identity, connection);
                    _spawnedPlayers[playerData.PlayerNetworkId] = instance;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PlayerSpawner] Error spawning player {e}");
                }
            }
        }

        public void DespawnAllPlayers()
        {
            foreach (var entry in _spawnedPlayers)
            {
                if (entry.Value != null)
                    entry.Value.Despawn();
            }
            
            _spawnedPlayers.Clear();
        }

        private void OnPlayerRemoved(int clientId, IPlayerData data)
        {
            _spawnedPlayers.Remove(clientId);
        }
    }
}