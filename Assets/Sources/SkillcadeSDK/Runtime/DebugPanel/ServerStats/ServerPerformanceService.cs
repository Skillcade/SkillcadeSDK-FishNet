#if SKILLCADE_DEBUG
using System;
using System.Diagnostics;
using FishNet.Managing;
using SkillcadeSDK.Connection;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.ServerStats
{
    public class ServerPerformanceService : IInitializable, IDisposable
    {
        private const float BroadcastIntervalSeconds = 1f;

        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly IConnectionController _connectionController;

        private readonly Stopwatch _tickStopwatch = new();
        private float _lastTickDurationMs;
        private int _tickOverrunCount;
        private float _smoothedFps;
        private float _nextBroadcastTime;
        private bool _subscribed;

        public void Initialize()
        {
            _connectionController.OnStateChanged += OnConnectionStateChanged;
            if (_connectionController.ConnectionState == ConnectionState.Hosting)
                Subscribe();
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            if (state == ConnectionState.Hosting)
                Subscribe();
            else
                Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _networkManager.TimeManager.OnPreTick += OnPreTick;
            _networkManager.TimeManager.OnPostTick += OnPostTick;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _networkManager.TimeManager.OnPreTick -= OnPreTick;
            _networkManager.TimeManager.OnPostTick -= OnPostTick;
            _subscribed = false;
        }

        private void OnPreTick()
        {
            _tickStopwatch.Restart();
        }

        private void OnPostTick()
        {
            _tickStopwatch.Stop();
            _lastTickDurationMs = (float)_tickStopwatch.Elapsed.TotalMilliseconds;

            float expectedMs = (float)_networkManager.TimeManager.TickDelta * 1000f;
            if (_lastTickDurationMs > expectedMs)
                _tickOverrunCount++;

            // Smooth FPS
            float currentFps = 1f / UnityEngine.Time.unscaledDeltaTime;
            _smoothedFps = UnityEngine.Mathf.Lerp(_smoothedFps, currentFps, 0.1f);

            // Broadcast periodically
            if (UnityEngine.Time.unscaledTime >= _nextBroadcastTime)
            {
                _nextBroadcastTime = UnityEngine.Time.unscaledTime + BroadcastIntervalSeconds;
                BroadcastStats();
            }
        }

        private void BroadcastStats()
        {
            if (!_networkManager.IsServerStarted) return;

            var broadcast = new ServerPerformanceBroadcast
            {
                TickDurationMs = _lastTickDurationMs,
                ExpectedTickDeltaMs = (float)_networkManager.TimeManager.TickDelta * 1000f,
                TickOverrunCount = _tickOverrunCount,
                ConnectedClients = _networkManager.ServerManager.Clients.Count,
                ServerUptime = _networkManager.TimeManager.ServerUptime,
                ServerFps = _smoothedFps
            };

            foreach (var client in _networkManager.ServerManager.Clients)
                _networkManager.ServerManager.Broadcast(client.Value, broadcast, true, FishNet.Transporting.Channel.Unreliable);
        }

        public void Dispose()
        {
            _connectionController.OnStateChanged -= OnConnectionStateChanged;
            Unsubscribe();
        }
    }
}
#endif
