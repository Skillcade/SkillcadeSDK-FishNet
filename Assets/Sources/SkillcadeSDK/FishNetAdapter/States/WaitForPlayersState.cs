using SkillcadeSDK.Common.Level;
using SkillcadeSDK.Common.Players;
using SkillcadeSDK.Connection;
using SkillcadeSDK.Events;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.StateMachine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.States
{
    /// <summary>
    /// State for waiting for players to be ready before starting the game.
    /// Handles ready checking and transitions to Countdown when all players are ready.
    /// Publishes events for UI and game-specific handlers.
    /// </summary>
    public class WaitForPlayersState : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.WaitForPlayers;

        [Inject] private readonly IPlayerSpawner _playerSpawner;
        [Inject] private readonly FishNetPlayersController _playersController;
        [Inject] private readonly RespawnServiceProvider _respawnServiceProvider;
        [Inject] private readonly IConnectionController _connectionController;
        [Inject] private readonly GameEventBus _eventBus;

        private bool _skipUpdate;

        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);
            _playersController.OnPlayerAdded += OnPlayerUpdated;
            _playersController.OnPlayerDataUpdated += OnPlayerUpdated;
            _playersController.OnPlayerRemoved += OnPlayerUpdated;

            if (IsServer)
            {
                ClearReadyStateForPlayers();
                _playerSpawner.EnsurePlayersDespawned();
                _respawnServiceProvider.TriggerRespawn();
            }

            _eventBus.Publish(new WaitForPlayersEnterEvent());
        }

        public override void OnExit(GameStateType nextState)
        {
            base.OnExit(nextState);
            _playersController.OnPlayerAdded -= OnPlayerUpdated;
            _playersController.OnPlayerDataUpdated -= OnPlayerUpdated;
            _playersController.OnPlayerRemoved -= OnPlayerUpdated;
        }

        private void ClearReadyStateForPlayers()
        {
            if (!IsServer)
                return;

            _skipUpdate = true;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (!PlayerInGameData.TryGetFromPlayer(playerData, out var inGameData))
                    inGameData = new PlayerInGameData();

                inGameData.InGame = false;
                inGameData.IsReady = false;
                inGameData.SetToPlayer(playerData);
            }
            _skipUpdate = false;
        }

        private void OnPlayerUpdated(int clientId, FishNetPlayerData data)
        {
            if (_skipUpdate)
                return;

            if (IsServer)
                CheckReadyPlayers();
        }

        private void CheckReadyPlayers()
        {
            int readyPlayers = 0;
            int notReadyPlayers = 0;
            int totalPlayers = 0;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                totalPlayers++;
                if (PlayerInGameData.TryGetFromPlayer(playerData, out var playerInGameData) && playerInGameData.IsReady)
                    readyPlayers++;
                else
                    notReadyPlayers++;
            }

            bool shouldStartGame = _connectionController.ActiveConfig.TargetPlayerCount > 0
                ? totalPlayers >= _connectionController.ActiveConfig.TargetPlayerCount
                : readyPlayers >= 1 && notReadyPlayers == 0;

            if (!shouldStartGame)
                return;

            _skipUpdate = true;
            SetReadyPlayersInGame();
            _playerSpawner.EnsurePlayersSpawned();
            _eventBus.Publish(new AllPlayersReadyEvent());
            _skipUpdate = false;
            StateMachine.SetStateServer(GameStateType.Countdown);
        }

        private void SetReadyPlayersInGame()
        {
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (!PlayerInGameData.TryGetFromPlayer(playerData, out var playerInGameData))
                {
                    if (_connectionController.ActiveConfig.TargetPlayerCount > 0)
                        playerInGameData = new PlayerInGameData();
                    else
                        continue;
                }

                if (_connectionController.ActiveConfig.TargetPlayerCount == 0 && !playerInGameData.IsReady)
                    continue;

                playerInGameData.IsReady = true;
                playerInGameData.InGame = true;
                playerInGameData.SetToPlayer(playerData);
            }
        }
    }
}
