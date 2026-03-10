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

        private ulong _clientInAccum;
        private ulong _clientOutAccum;
        private ulong _serverInAccum;
        private ulong _serverOutAccum;

        private ulong _clientInPerSec;
        private ulong _clientOutPerSec;
        private ulong _serverInPerSec;
        private ulong _serverOutPerSec;

        private float _lastResetTime;

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
                _serverInAccum += _serverInBytes;
                _serverOutAccum += _serverOutBytes;
            }

            if (clientTraffic != null)
            {
                _clientInBytes = clientTraffic.GetInboundTrafficBytes();
                _clientOutBytes = clientTraffic.GetOutboundTrafficBytes();
                _clientInAccum += _clientInBytes;
                _clientOutAccum += _clientOutBytes;
            }

            float now = UnityEngine.Time.unscaledTime;
            if (now - _lastResetTime >= 1f)
            {
                _clientInPerSec = _clientInAccum;
                _clientOutPerSec = _clientOutAccum;
                _serverInPerSec = _serverInAccum;
                _serverOutPerSec = _serverOutAccum;

                _clientInAccum = 0;
                _clientOutAccum = 0;
                _serverInAccum = 0;
                _serverOutAccum = 0;
                _lastResetTime = now;
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
                _sb.AppendLine($"Client In:  {FormatBytes(_clientInBytes)}/tick  ({FormatBytes(_clientInPerSec)}/s)");
                _sb.AppendLine($"Client Out: {FormatBytes(_clientOutBytes)}/tick  ({FormatBytes(_clientOutPerSec)}/s)");
            }

            if (_networkManager.IsServerStarted)
            {
                _sb.AppendLine($"Server In:  {FormatBytes(_serverInBytes)}/tick  ({FormatBytes(_serverInPerSec)}/s)");
                _sb.AppendLine($"Server Out: {FormatBytes(_serverOutBytes)}/tick  ({FormatBytes(_serverOutPerSec)}/s)");
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
