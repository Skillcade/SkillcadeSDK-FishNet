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

        private readonly SyncDictionary<string, IDataContainer> _data = new(new SyncTypeSettings(WritePermission.ServerOnly));
        private readonly List<MonoBehaviour> _playerObjects = new();

        [Inject] private readonly FishNetPlayersController _fishNetPlayersController;
        
        private int _localOwnerId = -1;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            this.InjectToMe();
            if (OwnerId > 0)
                _fishNetPlayersController.RegisterPlayerData(OwnerId, this);

            _data.OnChange += OnDataChanged;
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
            var id = _localOwnerId >= 0 ? _localOwnerId : OwnerId;
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