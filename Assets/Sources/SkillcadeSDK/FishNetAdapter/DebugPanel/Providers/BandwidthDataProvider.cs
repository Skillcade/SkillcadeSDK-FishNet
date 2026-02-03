#if SKILLCADE_DEBUG
using System;
using System.Text;
using FishNet.Editing;
using FishNet.Managing;
using FishNet.Managing.Statistic;
using Sources.SkillcadeSDK.FishNetAdapter.DebugPanel.Providers.FishNet;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Providers
{
    public class BandwidthDataProvider : INetworkDebugDataProvider, IInitializable, IDisposable
    {
        public string SectionName => "Bandwidth";

        public bool IsAvailable => _networkManager != null &&
            (_networkManager.IsClientStarted || _networkManager.IsServerStarted);

        [Inject] private readonly NetworkManager _networkManager;

        private NetworkTrafficStatistics _statistics;
        private ulong _clientInBytes;
        private ulong _clientOutBytes;
        private ulong _serverInBytes;
        private ulong _serverOutBytes;
        private readonly StringBuilder _sb = new StringBuilder(256);

        public void Initialize()
        {
            if (_networkManager.StatisticsManager == null)
                return;
            
            _networkManager.StatisticsManager.TryGetNetworkTrafficStatistics(out _statistics);
            if (_statistics != null)
            {
                _statistics.OnNetworkTraffic += OnNetworkTraffic;
            }
        }

        private void OnNetworkTraffic(uint tick, BidirectionalNetworkTraffic serverTraffic,
            BidirectionalNetworkTraffic clientTraffic)
        {
            if (serverTraffic != null)
            {
                _serverInBytes = serverTraffic.GetInboundTrafficBytes();
                _serverOutBytes = serverTraffic.GetOutboundTrafficBytes();
            }

            if (clientTraffic != null)
            {
                _clientInBytes = clientTraffic.GetInboundTrafficBytes();
                _clientOutBytes = clientTraffic.GetOutboundTrafficBytes();
            }
        }

        public string GetFormattedData()
        {
            _sb.Clear();

            if (_statistics == null)
            {
                _sb.AppendLine("Statistics not available");
                _sb.AppendLine("Enable in StatisticsManager");
                return _sb.ToString();
            }

            if (_networkManager.IsClientStarted)
            {
                _sb.AppendLine($"Client In:  {FormatBytes(_clientInBytes)}/tick");
                _sb.AppendLine($"Client Out: {FormatBytes(_clientOutBytes)}/tick");
            }

            if (_networkManager.IsServerStarted)
            {
                _sb.AppendLine($"Server In:  {FormatBytes(_serverInBytes)}/tick");
                _sb.AppendLine($"Server Out: {FormatBytes(_serverOutBytes)}/tick");
            }

            return _sb.ToString();
        }

        public void Dispose()
        {
            if (_statistics != null)
                _statistics.OnNetworkTraffic -= OnNetworkTraffic;
        }

        private static string FormatBytes(ulong bytes)
        {
            return NetworkTrafficStatistics.FormatBytesToLargest(bytes);
        }
    }
}
#endif
