using FishNet.Object;
using SkillcadeSDK.Replays.Components;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetReplayObjectHandler : ReplayObjectHandler
    {
        protected override int RuntimeNetworkObjectId => _networkObject.ObjectId;

        [SerializeField] private NetworkObject _networkObject;

        private bool _isRegistered;
        
        protected override void Awake()
        {
            base.Awake();
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