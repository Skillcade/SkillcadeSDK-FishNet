#if SKILLCADE_DEBUG
using System.Text;
using FishNet.Managing;
using FishNet.Transporting;
using SkillcadeSDK.FishNetAdapter.PingService;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.StatsOverlay
{
    public class NetworkStatsOverlay : IInitializable, ITickable
    {
        private const KeyCode ToggleKey = KeyCode.O;
        private const float UpdateInterval = 0.25f;

        [Inject] private readonly NetworkManager _networkManager;
        [Inject] private readonly FishNetPlayersController _playersController;
        [Inject] private readonly NetworkStatsOverlayView _view;

        private bool _isVisible;
        private float _nextUpdateTime;
        private readonly StringBuilder _sb = new StringBuilder(512);

        public void Initialize()
        {
            _isVisible = false;
            _view.SetVisible(_isVisible);
        }

        public void Tick()
        {
            if (Input.GetKeyDown(ToggleKey))
                ToggleVisibility();

            if (!_isVisible)
                return;

            if (Time.unscaledTime >= _nextUpdateTime)
            {
                _nextUpdateTime = Time.unscaledTime + UpdateInterval;
                UpdateDisplay();
            }
        }

        private void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            _view.SetVisible(_isVisible);

            if (_isVisible)
                UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            _sb.Clear();

            // Local ping
            long rtt = _networkManager.TimeManager.RoundTripTime;
            _sb.Append("PING: ");
            _sb.Append(rtt);
            _sb.Append("ms");

            // Packet loss
            Transport transport = _networkManager.TransportManager.Transport;
            if (transport != null)
            {
                float loss = transport.GetPacketLoss(asServer: _networkManager.IsServerStarted && !_networkManager.IsClientStarted);
                _sb.Append(" | LOSS: ");
                _sb.AppendFormat("{0:F1}%", loss);
            }

            // Per-player pings
            if (_playersController != null)
            {
                _sb.AppendLine();
                _sb.AppendLine("---");

                int localPlayerId = _playersController.LocalPlayerId;

                foreach (var playerData in _playersController.GetAllPlayersData())
                {
                    bool isLocal = playerData.PlayerNetworkId == localPlayerId;

                    // Get nickname
                    string nickname;
                    if (isLocal)
                        nickname = "You";
                    else if (PlayerMatchData.TryGetFromPlayer(playerData, out var matchData) && !string.IsNullOrEmpty(matchData.Nickname))
                        nickname = matchData.Nickname;
                    else
                        nickname = $"Player {playerData.PlayerNetworkId}";

                    // Get ping
                    int ping = 0;
                    if (PlayerPingData.TryGetFromPlayer(playerData, out var pingData))
                        ping = pingData.PingInMs;

                    // Format line
                    _sb.Append(isLocal ? "* " : "  ");

                    if (nickname.Length > 20)
                        _sb.Append(nickname.Substring(0, 20));
                    else
                        _sb.Append(nickname.PadRight(20));

                    _sb.Append(" ");
                    _sb.Append(ping);
                    _sb.AppendLine("ms");
                }
            }

            _view.SetContent(_sb.ToString());
        }
    }
}
#endif
