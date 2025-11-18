using FishNet.Object;
using FishNet.Object.Synchronizing;
using SkillcadeSDK.StateMachine;

namespace SkillcadeSDK.FishNetAdapter.StateMachine
{
    public class GameStateMachineSyncer : NetworkBehaviour, INetworkStateMachineSyncer
    {
        public event INetworkStateMachineSyncer.OnStateChanged StateChanged;

        public new bool IsServer => IsServerInitialized;
        public new bool IsClient => IsClientInitialized;

        public StateData CurrentState => _currentStateData.Value;
        
        private readonly SyncVar<StateData> _currentStateData = new(settings: new SyncTypeSettings(writePermissions: WritePermission.ServerOnly));

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _currentStateData.OnChange += OnStateChanged;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _currentStateData.OnChange -= OnStateChanged;
        }

        public void SetStateOnServer(StateData stateData)
        {
            if (IsServerInitialized)
                _currentStateData.Value = stateData;
        }

        private void OnStateChanged(StateData prev, StateData next, bool asServer)
        {
            StateChanged?.Invoke(prev, next);
        }
    }
}