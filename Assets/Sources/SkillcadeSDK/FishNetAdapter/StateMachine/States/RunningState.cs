using SkillcadeSDK.Common.Level;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.Events;
using SkillcadeSDK.Replays;
using SkillcadeSDK.StateMachine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.States
{
    /// <summary>
    /// State for the main game running phase.
    /// Most game-specific logic should be implemented through event handlers
    /// that subscribe to RunningStartEvent and other events.
    /// </summary>
    public class RunningState : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.Running;

        [Inject] private readonly IPlayerSpawner _playerSpawner;
        [Inject] private readonly RespawnServiceProvider _respawnServiceProvider;
        [Inject] private readonly GameEventBus _eventBus;
        [Inject] private readonly ReplayWriteService _replayWriteService;

        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);

            if (IsServer)
            {
                _playerSpawner.EnsurePlayersSpawned();
                _respawnServiceProvider.TriggerRespawn();
            }

            _eventBus.Publish(new RunningStartEvent());
            _replayWriteService.StartWrite();
        }

        public override void OnExit(GameStateType nextState)
        {
            base.OnExit(nextState);
            _replayWriteService.FinishWrite(IsServer);
        }
    }
}
