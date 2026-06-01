using System;
using System.Threading.Tasks;
using SkillcadeSDK.Common;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Events;
using SkillcadeSDK.FishNetAdapter.Match;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

#if UNITY_SERVER || UNITY_EDITOR
using SkillcadeSDK.Replays;
#endif

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
        [Inject] private readonly IConnectionController _connectionController;
        
#if UNITY_SERVER || UNITY_EDITOR
        [Inject] private readonly ReplaySendService _replaySendService;
        [Inject] private readonly PlayerReconnectService _reconnectService;
#endif

        private float _timer;

        protected override void OnEnter(GameStateType prevState, FinishedStateData data)
        {
            base.OnEnter(prevState, data);

            _timer = _config.WaitAfterFinishSeconds;

            // Publish both FishNet and stable hub identity; FishNet ids can change after reconnect.
            _eventBus.Publish(new GameFinishedEvent(data.WinnerClientId, data.WinnerPlayerId, data.FinishReason));
            
            if (IsServer)
            {
                WaitForReplaySendAndSendWinner(data.WinnerClientId, data.WinnerPlayerId).DoNotAwait();
                _playerSpawner.EnsurePlayersDespawned();
            }
        }

        public override void Update()
        {
            base.Update();
            if (_connectionController.ActiveConfig.SkillcadeHubIntegrated)
                return;
            
            _timer -= Time.deltaTime;

            if (_timer <= 0f && IsServer)
            {
                StateMachine.SetStateServer(GameStateType.WaitForPlayers);
            }
        }

        private async Task WaitForReplaySendAndSendWinner(int winnerId, string winnerPlayerId)
        {
            Debug.Log("[FinishedState] Sending replays and winner");
#if UNITY_SERVER || UNITY_EDITOR
            Debug.Log("[FinishedState] Waiting for replays");
            try
            {
                await _replaySendService.WaitForReplaySent();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FinishedState] Error on sending replays: {e}");
            }
            Debug.Log("[FinishedState] Replays sent");
#endif
            Debug.Log("[FinishedState] Send winner");
            await _matchService.SendWinnerToBackend(winnerId, winnerPlayerId);
            _reconnectService.ResetForNewMatch();
        }
    }
}
