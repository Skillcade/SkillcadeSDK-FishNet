using SkillcadeSDK.DI;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetReplayInstaller : MonoInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.Register<FishNetReplayPlayerDataService>(Lifetime.Singleton);
        }
    }
}