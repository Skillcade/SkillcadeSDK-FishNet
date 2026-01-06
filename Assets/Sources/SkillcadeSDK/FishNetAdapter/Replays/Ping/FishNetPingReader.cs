using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetPingReader
    {
        [Inject] private readonly FishNetReplayPlayerDataService _fishNetReplayPlayerDataService;

        public void SetPing(int objectId, int ping)
        {
            _fishNetReplayPlayerDataService!.SetPlayerPing(objectId, ping);
        }
    }
}