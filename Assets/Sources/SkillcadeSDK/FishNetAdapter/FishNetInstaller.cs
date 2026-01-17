using FishNet.Managing;
using SkillcadeSDK.DI;
using SkillcadeSDK.FishNetAdapter.PingService;
using SkillcadeSDK.FishNetAdapter.Players;
using SkillcadeSDK.FishNetAdapter.Replays;
using SkillcadeSDK.FishNetAdapter.StateMachine;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    public class FishNetInstaller : MonoInstaller
    {
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private FishNetConnectionController _connectionController;
        [SerializeField] private FishNetPlayersController _playersController;
        [SerializeField] private GameStateMachineSyncer _stateMachineSyncer;
        [SerializeField] private FishNetReplayWriteController _replayWriteController;

        public override void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_networkManager);
            builder.RegisterInstance(_connectionController).AsSelf().AsImplementedInterfaces();
            builder.RegisterInstance(_playersController).AsSelf();
            builder.RegisterInstance(_stateMachineSyncer).AsSelf().AsImplementedInterfaces();
            builder.RegisterInstance(_replayWriteController);

            builder.Register<FishNetPingService>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
        }
    }
}