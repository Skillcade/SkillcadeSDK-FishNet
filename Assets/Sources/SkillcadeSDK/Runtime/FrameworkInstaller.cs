using SkillcadeSDK.Common;
using SkillcadeSDK.Common.Level;
using SkillcadeSDK.Connection;
using SkillcadeSDK.DI;
using SkillcadeSDK.Replays;
using UnityEngine;
using VContainer;

#if UNITY_SERVER || UNITY_EDITOR
using SkillcadeSDK.ServerValidation;
using SkillcadeSDK.WebRequests;
#endif

namespace SkillcadeSDK
{
    public class FrameworkInstaller : MonoInstaller
    {
        [SerializeField] private WebBridge _webBridge;
        [SerializeField] private GameVersionConfig _gameVersionConfig;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.Register<LayerProvider>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<RespawnServiceProvider>(Lifetime.Singleton);
            builder.Register<ReplayWriteService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

#if UNITY_SERVER || UNITY_EDITOR
            builder.Register<WebRequester>(Lifetime.Singleton);
            builder.Register<SessionValidator>(Lifetime.Singleton);
            builder.Register<ServerPayloadController>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ReplaySendService>(Lifetime.Singleton);
#endif
            
            builder.RegisterInstance(_webBridge);
            builder.RegisterInstance(_gameVersionConfig);
        }
    }
}