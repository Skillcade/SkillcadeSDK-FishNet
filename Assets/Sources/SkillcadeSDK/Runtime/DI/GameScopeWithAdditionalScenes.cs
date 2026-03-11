using System.Collections.Generic;
using SkillcadeSDK.Connection;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.DI
{
    public class GameScopeWithAdditionalScenes : LifetimeScope
    {
        [SerializeField] private string[] _sceneNames;
        [SerializeField] private MonoInstaller[] _rootInstallers;
        [SerializeField] private ConnectionConfig _connectionConfig;
        
        private List<MonoInstaller> _loadedInstallers;
        
        protected override void Awake()
        {
            LoadScenesAndBuildAsync();
        }

        private async void LoadScenesAndBuildAsync()
        {
            // Combine scenes from GameScope and ConnectionConfig
            var scenesToLoad = new List<string>(_sceneNames);

            if (_connectionConfig != null && _connectionConfig.SceneNames != null)
            {
                scenesToLoad.AddRange(_connectionConfig.SceneNames);
            }

            // Remove duplicates if any
            var uniqueScenes = new HashSet<string>(scenesToLoad);

            foreach (var sceneName in uniqueScenes)
            {
                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }

            _loadedInstallers = new List<MonoInstaller>();
            foreach (var sceneName in uniqueScenes)
            {
                var scene = SceneManager.GetSceneByName(sceneName);
                if (!scene.IsValid())
                {
                    Debug.LogError($"[GameScopeWithAdditionalScenes] Scene {sceneName} not valid");
                    continue;
                }

                if (!scene.isLoaded)
                {
                    Debug.LogError($"[GameScopeWithAdditionalScenes] Scene {sceneName} not loaded");
                    continue;
                }
                
                var rootObjects = scene.GetRootGameObjects();
                Debug.Log($"[GameScopeWithAdditionalScenes] Scene {sceneName} has {rootObjects.Length} objects");
                foreach (var rootObject in rootObjects)
                {
                    _loadedInstallers.AddRange(rootObject.GetComponents<MonoInstaller>());
                }
            }
            
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.Register<ContainerSingletonWrapper>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterBuildCallback(AutoInjectTargets);

            if (_connectionConfig != null)
            {
                builder.RegisterInstance(_connectionConfig);
            }
            
            foreach (var installer in _rootInstallers)
            {
                Debug.Log($"[GameScopeWithAdditionalScenes] install {installer.GetType().Name}");
                installer.Install(builder);
            }
            
            foreach (var installer in _loadedInstallers)
            {
                Debug.Log($"[GameScopeWithAdditionalScenes] install {installer.GetType().Name}");
                installer.Install(builder);
            }
        }

        private void AutoInjectTargets(IObjectResolver objectResolver)
        {
            foreach (var installer in _rootInstallers)
            {
                foreach (var autoInjectGameObject in installer.GetAutoInjectGameObjects())
                {
                    objectResolver.InjectGameObject(autoInjectGameObject);
                }
            }
            
            foreach (var installer in _loadedInstallers)
            {
                foreach (var autoInjectGameObject in installer.GetAutoInjectGameObjects())
                {
                    objectResolver.InjectGameObject(autoInjectGameObject);
                }
            }
        }
    }
}