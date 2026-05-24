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
        [Inject] private readonly PlayerReconnectService _reconnectService;

        public override void Initialize()
        {
            base.Initialize();
            _playersController.OnPlayerRemoved += OnPlayerRemoved;
            _reconnectService.OnGraceExpired += OnGraceExpired;
        }

        public override void Dispose()
        {
            base.Dispose();
            _playersController.OnPlayerRemoved -= OnPlayerRemoved;
            _reconnectService.OnGraceExpired -= OnGraceExpired;
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

            Debug.Log($"[SkillcadeGameStateMachine] [PlayerReconnect] Player {playerId} removed (currentState={CurrentStateType})");
            if (CurrentStateType == GameStateType.Finished)
                return;

            int connectionClientId = fishNetPlayerData != null && fishNetPlayerData.ServerConnectionClientId >= 0
                ? fishNetPlayerData.ServerConnectionClientId
                : playerId;
            bool graceStarted = _reconnectService.TryBeginGracePeriod(connectionClientId, fishNetPlayerData, CurrentStateType);
            if (graceStarted)
                Debug.Log($"[SkillcadeGameStateMachine] [PlayerReconnect] Player {playerId} (connection={connectionClientId}) disconnect deferred via reconnect grace");

            EvaluateMatchAfterPlayerLost(graceStarted);
        }

        private void OnGraceExpired(PlayerReconnectService.GraceSlot slot)
        {
            if (!IsServer)
                return;

            Debug.Log($"[SkillcadeGameStateMachine] [PlayerReconnect] Grace expired for player {slot.PlayerId} (replayClientId={slot.ReplayClientId}); re-evaluating match");
            EvaluateMatchAfterPlayerLost(graceStarted: false);
        }

        /// <summary>
        /// Counts currently-active in-game players, plus any still-pending grace slots.
        /// Technical win only fires when exactly one active player remains and no one is
        /// in grace (i.e. nobody else can come back). With pending grace slots the match
        /// keeps running so the remaining player can still finish.
        /// </summary>
        private void EvaluateMatchAfterPlayerLost(bool graceStarted)
        {
            int activeInGame = 0;
            int winnerId = -1;
            string winnerPlayerId = null;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (PlayerInGameData.TryGetFromPlayer(playerData, out var inGameData) && inGameData.InGame)
                {
                    winnerId = playerData.PlayerNetworkId;
                    if (PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                        winnerPlayerId = matchData.PlayerId;
                    activeInGame++;
                }
            }

            int pendingInGame = _reconnectService.PendingInGameCount;
            int totalPlayers = _playersController.GetAllPlayersData().Count();
            Debug.Log($"[SkillcadeGameStateMachine] [PlayerReconnect] EvaluateMatch: activeInGame={activeInGame}, pendingGrace={pendingInGame}, totalRegistered={totalPlayers}, state={CurrentStateType}, graceStarted={graceStarted}");

            if (activeInGame == 0 && pendingInGame == 0 && CurrentStateType != GameStateType.WaitForPlayers)
            {
                Debug.Log("[SkillcadeGameStateMachine] [PlayerReconnect] No active players and none pending - returning to WaitForPlayers");
                SetStateServer(GameStateType.WaitForPlayers);
                return;
            }

            if (activeInGame == 1 && pendingInGame == 0
                && CurrentStateType != GameStateType.WaitForPlayers
                && CurrentStateType != GameStateType.Finished)
            {
                Debug.Log($"[SkillcadeGameStateMachine] [PlayerReconnect] Only one player left and no grace pending - TechnicalWin for {winnerPlayerId} (network={winnerId})");
                SetStateServer(GameStateType.Finished, new FinishedStateData(winnerId, winnerPlayerId, FinishReason.TechnicalWin));
            }
            else if (activeInGame == 1 && pendingInGame > 0)
            {
                Debug.Log($"[SkillcadeGameStateMachine] [PlayerReconnect] {pendingInGame} player(s) in grace - holding TechnicalWin for {winnerPlayerId}");
            }
        }
    }
}
