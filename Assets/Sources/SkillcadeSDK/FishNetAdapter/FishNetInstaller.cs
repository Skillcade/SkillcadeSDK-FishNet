using FishNet.Managing;
using FishNet.Transporting.UTP;
using SkillcadeSDK.DI;
using SkillcadeSDK.FishNetAdapter.PingService;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.FishNetAdapter.Replays;
using SkillcadeSDK.FishNetAdapter.StateMachine;
using SkillcadeSDK.Replays;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    public class FishNetInstaller : MonoInstaller
    {
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private UnityTransport _unityTransport;
        [SerializeField] private FishNetConnectionController _connectionController;
        [SerializeField] private FishNetPlayersController _playersController;
        [SerializeField] private PlayerSpawner _playerSpawner;
        [SerializeField] private GameStateMachineSyncer _stateMachineSyncer;
        
        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_networkManager);
            builder.RegisterInstance(_unityTransport);
            builder.RegisterInstance(_connectionController).AsSelf().AsImplementedInterfaces();
            builder.RegisterInstance(_playersController).AsSelf();
            builder.RegisterInstance(_playerSpawner);
            builder.RegisterInstance(_stateMachineSyncer).AsSelf().AsImplementedInterfaces();

            builder.Register<FishNetTransportAdapter>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<FishNetPingService>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<FishNetReplayWriteService>(Lifetime.Singleton).As<ReplayWriteService>().AsSelf().AsImplementedInterfaces();
        }
    }
}