using SkillcadeSDK.Common;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.Events;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.States
{
    /// <summary>
    /// State for countdown before the game starts.
    /// Counts down from configured seconds and transitions to Running state.
    /// Publishes CountdownTickEvent for UI updates.
    /// </summary>
    public class CountdownState : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.Countdown;

        [Inject] private readonly ISkillcadeConfig _config;
        [Inject] private readonly IPlayerSpawner _playerSpawner;
        [Inject] private readonly GameEventBus _eventBus;

        private float _timer;
        private int _lastSecond = -1;

        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);
            _timer = _config.StartGameCountdownSeconds;
            _lastSecond = Mathf.CeilToInt(_timer);

            if (IsServer)
            {
                _playerSpawner.EnsurePlayersSpawned();
            }

            // Publish initial countdown tick
            _eventBus.Publish(new CountdownTickEvent(_lastSecond));
        }

        public override void Update()
        {
            base.Update();
            _timer -= Time.deltaTime;

            int currentSecond = Mathf.CeilToInt(_timer);
            if (currentSecond != _lastSecond && currentSecond >= 0)
            {
                _lastSecond = currentSecond;
                _eventBus.Publish(new CountdownTickEvent(currentSecond));
            }

            if (IsServer && _timer <= 0f)
                StateMachine.SetStateServer(GameStateType.Running);
        }
    }
}
