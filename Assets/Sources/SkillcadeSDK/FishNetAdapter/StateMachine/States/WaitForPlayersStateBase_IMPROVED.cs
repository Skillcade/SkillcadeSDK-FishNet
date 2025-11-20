using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.StateMachine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.StateMachine.States
{
    /// <summary>
    /// УЛУЧШЕННАЯ ВЕРСИЯ WaitForPlayersStateBase
    /// Демонстрирует использование новых helper методов для упрощения кода
    /// </summary>
    public class WaitForPlayersStateBase_IMPROVED : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.WaitForPlayers;

        [Inject] private readonly PlayerSpawner _playerSpawner;
        [Inject] private readonly IPlayersController _playersController;

        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);
            _playersController.OnPlayerDataUpdated += OnPlayerUpdated;

            if (IsServer)
            {
                ResetPlayersState();
                _playerSpawner.DespawnAllPlayers();
            }
        }

        /// <summary>
        /// Сброс состояния игроков при входе в лобби
        /// </summary>
        private void ResetPlayersState()
        {
            if (!IsServer)
                return;

            // ✅ НОВЫЙ СПОСОБ - все в двух вызовах!
            PlayersHelper.ResetAllPlayersReady(_playersController);
            PlayersHelper.ResetAllPlayersInGame(_playersController);

            /* ❌ СТАРЫЙ СПОСОБ - много кода
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                playerData.SetDataOnServer(PlayerDataConst.IsReady, false);
                playerData.SetDataOnServer(PlayerDataConst.InGame, false);
            }
            */
        }

        private void OnPlayerUpdated(int playerId, IPlayerData data)
        {
            if (IsServer)
                CheckIfCanStartGame();
        }

        /// <summary>
        /// Проверка возможности начала игры
        /// </summary>
        private void CheckIfCanStartGame()
        {
            // ✅ НОВЫЙ СПОСОБ - один вызов, все понятно
            if (!PlayersHelper.AreAllPlayersReady(_playersController, minPlayers: 1))
                return;

            /* ❌ СТАРЫЙ СПОСОБ - сложная логика
            int readyPlayers = 0;
            int notReadyPlayers = 0;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (playerData.TryGetData(PlayerDataConst.IsReady, out bool ready) && ready)
                    readyPlayers++;
                else
                    notReadyPlayers++;
            }

            bool shouldStartGame = readyPlayers >= 1 && notReadyPlayers == 0;
            if (!shouldStartGame) return;
            */

            StartGame();
        }

        /// <summary>
        /// Запуск игры
        /// </summary>
        private void StartGame()
        {
            // ✅ НОВЫЙ СПОСОБ - один вызов
            PlayersHelper.SetReadyPlayersInGame(_playersController);

            /* ❌ СТАРЫЙ СПОСОБ
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (playerData.TryGetData(PlayerDataConst.IsReady, out bool ready) && ready)
                    playerData.SetDataOnServer(PlayerDataConst.InGame, true);
            }
            */

            _playerSpawner.SpawnAllInGamePlayers();
            StateMachine.SetStateServer(GameStateType.Countdown);
        }
    }
}
