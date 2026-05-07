using FishNet.Object;
using SkillcadeSDK;
using SkillcadeSDK.Replays.Components;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetReplayObjectHandler : ReplayObjectHandler
    {
        protected override int RuntimeNetworkObjectId => _runtimeObjectId.GetValueOrDefault(_networkObject.ObjectId);
        
        [SerializeField] private NetworkObject _networkObject;

        private bool _isRegistered;
        private bool _subscribedToWriteStarted;
        
        private int? _runtimeObjectId;
        
        protected override void Awake()
        {
            base.Awake();
            _isRegistered = false;
        }

        private void OnDestroy()
        {
            if (_isRegistered)
            {
                _isRegistered = false;
                Unregister();
            }

            if (_subscribedToWriteStarted && ReplayWriteService != null && !IsReplaying)
            {
                ReplayWriteService.OnWriteStarted -= RefreshReplayRegistration;
                _subscribedToWriteStarted = false;
            }
        }

        private void Update()
        {
            if (_networkObject.IsServerInitialized)
            {
                EnsureWriteStartedSubscription();
            }
            
            if (_isRegistered && !_networkObject.IsSpawned)
            {
                _isRegistered = false;
                Unregister();
                _runtimeObjectId = null;
            }

            if (!_isRegistered && _networkObject.IsSpawned && _networkObject.IsServerInitialized)
            {
                RefreshReplayRegistration();
            }
        }

        private void RefreshReplayRegistration()
        {
            if (IsReplaying || !_networkObject.IsSpawned)
                return;

            EnsureWriteStartedSubscription();
            if (ReplayWriteService == null || !ReplayWriteService.IsActive)
                return;

            _runtimeObjectId = _networkObject.ObjectId;
            Register();
            _isRegistered = true;
#if UNITY_EDITOR
            Debug.Log($"[FishNetReplayObjectHandler] Refreshing replay registration on object {RuntimeNetworkObjectId}");
#endif
        }

        private void EnsureWriteStartedSubscription()
        {
            if (_subscribedToWriteStarted || IsReplaying)
                return;

            this.InjectToMe();
            if (ReplayWriteService == null)
                return;
            
            ReplayWriteService.OnWriteStarted += RefreshReplayRegistration;
            _subscribedToWriteStarted = true;
        }
    }
}
