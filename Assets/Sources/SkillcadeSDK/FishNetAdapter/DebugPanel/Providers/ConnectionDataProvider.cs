#if SKILLCADE_DEBUG
using System.Text;
using FishNet.Managing;
using SkillcadeSDK.Connection;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Providers
{
    public class ConnectionDataProvider : INetworkDebugDataProvider
    {
        public string SectionName => "Connection";

        public bool IsAvailable => _networkManager != null;

        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly IConnectionController _connectionController;

        private readonly StringBuilder _sb = new StringBuilder(256);

        public string GetFormattedData()
        {
            _sb.Clear();

            if (_connectionController != null)
            {
                _sb.AppendLine($"State: {_connectionController.ConnectionState}");
            }

            _sb.AppendLine($"Is Server: {_networkManager.IsServerStarted}");
            _sb.AppendLine($"Is Client: {_networkManager.IsClientStarted}");
            _sb.AppendLine($"Is Host: {_networkManager.IsHostStarted}");

            if (_networkManager.IsClientStarted)
            {
                var conn = _networkManager.ClientManager.Connection;
                _sb.AppendLine($"Client ID: {conn.ClientId}");
                _sb.AppendLine($"Valid: {conn.IsValid} | Active: {conn.IsActive}");
            }

            if (_networkManager.IsServerStarted)
            {
                int clientCount = _networkManager.ServerManager.Clients.Count;
                _sb.AppendLine($"Connected Clients: {clientCount}");
            }

            return _sb.ToString();
        }
    }
}
#endif
