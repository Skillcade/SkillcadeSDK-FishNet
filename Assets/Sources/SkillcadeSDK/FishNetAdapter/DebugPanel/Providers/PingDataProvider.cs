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

        private const int MaxSamples = 128;
        private const float WindowSeconds = 5f;

        private long _currentRtt;
        private long _minRtt = long.MaxValue;
        private long _maxRtt;

        private readonly long[] _rttSamples = new long[MaxSamples];
        private readonly float[] _rttTimestamps = new float[MaxSamples];
        private int _rttSampleIndex;
        private int _rttSampleCount;

        private long _currentRttCustom;
        private long _minRttCustom = long.MaxValue;
        private long _maxRttCustom;

        private readonly long[] _rttCustomSamples = new long[MaxSamples];
        private readonly float[] _rttCustomTimestamps = new float[MaxSamples];
        private int _rttCustomSampleIndex;
        private int _rttCustomSampleCount;

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

            AddSample(_rttCustomSamples, _rttCustomTimestamps, ref _rttCustomSampleIndex,
                ref _rttCustomSampleCount, _currentRttCustom);
        }

        private void OnRttUpdated(long rtt)
        {
            _currentRtt = rtt;
            if (rtt > 0)
            {
                _minRtt = Math.Min(_minRtt, rtt);
                _maxRtt = Math.Max(_maxRtt, rtt);
                AddSample(_rttSamples, _rttTimestamps, ref _rttSampleIndex, ref _rttSampleCount, rtt);
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

                GetWindowStats(_rttSamples, _rttTimestamps, _rttSampleIndex, _rttSampleCount,
                    out var wMin, out var wMax, out var wAvg);
                if (wAvg > 0)
                    _sb.AppendLine($"Recent 5s: {wMin.ToString()}-{wMax.ToString()}ms avg:{wAvg.ToString()}ms");

                if (_currentRttCustom > 0)
                {
                    _sb.AppendLine();
                    _sb.AppendLine("RTT from custom ping sync:");
                    _sb.AppendLine($"RTT: {_currentRttCustom.ToString()}ms");
                    _sb.AppendLine($"Min/Max: {(_minRttCustom == long.MaxValue ? 0 : _minRttCustom).ToString()}ms / {_maxRttCustom.ToString()}ms");

                    GetWindowStats(_rttCustomSamples, _rttCustomTimestamps, _rttCustomSampleIndex,
                        _rttCustomSampleCount, out var wcMin, out var wcMax, out var wcAvg);
                    if (wcAvg > 0)
                        _sb.AppendLine($"Recent 5s: {wcMin.ToString()}-{wcMax.ToString()}ms avg:{wcAvg.ToString()}ms");
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

        private static void AddSample(long[] samples, float[] timestamps, ref int index, ref int count, long value)
        {
            samples[index] = value;
            timestamps[index] = UnityEngine.Time.unscaledTime;
            index = (index + 1) % MaxSamples;
            if (count < MaxSamples) count++;
        }

        private static void GetWindowStats(long[] samples, float[] timestamps, int index, int count,
            out long windowMin, out long windowMax, out long windowAvg)
        {
            windowMin = 0;
            windowMax = 0;
            windowAvg = 0;

            float cutoff = UnityEngine.Time.unscaledTime - WindowSeconds;
            long sum = 0;
            int valid = 0;
            long min = long.MaxValue;
            long max = 0;

            for (int i = 0; i < count; i++)
            {
                int idx = (index - 1 - i + MaxSamples * 2) % MaxSamples;
                if (timestamps[idx] < cutoff) break;

                long val = samples[idx];
                sum += val;
                valid++;
                if (val < min) min = val;
                if (val > max) max = val;
            }

            if (valid > 0)
            {
                windowMin = min;
                windowMax = max;
                windowAvg = sum / valid;
            }
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
