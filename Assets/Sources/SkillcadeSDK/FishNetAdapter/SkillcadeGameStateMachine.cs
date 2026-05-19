using System.Linq;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.StateMachine;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    /// <summary>
    /// Game state machine implementation for Skillcade games.
    /// Handles technical wins when players leave during the game.
    /// </summary>
    public class SkillcadeGameStateMachine : NetworkStateMachine<GameStateType>
    {
        [Inject] private readonly FishNetPlayersController _playersController;

        public override void Initialize()
        {
            base.Initialize();
            _playersController.OnPlayerRemoved += OnPlayerRemoved;
        }

        public override void Dispose()
        {
            base.Dispose();
            _playersController.OnPlayerRemoved -= OnPlayerRemoved;
        }

        protected override void OnNetworkStart()
        {
            base.OnNetworkStart();
            if (IsServer)
                SetStateServer(GameStateType.WaitForPlayers);
        }

        private void OnPlayerRemoved(int playerId, FishNetPlayerData fishNetPlayerData)
        {
            if (!IsServer)
                return;

            Debug.Log($"[SkillcadeGameStateMachine] Player {playerId} removed");
            if (CurrentStateType == GameStateType.Finished)
                return;

            int inGamePlayers = 0;
            int winnerId = -1;
            string winnerPlayerId = null;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (PlayerInGameData.TryGetFromPlayer(playerData, out var inGameData) && inGameData.InGame)
                {
                    winnerId = playerData.PlayerNetworkId;
                    if (PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                        winnerPlayerId = matchData.PlayerId;
                    
                    inGamePlayers++;
                }
            }

            Debug.Log($"[SkillcadeGameStateMachine] In game players {inGamePlayers}, total: {_playersController.GetAllPlayersData().Count()}, state: {CurrentStateType}");
            
            if (inGamePlayers == 0 && CurrentStateType != GameStateType.WaitForPlayers)
                SetStateServer(GameStateType.WaitForPlayers);
            else if (inGamePlayers == 1)
                SetStateServer(GameStateType.Finished, new FinishedStateData(winnerId, winnerPlayerId, FinishReason.TechnicalWin));
        }
    }
}
