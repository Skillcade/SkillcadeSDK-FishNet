using FishNet.Object;
using SkillcadeSDK.Replays.Components;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetReplayObjectHandler : ReplayObjectHandler
    {
        public override int NetworkPrefabId => _networkObject.PrefabId;
        public override int NetworkObjectId => _networkObject.ObjectId;

        [SerializeField] private NetworkObject _networkObject;

        private bool _isRegistered;
        
        private void Awake()
        {
            _isRegistered = false;
        }

        private void Update()
        {
            if (_isRegistered && !_networkObject.IsSpawned)
            {
                _isRegistered = false;
                Unregister();
            }

            if (!_isRegistered && _networkObject.IsSpawned)
            {
                _isRegistered = true;
                Register();
            }
        }
    }
}