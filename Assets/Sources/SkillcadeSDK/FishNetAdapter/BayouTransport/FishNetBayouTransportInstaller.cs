using FishNet.Transporting.Bayou;
using SkillcadeSDK.DI;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.BayouTransport
{
    public class FishNetBayouTransportInstaller : MonoInstaller
    {
        [SerializeField] private Bayou _transport;

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_transport);
            builder.Register<FishNetBayouTransportAdapter>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}