#if SKILLCADE_DEBUG
using System;
using System.Text;
using FishNet.Managing;
using FishNet.Transporting;
using SkillcadeSDK.Connection;
using SkillcadeSDK.FishNetAdapter.DebugPanel.Providers;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.ServerStats
{
    public class ServerPerformanceDataProvider : INetworkDebugDataProvider, IInitializable, IDisposable
    {
        public string SectionName => "Server Performance";
        public bool IsAvailable => _hasData;

        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly IConnectionController _connectionController;

        private ServerPerformanceBroadcast _latestData;
        private bool _hasData;
        private bool _subscribed;
        private readonly StringBuilder _sb = new(256);

        public void Initialize()
        {
            _connectionController.OnStateChanged += OnConnectionStateChanged;
            if (_connectionController.ConnectionState.IsConnectedOrHosting())
                Subscribe();
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            if (state.IsConnectedOrHosting())
                Subscribe();
            else
            {
                Unsubscribe();
                _hasData = false;
            }
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            if (_networkManager.ClientManager != null)
                _networkManager.ClientManager.RegisterBroadcast<ServerPerformanceBroadcast>(OnServerPerformance);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_networkManager.ClientManager != null)
                _networkManager.ClientManager.UnregisterBroadcast<ServerPerformanceBroadcast>(OnServerPerformance);
            _subscribed = false;
        }

        private void OnServerPerformance(ServerPerformanceBroadcast broadcast, Channel channel)
        {
            _latestData = broadcast;
            _hasData = true;
        }

        public string GetFormattedData()
        {
            _sb.Clear();

            _sb.Append("Tick Duration: ");
            _sb.AppendFormat("{0:F2}", _latestData.TickDurationMs);
            _sb.Append("ms / ");
            _sb.AppendFormat("{0:F2}", _latestData.ExpectedTickDeltaMs);
            _sb.AppendLine("ms");

            float load = _latestData.ExpectedTickDeltaMs > 0
                ? _latestData.TickDurationMs / _latestData.ExpectedTickDeltaMs * 100f
                : 0f;
            _sb.Append("Tick Load: ");
            _sb.AppendFormat("{0:F1}", load);
            _sb.AppendLine("%");

            _sb.Append("Tick Overruns: ");
            _sb.AppendLine(_latestData.TickOverrunCount.ToString());

            _sb.Append("Server FPS: ");
            _sb.AppendFormat("{0:F0}", _latestData.ServerFps);
            _sb.AppendLine();

            _sb.Append("Clients: ");
            _sb.AppendLine(_latestData.ConnectedClients.ToString());

            _sb.Append("Server Uptime: ");
            _sb.AppendFormat("{0:F0}", _latestData.ServerUptime);
            _sb.Append("s");

            return _sb.ToString();
        }

        public void Dispose()
        {
            _connectionController.OnStateChanged -= OnConnectionStateChanged;
            Unsubscribe();
        }
    }
}
#endif
