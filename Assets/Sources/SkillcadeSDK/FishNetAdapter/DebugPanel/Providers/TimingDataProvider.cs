#if SKILLCADE_DEBUG
using System.Text;
using FishNet.Managing;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Providers
{
    public class TimingDataProvider : INetworkDebugDataProvider
    {
        public string SectionName => "Timing";

        public bool IsAvailable => _networkManager != null && _networkManager.TimeManager != null;

        [Inject] private readonly NetworkManager _networkManager;

        private readonly StringBuilder _sb = new StringBuilder(256);

        public string GetFormattedData()
        {
            _sb.Clear();

            var tm = _networkManager.TimeManager;

            _sb.AppendLine($"Tick Rate: {tm.TickRate} ticks/s");
            _sb.AppendLine($"Current Tick: {tm.Tick}");
            _sb.AppendLine($"Tick Delta: {tm.TickDelta * 1000:F2}ms");
            _sb.AppendLine($"Frame Ticked: {tm.FrameTicked}");

            if (_networkManager.IsServerStarted)
            {
                _sb.AppendLine($"Server Uptime: {tm.ServerUptime:F1}s");
            }

            if (_networkManager.IsClientStarted)
            {
                _sb.AppendLine($"Client Uptime: {tm.ClientUptime:F1}s");
            }

            return _sb.ToString();
        }
    }
}
#endif
