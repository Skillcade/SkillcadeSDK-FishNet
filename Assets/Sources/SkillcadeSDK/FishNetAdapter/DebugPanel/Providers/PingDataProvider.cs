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
        
        private long _currentRttCustom;
        private long _minRttCustom = long.MaxValue;
        private long _maxRttCustom;
        
        private readonly StringBuilder _sb = new StringBuilder(256);

        public void Initialize()
        {
            if (_networkManager.TimeManager != null)
                _networkManager.TimeManager.OnRoundTripTimeUpdated += OnRttUpdated;

            _playersController.OnPlayerDataUpdated += OnPlayerDataUpdated;
        }

        private void OnPlayerDataUpdated(int playerId, FishNetPlayerData data)
        {
            if (playerId != _playersController.LocalPlayerId)
                return;

            if (!PlayerPingData.TryGetFromPlayer(data, out var pingData))
                return;
            
            _currentRttCustom = pingData.PingInMs;
            _minRttCustom = Math.Min(_minRttCustom, _currentRttCustom);
            _maxRttCustom = Math.Max(_maxRttCustom, _currentRttCustom);
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
                _sb.AppendLine("RTT from FishNet:");
                _sb.AppendLine($"RTT: {_currentRtt.ToString()}ms (half: {tm.HalfRoundTripTime.ToString()}ms)");
                _sb.AppendLine($"Min/Max: {(_minRtt == long.MaxValue ? 0 : _minRtt).ToString()}ms / {_maxRtt.ToString()}ms");

                if (_currentRttCustom > 0)
                {
                    _sb.AppendLine();
                    _sb.AppendLine("RTT from custom ping sync:");
                    _sb.AppendLine($"RTT: {_currentRttCustom.ToString()}ms");
                    _sb.AppendLine($"Min/Max: {(_minRttCustom == long.MaxValue ? 0 : _minRttCustom).ToString()}ms / {_maxRttCustom.ToString()}ms");
                }
            }

            if (_networkManager.IsServerStarted && _playersController != null)
            {
                _sb.AppendLine("-- Player Pings --");
                foreach (var playerData in _playersController.GetAllPlayersData())
                {
                    if (PlayerPingData.TryGetFromPlayer(playerData, out var pingData))
                    {
                        _sb.AppendLine($"  Client {playerData.PlayerNetworkId.ToString()}: {pingData.PingInMs.ToString()}ms");
                    }
                }
            }

            return _sb.ToString();
        }

        public void Dispose()
        {
            if (_networkManager.TimeManager != null)
                _networkManager.TimeManager.OnRoundTripTimeUpdated -= OnRttUpdated;
            
            _playersController.OnPlayerDataUpdated -= OnPlayerDataUpdated;
        }
    }
}
#endif
