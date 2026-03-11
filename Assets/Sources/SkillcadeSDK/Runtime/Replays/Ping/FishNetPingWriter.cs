using SkillcadeSDK.FishNetAdapter.PingService;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.Replays;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetPingWriter
    {
        [Inject] private readonly FishNetPlayersController _playersController;
        
        private int _ping = -1;
        
        public void Write(ReplayWriter writer, int ownerId)
        {
            if (_playersController == null)
                this.InjectToMe();
            
            UpdatePingFromData(ownerId);
            writer.WriteUshort((ushort)_ping);
        }

        private void UpdatePingFromData(int ownerId)
        {
            _ping = -1;
            if (!_playersController.TryGetPlayerData(ownerId, out var playerData))
                return;
            
            if (!PlayerPingData.TryGetFromPlayer(playerData, out var pingData))
                return;
            
            _ping = pingData.PingInMs;
        }
    }
}