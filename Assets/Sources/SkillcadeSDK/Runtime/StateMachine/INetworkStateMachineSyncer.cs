using System;

namespace SkillcadeSDK.StateMachine
{
    public interface INetworkStateMachineSyncer
    {
        public event Action OnNetworkStart;
        public event Action OnNetworkStop;
        
        public delegate void OnStateChanged(StateData prev, StateData next);
        
        public event OnStateChanged StateChanged;

        public bool IsServer { get; }
        public bool IsClient { get; }
        public StateData CurrentState { get; }

        public void SetInitialState(StateData stateData);
        public void SetStateOnServer(StateData stateData);
    }
}