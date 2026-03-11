#if SKILLCADE_DEBUG
using System.Text;
using FishNet.Managing;
using FishNet.Transporting;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Providers
{
    public class PacketStatisticsProvider : INetworkDebugDataProvider
    {
        public string SectionName => "Packet Statistics";

        public bool IsAvailable => _networkManager != null && (_networkManager.IsClientStarted || _networkManager.IsServerStarted);

        [Inject] private readonly NetworkManager _networkManager;

        private readonly StringBuilder _sb = new StringBuilder(256);

        public string GetFormattedData()
        {
            _sb.Clear();

            Transport transport = _networkManager.TransportManager.Transport;
            if (transport == null)
            {
                _sb.AppendLine("Transport not available");
                return _sb.ToString();
            }

            if (_networkManager.IsClientStarted)
            {
                float clientLoss = transport.GetPacketLoss(asServer: false);
                _sb.AppendLine($"Client Packet Loss: {clientLoss:F1}%");
            }

            if (_networkManager.IsServerStarted)
            {
                float serverLoss = transport.GetPacketLoss(asServer: true);
                _sb.AppendLine($"Server Packet Loss: {serverLoss:F1}%");
            }

            int mtu = _networkManager.TransportManager.GetLowestMTU();
            _sb.AppendLine($"MTU (lowest): {mtu} bytes");

            return _sb.ToString();
        }
    }
}
#endif
