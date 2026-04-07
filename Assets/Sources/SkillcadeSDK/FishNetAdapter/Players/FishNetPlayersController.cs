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

#if UNITY_SERVER || UNITY_EDITOR
using SkillcadeSDK.ServerValidation;
#endif

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
        
#if UNITY_SERVER || UNITY_EDITOR
        [Inject] private readonly ServerPayloadController _serverPayloadController;
#endif
        
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
            Debug.Log($"[FishNetPlayersController] Register player {playerId} data");
            if (!_players.TryAdd(playerId, playerData))
            {
                Debug.Log($"[FishNetPlayersController] Data for player {playerId} already exists");
                return;
            }
            
            OnPlayerAdded?.Invoke(playerId, playerData);
            playerData.OnChanged += OnPlayerDataChanged;

            if (!IsClientInitialized || playerId != LocalPlayerId)
                return;

            Debug.Log("[FishNetPlayersController] Process local player data");
            if (_connectionConfig.SkillcadeHubIntegrated)
            {
                Debug.Log("[FishNetPlayersController] Waiting for payload to set PlayerMatchData data");
                WaitForPayloadAndSetMatchData(destroyCancellationToken).DoNotAwait();
            }
            else
            {
                Debug.Log("[FishNetPlayersController] Set default PlayerMatchData data");
                var data = new PlayerMatchData
                {
                    Nickname = $"Player_{LocalPlayerId}",
                    PlayerId = ""
                };
                data.SetToPlayer(playerData);
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

        public bool TryGetLocalPlayerData(out FishNetPlayerData data)
        {
            data = null;
            if (NetworkManager == null || NetworkManager.ClientManager == null)
                return false;

            if (NetworkManager.ClientManager.Connection == null)
                return false;
            
            return TryGetPlayerData(NetworkManager.ClientManager.Connection.ClientId, out data);
        }

        public IEnumerable<FishNetPlayerData> GetAllPlayersData() => _players.Values;

        private void OnPlayerDataChanged(FishNetPlayerData playerData)
        {
            Debug.Log($"[FishNetPlayersController] Player {playerData.PlayerNetworkId} data changed");
            OnPlayerDataUpdated?.Invoke(playerData.PlayerNetworkId, playerData);
            
#if UNITY_SERVER || UNITY_EDITOR
            Debug.Log("[FishNetPlayersController] Trying to get character");
            if (!IsServerInitialized)
            {
                Debug.Log("[FishNetPlayersController] Not a server");
                return;
            }
            
            if (_serverPayloadController.Payload == null || _serverPayloadController.Payload.CharacterByPlayerIds == null)
            {
                Debug.Log("[FishNetPlayersController] No characters in server payload");
                return;
            }
            
            if (PlayerCharacterData.TryGetFromPlayer(playerData, out _))
            {
                Debug.Log("[FishNetPlayersController] Already has character data for this player");
                return;
            }
            
            if (!PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
            {
                Debug.Log("[FishNetPlayersController] Can't get match data for player");
                return;
            }

            Debug.Log($"[FishNetPlayersController] Got player {playerData.PlayerNetworkId} match data with playerId {matchData.PlayerId}");
            foreach (var characterContainer in _serverPayloadController.Payload.CharacterByPlayerIds)
            {
                if (characterContainer.PlayerId != matchData.PlayerId)
                    continue;

                Debug.Log($"[FishNetPlayersController] Set character {characterContainer.CharacterName} to player {playerData.PlayerNetworkId} - {matchData.PlayerId}");
                var characterData = new PlayerCharacterData
                {
                    CharacterName = characterContainer.CharacterName
                };
                characterData.SetToPlayer(playerData);
                break;
            }

            Debug.Log($"[FishNetPlayersController] Can't find character for player {playerData.PlayerNetworkId}, id: {matchData.PlayerId}");
#endif
        }

        private async Task WaitForPayloadAndSetMatchData(CancellationToken cancellationToken)
        {
            while (_webBridge.Payload == null)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }

            Debug.Log("[FishNetPlayersController] Got payload");
            if (!TryGetLocalPlayerData(out var playerData))
            {
                Debug.Log("[FishNetPlayersController] Can't get local player data");
                return;
            }

            Debug.Log($"[FishNetPlayersController] set PlayerMatchData player id {_webBridge.Payload.PlayerId} for player {playerData.PlayerNetworkId}");
            var data = new PlayerMatchData
            {
                Nickname = _webBridge.Payload.Nickname,
                PlayerId = _webBridge.Payload.PlayerId
            };
            data.SetToPlayer(playerData);
        }
    }
}