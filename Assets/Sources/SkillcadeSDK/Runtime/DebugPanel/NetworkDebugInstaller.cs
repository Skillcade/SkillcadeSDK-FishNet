using UnityEngine;
using VContainer;
using SkillcadeSDK.DI;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel
{
    public class NetworkDebugInstaller : MonoInstaller
    {
        [SerializeField] private string _debugSceneName;

        public override void Install(IContainerBuilder builder)
        {
#if SKILLCADE_DEBUG
            Debug.Log("[NetworkDebugInstaller] Install");
            if (string.IsNullOrEmpty(_debugSceneName))
            {
                Debug.LogWarning("[NetworkDebugInstaller] Debug scene name is empty, skipping registration.");
                return;
            }
            
            builder.Register<NetworkDebugScopeLoader>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .WithParameter(_debugSceneName);
#else
            Debug.Log("[NetworkDebugInstaller] Not install");
#endif
        }
    }
}
