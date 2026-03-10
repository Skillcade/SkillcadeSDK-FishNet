#if SKILLCADE_DEBUG
using System;
using System.Collections.Generic;
using SkillcadeSDK.FishNetAdapter.DebugPanel.Controls;
using SkillcadeSDK.FishNetAdapter.DebugPanel.Providers;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel.Views
{
    public class NetworkDebugPanelView : MonoBehaviour
    {
        public event Action OnCloseClicked;

        [Header("Main Panel")]
        [SerializeField] private Button _closeButton;

        [Header("Sections")]
        [SerializeField] private DebugSectionView _sectionPrefab;
        [SerializeField] private Transform _sectionsParent;

        [Header("Latency Simulator")]
        [SerializeField] private LatencySimulatorView _latencySimulatorView;
        
        private readonly Dictionary<string, DebugSectionView> _sectionMap = new Dictionary<string, DebugSectionView>();

        private void Awake()
        {
            _closeButton.onClick.AddListener(HandleCloseClick);
        }

        public void UpdateSection(INetworkDebugDataProvider sectionProvider)
        {
            if (!_sectionMap.TryGetValue(sectionProvider.SectionName, out var section))
            {
                section = Instantiate(_sectionPrefab, _sectionsParent);
                section.SetName(sectionProvider.SectionName);
                _sectionMap.Add(sectionProvider.SectionName, section);
            }
            
            section.SetContent(sectionProvider.GetFormattedData());
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
