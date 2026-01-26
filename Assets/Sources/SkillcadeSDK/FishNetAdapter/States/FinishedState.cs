using SkillcadeSDK.Common;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.Events;
using SkillcadeSDK.FishNetAdapter.Match;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.States
{
    /// <summary>
    /// State for game finished with a winner.
    /// Sends winner to backend and waits before transitioning back to WaitForPlayers.
    /// Publishes GameFinishedEvent for UI and game-specific handlers.
    /// </summary>
    public class FinishedState : NetworkState<GameStateType, FinishedStateData>
    {
        public override GameStateType Type => GameStateType.Finished;

        [Inject] private readonly ISkillcadeConfig _config;
        [Inject] private readonly IPlayerSpawner _playerSpawner;
        [Inject] private readonly MatchService _matchService;
        [Inject] private readonly GameEventBus _eventBus;

        private float _timer;

        protected override void OnEnter(GameStateType prevState, FinishedStateData data)
        {
            base.OnEnter(prevState, data);

            _timer = _config.WaitAfterFinishSeconds;

            if (IsServer)
            {
                _matchService.SendWinnerToBackend(data.WinnerClientId);
                _playerSpawner.EnsurePlayersDespawned();
            }

            // Publish event with winner id
            _eventBus.Publish(new GameFinishedEvent(data.WinnerClientId, data.FinishReason));
        }

        public override void Update()
        {
            base.Update();
            _timer -= Time.deltaTime;

            if (_timer <= 0f && IsServer)
            {
                StateMachine.SetStateServer(GameStateType.WaitForPlayers);
            }
        }
    }
}
