using FishNet.Object;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays.Rollback
{
    public class RollbackReplayWriteController : NetworkBehaviour
    {
        [Inject] private readonly RollbackReplayWriteService _rollbackReplayService;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            TimeManager.OnPostTick += OnPostTick;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            TimeManager.OnPostTick -= OnPostTick;
        }

        private void OnPostTick()
        {
            if (!IsServerInitialized)
                return;

            _rollbackReplayService.CaptureServerFrame((int)TimeManager.Tick);
        }
    }
}
