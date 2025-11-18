using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.StateMachine;
using SkillcadeSDK.WebRequests;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.StateMachine.States
{
    public enum FinishReason
    {
        ReachedFinish,
        TechnicalWin,
    }
    
    public class FinishedStateData
    {
        public readonly int WinnerId;
        public readonly float WaitTimer;
        public readonly FinishReason FinishReason;

        public FinishedStateData(int winnerId, float waitTimer, FinishReason finishReason)
        {
            WinnerId = winnerId;
            WaitTimer = waitTimer;
            FinishReason = finishReason;
        }
    }
    
    public class FinishedStateBase : NetworkState<GameStateType, FinishedStateData>
    {
        public override GameStateType Type => GameStateType.Finished;

        [Inject] private readonly WebRequester _webRequester;
        [Inject] private readonly PlayerSpawner _playerSpawner;
        [Inject] private readonly IPlayersController _playersController;

        private float _timer;

        protected override void OnEnter(GameStateType prevState, FinishedStateData data)
        {
            base.OnEnter(prevState, data);
            _timer = data.WaitTimer;

            if (!TryValidateMatchIds())
                Debug.LogError("MatchId is invalid");
            
            if (IsServer)
            {
                SendWinnerToBackend(data.WinnerId);
                _playerSpawner.DespawnAllPlayers();
            }
        }

        public override void Update()
        {
            base.Update();
            _timer -= Time.deltaTime;

            if (IsServer && _timer <= 0)
                StateMachine.SetStateServer(GameStateType.WaitForPlayers);
        }

        private bool TryValidateMatchIds()
        {
            string matchId = null;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (!playerData.TryGetData(PlayerDataConst.MatchId, out string playerMatchId))
                    continue;
                
                if (string.IsNullOrEmpty(playerMatchId))
                {
                    Debug.Log($"Player {playerData.PlayerNetworkId} matchId is null");
                    continue;
                }
                
                if (string.IsNullOrEmpty(matchId))
                {
                    matchId = playerMatchId;
                }
                else if (!string.Equals(matchId, playerMatchId))
                {
                    Debug.Log($"Players matchId is different: first {matchId}, second: {playerMatchId}");
                    return false;
                }
            }
            
            return matchId != null;
        }

        private void SendWinnerToBackend(int winnerId)
        {
            if (winnerId == 0 || !_playersController.TryGetPlayerData(winnerId, out var playerData))
            {
                Debug.Log($"Don't send winner, id: {winnerId}");
                return;
            }
            
            if (!playerData.TryGetData(PlayerDataConst.MatchId, out string matchId))
                return;
            
            if (!playerData.TryGetData(PlayerDataConst.UserId, out string userId))
                return;

            _webRequester.SendWinner(matchId, userId);
        }
    }
}