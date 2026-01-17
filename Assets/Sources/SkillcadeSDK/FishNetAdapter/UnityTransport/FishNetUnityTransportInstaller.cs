using FishNet.Transporting.UTP;
using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    public class FishNetUnityTransportInstaller : MonoInstaller
    {
        [SerializeField] private UnityTransport _transport;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_transport);
            builder.Register<FishNetUnityTransportAdapter>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}