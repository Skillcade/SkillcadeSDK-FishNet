using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.StateMachine.States
{
    public class CountdownStateData
    {
        public float Timer;
    }
    
    public class CountdownStateBase : NetworkState<GameStateType, CountdownStateData>
    {
        public override GameStateType Type => GameStateType.Countdown;

        [Inject] private readonly PlayerSpawner _playerSpawner;
        
        private float _timer;

        protected override void OnEnter(GameStateType prevState, CountdownStateData data)
        {
            base.OnEnter(prevState);
            _timer = data.Timer;

            if (IsServer)
            {
                _playerSpawner.SpawnAllInGamePlayers();
            }
        }

        public override void Update()
        {
            base.Update();
            _timer -= Time.deltaTime;
            
            if (IsServer && _timer <= 0f)
                StateMachine.SetStateServer(GameStateType.Running);
        }
    }
}