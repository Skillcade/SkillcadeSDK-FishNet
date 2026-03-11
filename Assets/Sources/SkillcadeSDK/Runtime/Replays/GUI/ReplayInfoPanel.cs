using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.Replays.GUI
{
    public class ReplayInfoPanel : MonoBehaviour
    {
        [Header("Replay info")]
        [SerializeField] private TMP_Text _matchDateText;
        [SerializeField] private TMP_Text _gameVersionText;
        [SerializeField] private TMP_Text _unityVersionText;
        [SerializeField] private TMP_Text _matchIdText;

        [Header("Players info")]
        [SerializeField] private ReplayPlayerInfoItem _playerInfoItemPrefab;
        [SerializeField] private Transform _playerInfoItemsParent;

        [Inject] private readonly ReplayReadService _replayReadService;

        private Dictionary<int, ReplayPlayerInfoItem> _playerInfoItems;

        private void Awake()
        {
            _playerInfoItems = new Dictionary<int, ReplayPlayerInfoItem>();
        }

        private void OnEnable()
        {
            if (_replayReadService == null || _replayReadService.ReplayInfo == null)
                return;

            _matchDateText.text = _replayReadService.ReplayInfo.StartDateTime.ToString("yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture);
            _gameVersionText.text = _replayReadService.ReplayInfo.GameVersion;
            _unityVersionText.text = _replayReadService.ReplayInfo.UnityVersion;
            _matchIdText.text = _replayReadService.ReplayInfo.MatchId;
        }

        public void SetPlayerNickname(int playerId, string nickname)
        {
            Debug.Log($"[ReplayInfoPanel] Set player {playerId} nickname {nickname}");
            var item = GetOrCreateItem(playerId);
            item.NicknameText.text = nickname;
        }

        public void SetPlayerPing(int playerId, int ping)
        {
            var item = GetOrCreateItem(playerId);
            item.PingText.text = $"{ping.ToString(CultureInfo.InvariantCulture)} ms";
        }

        private ReplayPlayerInfoItem GetOrCreateItem(int playerId)
        {
            if (_playerInfoItems.TryGetValue(playerId, out var item))
                return item;

            item = Instantiate(_playerInfoItemPrefab, _playerInfoItemsParent);
            item.PlayerId = playerId;
            _playerInfoItems.Add(playerId, item);
            return item;
        }
    }
}