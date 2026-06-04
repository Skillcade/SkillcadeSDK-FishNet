using FishNet.Managing;
using SkillcadeSDK.Common.Level;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Events;
using SkillcadeSDK.FishNetAdapter.StateMachine.Events;
using SkillcadeSDK.Replays;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.States
{
    /// <summary>
    /// State for the main game running phase.
    /// Manages the game timer and transitions to Finished on timeout.
    /// </summary>
    public class RunningState : NetworkState<GameStateType, RunningStateData>
    {
        public override GameStateType Type => GameStateType.Running;

        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly IPlayerSpawner _playerSpawner;
        [Inject] private readonly RespawnServiceProvider _respawnServiceProvider;
        [Inject] private readonly GameEventBus _eventBus;
        [Inject] private readonly ReplayWriteService _replayWriteService;
        [Inject] private readonly IConnectionController _connectionController;

        private float _gameTime;
        private double _startTime;
        private int _lastSecond = -1;

        protected override void OnEnter(GameStateType prevState, RunningStateData data)
        {
            base.OnEnter(prevState, data);

            _gameTime = data.GameDurationSeconds;
            _startTime = data.RunningStartTime > 0
                ? data.RunningStartTime
                : _networkManager.TimeManager.TicksToTime(_networkManager.TimeManager.Tick);
            _lastSecond = Mathf.CeilToInt(_gameTime);
            var timePassed = _networkManager.TimeManager.TicksToTime(_networkManager.TimeManager.Tick) - _startTime;
            _lastSecond = Mathf.CeilToInt(_gameTime - (float)timePassed);
            if (_lastSecond < 0)
                _lastSecond = 0;
            _eventBus.Publish(new RunningTimerTickEvent(_lastSecond));

            if (IsServer)
            {
                _playerSpawner.EnsurePlayersSpawned();
                _respawnServiceProvider.TriggerRespawn();
            }

            _eventBus.Publish(new RunningStartEvent(_startTime));

            if (_connectionController.ConnectionState != ConnectionState.SinglePlayer)
                _replayWriteService.StartWrite();
        }

        public override void Update()
        {
            base.Update();
            
            var time = _networkManager.TimeManager.TicksToTime(_networkManager.TimeManager.Tick);
            var timePassed = time - _startTime;
            var timeRemaining = _gameTime - timePassed;

            var currentSecond = Mathf.CeilToInt((float)timeRemaining);
            if (currentSecond != _lastSecond && currentSecond >= 0)
            {
                _lastSecond = currentSecond;
                _eventBus.Publish(new RunningTimerTickEvent(currentSecond));
            }

            if (IsServer && timeRemaining <= 0f)
                StateMachine.SetStateServer(GameStateType.Finished, new FinishedStateData(0, FinishReason.Draw));
        }

        public override void OnExit(GameStateType nextState)
        {
            base.OnExit(nextState);
            if (_connectionController.ConnectionState != ConnectionState.SinglePlayer)
                _replayWriteService.FinishWrite(IsServer);
        }
    }
}
