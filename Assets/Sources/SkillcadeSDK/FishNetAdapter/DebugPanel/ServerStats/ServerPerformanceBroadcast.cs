#if SKILLCADE_DEBUG
using FishNet.Broadcast;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.ServerStats
{
    public struct ServerPerformanceBroadcast : IBroadcast
    {
        public float TickDurationMs;
        public float ExpectedTickDeltaMs;
        public int TickOverrunCount;
        public int ConnectedClients;
        public float ServerUptime;
        public float ServerFps;
    }
}
#endif
