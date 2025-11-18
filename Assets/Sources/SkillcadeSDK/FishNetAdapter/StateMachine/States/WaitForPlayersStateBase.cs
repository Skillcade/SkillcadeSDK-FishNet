using SkillcadeSDK.Common.Players;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.StateMachine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.StateMachine.States
{
    public class WaitForPlayersStateBase : NetworkState<GameStateType>
    {
        public override GameStateType Type => GameStateType.WaitForPlayers;

        [Inject] private readonly PlayerSpawner _playerSpawner;
        [Inject] private readonly IPlayersController _playersController;
        
        private bool _skipUpdate;
        
        public override void OnEnter(GameStateType prevState)
        {
            base.OnEnter(prevState);
            _playersController.OnPlayerDataUpdated += OnPlayerUpdated;

            if (IsServer)
            {
                ClearReadyStateForPlayers();
                _playerSpawner.DespawnAllPlayers();
            }
        }

        private void ClearReadyStateForPlayers()
        {
            if (!IsServer)
                return;
            
            _skipUpdate = true;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                playerData.SetDataOnServer(PlayerDataConst.IsReady, false);
                playerData.SetDataOnServer(PlayerDataConst.InGame, false);
            }
            _skipUpdate = false;
        }

        private void OnPlayerUpdated(int playerId, IPlayerData data)
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
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (playerData.TryGetData(PlayerDataConst.IsReady, out bool ready) && ready)
                    readyPlayers++;
                else
                    notReadyPlayers++;
            }
                
            bool shouldStartGame = readyPlayers >= 1 && notReadyPlayers == 0;
            if (!shouldStartGame) return;

            SetReadyPlayersInGame();
            _playerSpawner.SpawnAllInGamePlayers();
            StateMachine.SetStateServer(GameStateType.Countdown);
        }
        
        private void SetReadyPlayersInGame()
        {
            _skipUpdate = true;
            foreach (var playerData in _playersController.GetAllPlayersData())
            {
                if (playerData.TryGetData(PlayerDataConst.IsReady, out bool ready) && ready)
                    playerData.SetDataOnServer(PlayerDataConst.InGame, true);
            }
            _skipUpdate = false;
        }
    }
}