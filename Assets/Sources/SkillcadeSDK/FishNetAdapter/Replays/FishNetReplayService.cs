using FishNet.Managing;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Replays;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetReplayService : ReplayService
    {
        [Inject] private readonly IConnectionController _connectionController;
        [Inject] private readonly NetworkManager _networkManager;
        
        public override void Initialize()
        {
            base.Initialize();
            if (_connectionController.ConnectionState == ConnectionState.Hosting)
                Subscribe();
            else
                _connectionController.OnStateChanged += OnConnectionStateChanged;
        }

        public override void Dispose()
        {
            base.Dispose();
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