using SkillcadeSDK.DI;
using SkillcadeSDK.Events;
using SkillcadeSDK.FishNetAdapter.Match;
using SkillcadeSDK.FishNetAdapter.States;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter
{
    /// <summary>
    /// Installer for Skillcade SDK game components.
    /// Registers GameEventBus, SkillcadeGameStateMachine, states, and services.
    /// </summary>
    public class SkillcadeGameInstaller : MonoInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            // Event Bus
            builder.Register<GameEventBus>(Lifetime.Singleton).AsSelf();

            // State Machine
            builder.Register<SkillcadeGameStateMachine>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();

            // States
            builder.Register<WaitForPlayersState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<CountdownState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<RunningState>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FinishedState>(Lifetime.Singleton).AsImplementedInterfaces();

            // Services
            builder.Register<MatchService>(Lifetime.Singleton);
        }
    }
}
