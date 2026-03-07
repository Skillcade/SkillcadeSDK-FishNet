using FishNet.Managing;
using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    public class FishNetWebRTCTransportInstaller : MonoInstaller
    {
        [SerializeField] private NetworkManager _networkManager;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_networkManager);
            builder.Register<FishNetWebRTCTransportAdapter>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
