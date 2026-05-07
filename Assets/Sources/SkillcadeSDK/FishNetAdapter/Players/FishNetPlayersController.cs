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
            if (!authenticated)
                return;

            if (!TryGetPlayerData(connection.ClientId, out var playerData))
                return;

#if UNITY_EDITOR || UNITY_SERVER
            LogAuthDiag($"[FishNetPlayersController] ApplyAuthenticatedData after auth success clientId={connection.ClientId}");
#endif
            ApplyAuthenticatedData(connection.ClientId, playerData);
        }

        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs stateArgs)
        {
            if (stateArgs.ConnectionState != RemoteConnectionState.Started)
                return;

            var instance = NetworkManager.ServerManager.InstantiateAndSpawn(_playerDataPrefab, Vector3.zero, Quaternion.identity, connection);
            RegisterPlayerData(connection.ClientId, instance);
            instance.InitializeWithOwnerId(connection.ClientId);
#if UNITY_EDITOR || UNITY_SERVER
            LogAuthDiag($"[FishNetPlayersController] RemoteConnection Started clientId={connection.ClientId}, early ApplyAuthenticatedData (store may be empty until token validates)");
#endif
            ApplyAuthenticatedData(connection.ClientId, instance);
        }

        public void RegisterPlayerData(int playerId, FishNetPlayerData playerData)
        {
            if (!_players.TryAdd(playerId, playerData))
                return;
            
            OnPlayerAdded?.Invoke(playerId, playerData);
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
            if (_players.Remove(playerId, out var data))
            {
                data.OnChanged -= OnPlayerDataChanged;
                OnPlayerRemoved?.Invoke(playerId, data);
                
                if (IsServerInitialized)
                    _authenticatedPlayerDataStore.RemoveClient(playerId);
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
            OnPlayerDataUpdated?.Invoke(playerData.PlayerNetworkId, playerData);
            
#if UNITY_SERVER || UNITY_EDITOR
            if (!IsServerInitialized)
                return;
            
            if (_serverPayloadController.Payload == null || _serverPayloadController.Payload.CharacterByPlayerIds == null)
                return;
            
            if (PlayerCharacterData.TryGetFromPlayer(playerData, out _))
                return;
            
            if (!PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                return;

            foreach (var characterContainer in _serverPayloadController.Payload.CharacterByPlayerIds)
            {
                if (characterContainer.PlayerId != matchData.PlayerId)
                    continue;

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
            if (!_authenticatedPlayerDataStore.TryGetByClientId(clientId, out var authData))
            {
#if UNITY_EDITOR || UNITY_SERVER
                LogAuthDiag($"[FishNetPlayersController] ApplyAuthenticatedData miss clientId={clientId} (auth store has no entry yet)");
#endif
                return;
            }

            var matchData = new PlayerMatchData
            {
                Nickname = authData.Nickname,
                PlayerId = authData.PlayerId
            };
            matchData.SetToPlayer(playerData);

#if UNITY_SERVER || UNITY_EDITOR
            EnsureHubCharacterDataIfNeeded(playerData, authData);
#endif
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void EnsureHubCharacterDataIfNeeded(FishNetPlayerData playerData, AuthenticatedPlayerData authData)
        {
            var roster = _serverPayloadController.Payload?.CharacterByPlayerIds;
            if (roster == null)
                return;

            if (PlayerCharacterData.TryGetFromPlayer(playerData, out _))
                return;

            var characterName = authData.CharacterName;
            if (string.IsNullOrEmpty(characterName) && !string.IsNullOrEmpty(authData.PlayerId))
            {
                foreach (var container in roster)
                {
                    if (container.PlayerId != authData.PlayerId)
                        continue;
                    characterName = container.CharacterName ?? string.Empty;
                    break;
                }
            }

            var characterData = new PlayerCharacterData
            {
                CharacterName = characterName ?? string.Empty
            };
            characterData.SetToPlayer(playerData);
        }
#endif

#if UNITY_EDITOR || UNITY_SERVER
        private static void LogAuthDiag(string message)
        {
            Debug.Log(message);
        }
#endif
    }
}