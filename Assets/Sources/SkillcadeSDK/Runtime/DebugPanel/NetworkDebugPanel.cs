#if SKILLCADE_DEBUG
using System;
using System.Collections.Generic;
using SkillcadeSDK.FishNetAdapter.DebugPanel.Controls;
using SkillcadeSDK.FishNetAdapter.DebugPanel.Providers;
using SkillcadeSDK.FishNetAdapter.DebugPanel.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel
{
    public class NetworkDebugPanel : IInitializable, IDisposable, ITickable
    {
        [Inject] private readonly NetworkDebugConfig _config;
        [Inject] private readonly LatencySimulatorControl _latencyControl;
        [Inject] private readonly IReadOnlyList<INetworkDebugDataProvider> _dataProviders;
        [Inject] private readonly NetworkDebugPanelView _view;

        private bool _isVisible;
        private float _nextUpdateTime;

        public void Initialize()
        {
            Debug.Log($"[NetworkDebugPanel] Initialize, key: {_config.ToggleKey}");
            _isVisible = _config.ShowPanelOnStart;
            
            _view.gameObject.SetActive(_isVisible);
            _view.OnCloseClicked += HandleCloseClicked;
            UpdateVisibility();
        }

        public void Tick()
        {
            if (Input.GetKeyDown(_config.ToggleKey))
                ToggleVisibility();

            if (!_isVisible)
                return;

            if (Time.unscaledTime >= _nextUpdateTime)
            {
                _nextUpdateTime = Time.unscaledTime + _config.UpdateInterval;
                UpdateAllSections();
            }
        }

        public void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            UpdateVisibility();
        }

        public void Show()
        {
            _isVisible = true;
            UpdateVisibility();
        }

        public void Hide()
        {
            _isVisible = false;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            _view.gameObject.SetActive(_isVisible);
            if (_isVisible)
                UpdateAllSections();
        }

        private void HandleCloseClicked()
        {
            Hide();
        }

        private void UpdateAllSections()
        {
            foreach (var provider in _dataProviders)
            {
                if (provider.IsAvailable)
                    _view.UpdateSection(provider);
            }

            if (_config.ShowLatencySimulatorSection && _latencyControl != null)
                _view.UpdateLatencySimulator(_latencyControl);
        }

        public void Dispose()
        {
            _view.OnCloseClicked -= HandleCloseClicked;
        }
    }
}
#endif
