using FishNet.Managing;
using FishNet.Transporting.UTP;
using SkillcadeSDK.DI;
using SkillcadeSDK.FishNetAdapter.PingService;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SkillcadeSDK.FishNetAdapter
{
    public class FishNetInstaller : MonoInstaller
    {
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private UnityTransport _unityTransport;
        [SerializeField] private FishNetConnectionController _connectionController;
        [SerializeField] private FishNetPlayersController _playersController;
        [SerializeField] private PlayerSpawner _playerSpawner;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_networkManager);
            builder.RegisterInstance(_unityTransport);
            builder.RegisterInstance(_connectionController).AsSelf().AsImplementedInterfaces();
            builder.RegisterInstance(_playersController).AsSelf().AsImplementedInterfaces();
            builder.RegisterInstance(_playerSpawner);

            builder.RegisterEntryPoint<FishNetTransportAdapter>().AsSelf();
            builder.RegisterEntryPoint<FishNetPingService>();
        }
    }
}