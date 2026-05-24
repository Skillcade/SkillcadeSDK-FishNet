using FishNet.Object;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.Replays;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetPlayerDataEventWriter : NetworkBehaviour
    {
        [Inject] private readonly ReplayWriteService _replayWriteService;
        [Inject] private readonly FishNetPlayersController _playersController;
        [Inject] private readonly PlayerReconnectService _reconnectService;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            this.InjectToMe();
            
            if (!IsServerInitialized) // Only write this event on server, we don't need to write this data on clients replays
                return;

            int replayClientId = _reconnectService.ResolveReplayClientId(OwnerId);
            if (!_playersController.TryGetPlayerData(replayClientId, out var playerData))
            {
                Debug.LogWarning($"[FishNetPlayerDataEventWriter] No player data for OwnerId={OwnerId}, replayClientId={replayClientId}; skipping event");
                return;
            }
            
            if (!PlayerMatchData.TryGetFromPlayer(playerData, out var matchData))
                return;

            if (replayClientId != OwnerId)
                Debug.Log($"[PlayerReconnect] PlayerDataEvent: OwnerId={OwnerId} -> replayClientId={replayClientId} for player {matchData.PlayerId}");

            _replayWriteService.AddEvent(new FishNetPlayerDataEvent(replayClientId, ObjectId, matchData.Nickname, matchData.PlayerId));
        }
    }
}