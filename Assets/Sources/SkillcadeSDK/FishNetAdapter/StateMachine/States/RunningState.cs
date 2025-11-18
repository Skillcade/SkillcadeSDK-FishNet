using SkillcadeSDK.StateMachine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.StateMachine.States
{
    public class RunningStateBase : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.Running;

        [Inject] private readonly PlayerSpawner _playerSpawner;

        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);

            if (IsServer)
            {
                _playerSpawner.SpawnAllInGamePlayers();
            }
        }
    }
}