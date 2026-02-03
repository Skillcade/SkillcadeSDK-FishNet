#if SKILLCADE_DEBUG
using System;
using System.Collections.Generic;
using SkillcadeSDK.FishNetAdapter.DebugPanel.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Views
{
    public class NetworkDebugPanelView : MonoBehaviour
    {
        public event Action OnCloseClicked;

        [Header("Main Panel")]
        [SerializeField] private Button _closeButton;

        [Header("Sections")]
        [SerializeField] private DebugSectionView _connectionSection;
        [SerializeField] private DebugSectionView _timingSection;
        [SerializeField] private DebugSectionView _pingSection;
        [SerializeField] private DebugSectionView _bandwidthSection;
        [SerializeField] private DebugSectionView _packetStatsSection;

        [Header("Latency Simulator")]
        [SerializeField] private LatencySimulatorView _latencySimulatorView;

        private readonly Dictionary<string, DebugSectionView> _sectionMap = new Dictionary<string, DebugSectionView>();

        private void Awake()
        {
            InitializeSectionMap();
            _closeButton.onClick.AddListener(HandleCloseClick);
        }

        private void InitializeSectionMap()
        {
            if (_connectionSection != null)
                _sectionMap["Connection"] = _connectionSection;

            if (_timingSection != null)
                _sectionMap["Timing"] = _timingSection;

            if (_pingSection != null)
                _sectionMap["Ping / RTT"] = _pingSection;

            if (_bandwidthSection != null)
                _sectionMap["Bandwidth"] = _bandwidthSection;

            if (_packetStatsSection != null)
                _sectionMap["Packet Statistics"] = _packetStatsSection;
        }

        public void UpdateSection(string sectionName, string content)
        {
            if (_sectionMap.TryGetValue(sectionName, out var section))
                section.SetContent(content);
        }

        public void SetSectionVisible(string sectionName, bool visible)
        {
            if (_sectionMap.TryGetValue(sectionName, out var section))
                section.gameObject.SetActive(visible);
        }

        public void UpdateLatencySimulator(LatencySimulatorControl control)
        {
            _latencySimulatorView.UpdateFromControl(control);
        }

        private void HandleCloseClick()
        {
            OnCloseClicked?.Invoke();
        }
    }
}
#endif
