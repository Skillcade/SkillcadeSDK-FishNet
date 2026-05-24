using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SkillcadeSDK.Common.Players;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Players
{
    public class FishNetPlayerData : NetworkBehaviour, IPlayerData<FishNetPlayerData, IDataContainer>
    {
        public event Action<FishNetPlayerData> OnChanged;
        public event Action<FishNetPlayerData, string> OnValueChanged;

        public int PlayerNetworkId => _localOwnerId >= 0 ? _localOwnerId : OwnerId;

        /// <summary>
        /// Server-side: stable copy of the FishNet connection id used to authenticate this
        /// player. Distinct from <see cref="PlayerNetworkId"/> after a reconnect, where the
        /// network id is remapped to the original ReplayClientId.
        /// </summary>
        public int ServerConnectionClientId { get; private set; } = -1;

        private readonly SyncDictionary<string, IDataContainer> _data = new(new SyncTypeSettings(WritePermission.ServerOnly));
        private readonly List<MonoBehaviour> _playerObjects = new();

        [Inject] private readonly FishNetPlayersController _fishNetPlayersController;
        
        private int _localOwnerId = -1;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            this.InjectToMe();
            if (IsServerInitialized && OwnerId > 0)
                ServerConnectionClientId = OwnerId;
            // On the server, registration is driven explicitly by FishNetPlayersController.OnServerAuthenticationResult
            // so the correct (possibly remapped) ReplayClientId is used. Clients still need to
            // auto-register here based on the FishNet OwnerId they receive.
            if (OwnerId > 0 && !IsServerInitialized)
                _fishNetPlayersController.RegisterPlayerData(OwnerId, this);

            _data.OnChange += OnDataChanged;
        }

        /// <summary>
        /// Server-only helper used during a reconnect so that <see cref="PlayerNetworkId"/>
        /// on the server matches the stable ReplayClientId rather than the brand-new FishNet
        /// connection id. Clients keep their (new) connection id and are not made aware of
        /// the remap — replay frames are still routed correctly by the server-side mapping.
        /// </summary>
        public void RebindServerNetworkId(int replayClientId)
        {
            _localOwnerId = replayClientId;
            _fishNetPlayersController.RegisterPlayerData(replayClientId, this);
        }

        [ObserversRpc(BufferLast = true)]
        public void InitializeWithOwnerId(int ownerId)
        {
            _localOwnerId = ownerId;
            _fishNetPlayersController.RegisterPlayerData(ownerId, this);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            // Prefer the explicit local owner id (set on server via RebindServerNetworkId or on
            // clients via InitializeWithOwnerId). Fall back to OwnerId, and finally to the
            // saved ServerConnectionClientId in case FishNet has already cleared the connection.
            int id = _localOwnerId >= 0
                ? _localOwnerId
                : (OwnerId > 0 ? OwnerId : ServerConnectionClientId);
            if (id >= 0)
                _fishNetPlayersController.UnregisterPlayerData(id);
            _data.OnChange -= OnDataChanged;
        }

        public void SetData<T>(string key, T data) where T : IDataContainer
        {
            if (IsServerInitialized)
            {
                _data[key] = data;
                return;
            }

            if (OwnerId == NetworkManager.ClientManager.Connection.ClientId)
            {
                SetDataFromLocalServerRpc(key, data);
                return;
            }

            Debug.LogError("[FishNetPlayerData] Trying to set data on remote client. Only server and local client are allowed");
        }

        public bool TryGetData<T>(string key, out T data) where T : IDataContainer
        {
            data = default;
            if (!_data.TryGetValue(key, out var obj))
                return false;

            if (obj is not T typedData)
                return false;

            data = typedData;
            return true;
        }

        public void AddPlayerObject<T>(T instance) where T : MonoBehaviour
        {
            _playerObjects.Add(instance);
        }

        public void RemovePlayerObject<T>(T instance) where T : MonoBehaviour
        {
            _playerObjects.Remove(instance);
        }

        public bool TryGetPlayerObject<T>(out T playerObject) where T : MonoBehaviour
        {
            foreach (var behaviour in _playerObjects)
            {
                if (behaviour == null)
                    continue;

                if (behaviour is T typedBehaviour)
                {
                    playerObject = typedBehaviour;
                    return true;
                }
            }

            playerObject = null;
            return false;
        }

        public IEnumerator<T> GetAllPlayerObjects<T>() where T : MonoBehaviour
        {
            foreach (var behaviour in _playerObjects)
            {
                if (behaviour != null && behaviour is T typedBehaviour)
                    yield return typedBehaviour;
            }
        }

        [ServerRpc(RequireOwnership = true)]
        private void SetDataFromLocalServerRpc(string key, IDataContainer data)
        {
            _data[key] = data;
        }

        private void OnDataChanged(SyncDictionaryOperation op, string key, IDataContainer value, bool asServer)
        {
            if (value == null || string.IsNullOrEmpty(key) || op == SyncDictionaryOperation.Complete)
                return;

            OnChanged?.Invoke(this);
            OnValueChanged?.Invoke(this, key);
        }
    }
}