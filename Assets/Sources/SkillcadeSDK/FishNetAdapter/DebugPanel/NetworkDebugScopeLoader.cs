#if SKILLCADE_DEBUG
using System;
using SkillcadeSDK.Connection;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel
{
    public class NetworkDebugScopeLoader : IInitializable, IDisposable
    {
        [Inject] private readonly IConnectionController _connectionController;
        [Inject] private readonly LifetimeScope _parentScope;
        
        private readonly string _debugSceneName;
        
        private bool _isLoaded;

        public NetworkDebugScopeLoader(string debugSceneName)
        {
            _debugSceneName = debugSceneName;
        }

        public void Initialize()
        {
            _connectionController.OnStateChanged += OnConnectionStateChanged;
            
            // Check current state in case we are already connected when this starts
            if (_connectionController.ConnectionState == ConnectionState.Connected)
            {
                LoadDebugScope();
            }
        }

        public void Dispose()
        {
            _connectionController.OnStateChanged -= OnConnectionStateChanged;
            UnloadDebugScope();
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                LoadDebugScope();
            }
            else if (state == ConnectionState.Disconnected)
            {
                UnloadDebugScope();
            }
        }

        private async void LoadDebugScope()
        {
            if (_isLoaded || string.IsNullOrEmpty(_debugSceneName))
                return;

            try
            {
                using (LifetimeScope.EnqueueParent(_parentScope))
                {
                    await SceneManager.LoadSceneAsync(_debugSceneName, LoadSceneMode.Additive);
                }
                _isLoaded = true;
                Debug.Log($"[NetworkDebugScopeLoader] Loaded debug scene: {_debugSceneName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkDebugScopeLoader] Failed to load debug scene '{_debugSceneName}': {e}");
            }
        }

        private void UnloadDebugScope()
        {
            if (!_isLoaded || string.IsNullOrEmpty(_debugSceneName))
                return;

            try
            {
                SceneManager.UnloadSceneAsync(_debugSceneName);
                _isLoaded = false;
                Debug.Log($"[NetworkDebugScopeLoader] Unloaded debug scene: {_debugSceneName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkDebugScopeLoader] Failed to unload debug scene '{_debugSceneName}': {e}");
            }
        }
    }
}
#endif
