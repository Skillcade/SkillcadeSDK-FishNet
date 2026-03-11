using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    public class FishNetSinglePlayerTransportInstaller : MonoInstaller
    {
        [SerializeField] private SinglePlayerOfflineTransport _transport;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_transport);
            builder.Register<FishNetSinglePlayerTransportAdapter>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}