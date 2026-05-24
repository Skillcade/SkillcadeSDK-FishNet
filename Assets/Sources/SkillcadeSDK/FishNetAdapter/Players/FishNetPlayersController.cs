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
        
        [SerializeField] private FishNetPlayerData _playerDataPrefab;

        [Inject] private readonly ConnectionConfig _connectionConfig;
        [Inject] private readonly AuthenticatedPlayerDataStore _authenticatedPlayerDataStore;
        [Inject] private readonly PlayerReconnectService _reconnectService;
        [Inject] private readonly IPlayerSpawner _playerSpawner;

#if UNITY_SERVER || UNITY_EDITOR
        [Inject] private readonly ServerPayloadController _serverPayloadController;
#endif
        
        private readonly Dictionary<int, FishNetPlayerData> _players = new();
        
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (IsServerInitialized)
            {
                // NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
                NetworkManager.ServerManager.OnAuthenticationResult += OnServerAuthenticationResult;
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (IsServerInitialized)
            {
                // NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
                NetworkManager.ServerManager.OnAuthenticationResult -= OnServerAuthenticationResult;
            }
        }

        private void OnServerAuthenticationResult(NetworkConnection connection, bool authenticated)
        {
            Debug.Log($"[FishNetPlayersController] Server authentication result for connection={connection.ClientId}, authenticated={authenticated}");
            if (!authenticated)
            {
                Debug.Log("[FishNetPlayersController] not authenticated");
                return;
            }

            string playerId = null;
            if (_authenticatedPlayerDataStore.TryGetByClientId(connection.ClientId, out var authData))
                playerId = authData.PlayerId;

            int replayClientId = _reconnectService.RegisterAuthenticatedConnection(playerId, connection.ClientId);

            var instance = NetworkManager.ServerManager.InstantiateAndSpawn(_playerDataPrefab, Vector3.zero, Quaternion.identity, connection);

            if (replayClientId != connection.ClientId)
            {
                Debug.Log($"[FishNetPlayersController] [PlayerReconnect] Reconnect spawn: player={playerId}, replayClientId={replayClientId}, newConnection={connection.ClientId}");
                instance.RebindServerNetworkId(replayClientId);
            }
            else
            {
                RegisterPlayerData(replayClientId, instance);
            }

            // Clients always learn their player's owner id from the new connection id — they
            // don't need to know that the server is remapping it for replay/grace continuity.
            instance.InitializeWithOwnerId(connection.ClientId);

            if (!string.IsNullOrEmpty(playerId) && _reconnectService.TryConsumeReconnectSlot(playerId, out var slot))
            {
                ApplyReconnectSnapshot(instance, slot);
                _playerSpawner.EnsurePlayersSpawned();
            }
        }

        private void ApplyReconnectSnapshot(FishNetPlayerData instance, PlayerReconnectService.GraceSlot slot)
        {
            Debug.Log($"[FishNetPlayersController] [PlayerReconnect] Apply reconnect snapshot: player={slot.PlayerId}, replayClientId={slot.ReplayClientId}, hasCharacterData={slot.HasCharacterData}");
            if (slot.MatchData != null)
                slot.MatchData.SetToPlayer(instance);
            if (slot.InGameData != null)
                slot.InGameData.SetToPlayer(instance);
            if (slot.HasCharacterData && slot.CharacterData != null)
                slot.CharacterData.SetToPlayer(instance);
        }

        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs stateArgs)
        {
            Debug.Log($"[FishNetPlayersController] Remote connection state for {connection.ClientId}: {stateArgs.ConnectionState}");
            if (stateArgs.ConnectionState != RemoteConnectionState.Started)
                return;
            
        }

        public void RegisterPlayerData(int playerId, FishNetPlayerData playerData)
        {
            Debug.Log($"[FishNetPlayersController] Register player data {playerId}");

            foreach (var existing in _players)
            {
                if (!ReferenceEquals(existing.Value, playerData))
                    continue;

                if (existing.Key == playerId)
                    Debug.Log("[FishNetPlayersController] Already registered, exiting");
                else
                    Debug.LogWarning($"[FishNetPlayersController] Instance already registered under key {existing.Key}, refusing to register under {playerId}");
                return;
            }

            if (!_players.TryAdd(playerId, playerData))
            {
                Debug.LogWarning($"[FishNetPlayersController] Key {playerId} already in use by a different instance, exiting");
                return;
            }

            Debug.Log("[FishNetPlayersController] Subscribe to data changed");
            playerData.OnChanged += OnPlayerDataChanged;
            Debug.Log("[FishNetPlayersController] Invoke OnPlayerAdded");
            OnPlayerAdded?.Invoke(playerId, playerData);

            if (IsServerInitialized)
            {
                int authLookupId = playerData != null && playerData.ServerConnectionClientId >= 0
                    ? playerData.ServerConnectionClientId
                    : playerId;
                Debug.Log($"[FishNetPlayersController] RemoteConnection Started networkId={playerId}, authLookup={authLookupId}, call ApplyAuthenticatedData");
                ApplyAuthenticatedData(authLookupId, playerData);
            }
            
            if (!IsClientInitialized || !TryGetLocalPlayerId(out var localPlayerId) || playerId != localPlayerId)
                return;
            
            if (!_connectionConfig.SkillcadeHubIntegrated)
            {
                var data = new PlayerMatchData
                {
                    Nickname = $"Player_{localPlayerId}",
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
            {
                int connectionClientId = data != null && data.ServerConnectionClientId >= 0 ? data.ServerConnectionClientId : playerId;
                _authenticatedPlayerDataStore.RemoveClient(connectionClientId);
                _reconnectService.ForgetConnection(connectionClientId);
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
            if (NetworkObject == null || NetworkManager == null || NetworkManager.ClientManager == null)
                return false;

            if (NetworkManager.ClientManager.Connection == null)
                return false;
            
            return TryGetPlayerData(NetworkManager.ClientManager.Connection.ClientId, out data);
        }

        public bool TryGetLocalPlayerId(out int localPlayerId)
        {
            localPlayerId = -1;
            if (NetworkObject == null || NetworkManager == null || NetworkManager.ClientManager == null)
                return false;

            if (NetworkManager.ClientManager.Connection == null)
                return false;

            localPlayerId = NetworkManager.ClientManager.Connection.ClientId;
            return true;
        }

        public bool IsLocalPlayerId(int playerId)
        {
            return TryGetLocalPlayerId(out var localPlayerId) && localPlayerId == playerId;
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

            if (PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
            {
                if (matchData.Nickname.Equals(authData.Nickname) && matchData.PlayerId.Equals(authData.PlayerId))
                {
                    Debug.Log("[FishNetPlayersController] Already has match data");
                    return;
                }
            }

            matchData = new PlayerMatchData
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