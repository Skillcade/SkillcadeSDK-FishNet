using System;
using FishNet.Managing;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Replays;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetReplayWriteService : ReplayWriteService, IInitializable, IDisposable
    {
        [Inject] private readonly IConnectionController _connectionController;
        [Inject] private readonly NetworkManager _networkManager;
        
        public void Initialize()
        {
            if (_connectionController.ConnectionState == ConnectionState.Hosting)
                Subscribe();
            else
                _connectionController.OnStateChanged += OnConnectionStateChanged;
        }

        public void Dispose()
        {
            _connectionController.OnStateChanged -= OnConnectionStateChanged;
            _networkManager.TimeManager.OnPostTick -= SimulateTick;
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            if (state == ConnectionState.Hosting)
                Subscribe();
        }

        public void Start() => StartWrite();
        public void Stop() => FinishWrite();

        private void Subscribe()
        {
            _networkManager.TimeManager.OnPostTick += SimulateTick;
        }

        private void SimulateTick()
        {
            OnNetworkTick((int)_networkManager.TimeManager.Tick);
        }
    }
}