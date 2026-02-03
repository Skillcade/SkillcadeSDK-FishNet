#if SKILLCADE_DEBUG
using System;
using System.Text;
using FishNet.Managing;
using SkillcadeSDK.FishNetAdapter.PingService;
using SkillcadeSDK.FishNetAdapter.Players;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Providers
{
    public class PingDataProvider : INetworkDebugDataProvider, IInitializable, IDisposable
    {
        public string SectionName => "Ping / RTT";

        public bool IsAvailable => _networkManager != null &&
            (_networkManager.IsClientStarted || _networkManager.IsServerStarted);

        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly FishNetPlayersController _playersController;

        private long _currentRtt;
        private long _minRtt = long.MaxValue;
        private long _maxRtt;
        private readonly StringBuilder _sb = new StringBuilder(256);

        public void Initialize()
        {
            if (_networkManager?.TimeManager != null)
            {
                _networkManager.TimeManager.OnRoundTripTimeUpdated += OnRttUpdated;
            }
        }

        private void OnRttUpdated(long rtt)
        {
            _currentRtt = rtt;
            if (rtt > 0)
            {
                _minRtt = Math.Min(_minRtt, rtt);
                _maxRtt = Math.Max(_maxRtt, rtt);
            }
        }

        public string GetFormattedData()
        {
            _sb.Clear();

            if (_networkManager.IsClientStarted)
            {
                var tm = _networkManager.TimeManager;
                _sb.AppendLine($"RTT: {_currentRtt}ms (half: {tm.HalfRoundTripTime}ms)");
                _sb.AppendLine($"Min/Max: {(_minRtt == long.MaxValue ? 0 : _minRtt)}ms / {_maxRtt}ms");
            }

            if (_networkManager.IsServerStarted && _playersController != null)
            {
                _sb.AppendLine("-- Player Pings --");
                foreach (var playerData in _playersController.GetAllPlayersData())
                {
                    if (PlayerPingData.TryGetFromPlayer(playerData, out var pingData))
                    {
                        _sb.AppendLine($"  Client {playerData.PlayerNetworkId}: {pingData.PingInMs}ms");
                    }
                }
            }

            return _sb.ToString();
        }

        public void Dispose()
        {
            if (_networkManager?.TimeManager != null)
            {
                _networkManager.TimeManager.OnRoundTripTimeUpdated -= OnRttUpdated;
            }
        }
    }
}
#endif
