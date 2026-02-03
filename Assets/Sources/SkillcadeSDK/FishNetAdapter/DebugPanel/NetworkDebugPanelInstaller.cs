#if SKILLCADE_DEBUG
using SkillcadeSDK.DI;
using SkillcadeSDK.FishNetAdapter.DebugPanel.Controls;
using SkillcadeSDK.FishNetAdapter.DebugPanel.Providers;
using SkillcadeSDK.FishNetAdapter.DebugPanel.Views;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.DebugPanel
{
    public class NetworkDebugPanelInstaller : MonoInstaller
    {
        [SerializeField] private NetworkDebugPanelView _debugPanelView;
        [SerializeField] private NetworkDebugConfig _config;

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_config);
            builder.RegisterInstance(_debugPanelView);

            builder.Register<NetworkDebugPanel>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            
            // Data Providers
            builder.Register<PingDataProvider>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<BandwidthDataProvider>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<PacketStatisticsProvider>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ConnectionDataProvider>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<TimingDataProvider>(Lifetime.Singleton).AsImplementedInterfaces();

            // Controls
            builder.Register<LatencySimulatorControl>(Lifetime.Singleton);
        }
    }
}
#endif
