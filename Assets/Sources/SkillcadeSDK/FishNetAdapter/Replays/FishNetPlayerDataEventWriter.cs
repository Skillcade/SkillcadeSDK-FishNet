using FishNet.Object;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.Replays;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetPlayerDataEventWriter : NetworkBehaviour
    {
        [Inject] private readonly ReplayWriteService _replayWriteService;
        [Inject] private readonly FishNetPlayersController _playersController;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            this.InjectToMe();
            
            if (!IsServerInitialized) // Only write this event on server, we don't need to write this data on clients replays
                return;
            
            if (!_playersController.TryGetPlayerData(OwnerId, out var playerData))
                return;
            
            if (!PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                return;
            
            _replayWriteService.AddEvent(new FishNetPlayerDataEvent(OwnerId, ObjectId, matchData.Nickname, matchData.PlayerId));
        }
    }
}