using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.Connection;
using SkillcadeSDK.FishNetAdapter.Authenticator;
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

        [Inject] private readonly ConnectionConfig _connectionConfig;
        [Inject] private readonly AuthenticatedPlayerDataStore _authenticatedPlayerDataStore;
        
#if UNITY_SERVER || UNITY_EDITOR
        [Inject] private readonly ServerPayloadController _serverPayloadController;
#endif
        
        private readonly Dictionary<int, FishNetPlayerData> _players = new();
        
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (IsServerInitialized)
            {
                NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
                NetworkManager.ServerManager.OnAuthenticationResult += OnServerAuthenticationResult;
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (IsServerInitialized)
            {
                NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
                NetworkManager.ServerManager.OnAuthenticationResult -= OnServerAuthenticationResult;
            }
        }

        private void OnServerAuthenticationResult(NetworkConnection connection, bool authenticated)
        {
            Debug.Log("[FishNetPlayersController] Server authentication result");
            if (!authenticated)
            {
                Debug.Log("[FishNetPlayersController] not authenticated");
                return;
            }

            if (!TryGetPlayerData(connection.ClientId, out var playerData))
            {
                Debug.Log("[FishNetPlayersController] Can't get player data");
                return;
            }

            Debug.Log($"[FishNetPlayersController] ApplyAuthenticatedData after auth success clientId={connection.ClientId}");
            ApplyAuthenticatedData(connection.ClientId, playerData);
        }

        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs stateArgs)
        {
            if (stateArgs.ConnectionState != RemoteConnectionState.Started)
                return;

            Debug.Log($"[FishNetPlayersController] Remote connection started for {connection.ClientId}, creating player data");
            var instance = NetworkManager.ServerManager.InstantiateAndSpawn(_playerDataPrefab, Vector3.zero, Quaternion.identity, connection);
            RegisterPlayerData(connection.ClientId, instance);
            instance.InitializeWithOwnerId(connection.ClientId);
            Debug.Log($"[FishNetPlayersController] RemoteConnection Started clientId={connection.ClientId}, call ApplyAuthenticatedData");
            ApplyAuthenticatedData(connection.ClientId, instance);
        }

        public void RegisterPlayerData(int playerId, FishNetPlayerData playerData)
        {
            Debug.Log($"[FishNetPlayersController] Register player data {playerId}");
            if (!_players.TryAdd(playerId, playerData))
            {
                Debug.Log("[FishNetPlayersController] Already registered, exiting");
                return;
            }

            Debug.Log("[FishNetPlayersController] Invoke OnPlayerAdded");
            OnPlayerAdded?.Invoke(playerId, playerData);
            Debug.Log("[FishNetPlayersController] Subscribe to data changed");
            playerData.OnChanged += OnPlayerDataChanged;

            if (!IsClientInitialized || playerId != LocalPlayerId)
                return;
            
            if (!_connectionConfig.SkillcadeHubIntegrated)
            {
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
            Debug.Log($"[FishNetPlayersController] Unregister player data {playerId}");
            if (!_players.Remove(playerId, out var data))
                return;

            Debug.Log("[FishNetPlayersController] Unsubscribe to data changed");
            data.OnChanged -= OnPlayerDataChanged;
            Debug.Log("[FishNetPlayersController] Call OnPlayerRemoved");
            OnPlayerRemoved?.Invoke(playerId, data);
                
            if (IsServerInitialized)
                _authenticatedPlayerDataStore.RemoveClient(playerId);
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
            OnPlayerDataUpdated?.Invoke(playerData.PlayerNetworkId, playerData);
            
#if UNITY_SERVER || UNITY_EDITOR
            if (!IsServerInitialized)
                return;
            
            if (_serverPayloadController.Payload?.CharacterByPlayerIds == null)
                return;
            
            if (!PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                return;

            if (PlayerCharacterData.TryGetFromPlayer(playerData, out _))
                return;
            
            foreach (var characterContainer in _serverPayloadController.Payload.CharacterByPlayerIds)
            {
                if (characterContainer.PlayerId != matchData.PlayerId)
                    continue;

                Debug.Log($"[FishNetPlayersController] Set character {characterContainer.CharacterName} to player {matchData.PlayerId}");
                var characterData = new PlayerCharacterData
                {
                    CharacterName = characterContainer.CharacterName
                };
                characterData.SetToPlayer(playerData);
                break;
            }
#endif
        }

        private void ApplyAuthenticatedData(int clientId, FishNetPlayerData playerData)
        {
            Debug.Log($"[FishNetPlayersController] Apply authenticated data for {clientId}");
            if (!_authenticatedPlayerDataStore.TryGetByClientId(clientId, out var authData))
            {
                Debug.Log($"[FishNetPlayersController] ApplyAuthenticatedData miss clientId={clientId} (auth store has no entry yet)");
                return;
            }

            var matchData = new PlayerMatchData
            {
                Nickname = authData.Nickname,
                PlayerId = authData.PlayerId
            };
            Debug.Log($"[FishNetPlayersController] Set match data, nickname {authData.Nickname}, player {authData.PlayerId}");
            matchData.SetToPlayer(playerData);

#if UNITY_SERVER || UNITY_EDITOR
            EnsureHubCharacterDataIfNeeded(playerData, authData);
#endif
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void EnsureHubCharacterDataIfNeeded(FishNetPlayerData playerData, AuthenticatedPlayerData authData)
        {
            Debug.Log("[FishNetPlayersController] Apply character data");
            var roster = _serverPayloadController.Payload?.CharacterByPlayerIds;
            if (roster == null)
            {
                Debug.Log("[FishNetPlayersController] No characters config");
                return;
            }

            var characterName = authData.CharacterName;
            Debug.Log($"[FishNetPlayersController] Got {characterName} from auth data for player {authData.PlayerId}");
            if (string.IsNullOrEmpty(characterName) && !string.IsNullOrEmpty(authData.PlayerId))
            {
                foreach (var container in roster)
                {
                    if (container.PlayerId != authData.PlayerId)
                        continue;
                    
                    characterName = container.CharacterName ?? string.Empty;
                    Debug.Log($"[FishNetPlayersController] Override character name for player {authData.PlayerId} with {characterName}");
                    break;
                }
            }

            Debug.Log($"[FishNetPlayersController] Set result character name {characterName} to player {authData.PlayerId}");
            if (!PlayerCharacterData.TryGetFromPlayer(playerData, out var characterData))
                characterData = new PlayerCharacterData();

            characterData.CharacterName = characterName;
            characterData.SetToPlayer(playerData);
        }
#endif
    }
}