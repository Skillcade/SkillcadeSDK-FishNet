using FishNet.Component.ColliderRollback;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Utility.Template;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays.Rollback
{
    public class RollbackReplayTickSender : TickNetworkBehaviour
    {
        [Inject] private readonly RollbackReplayWriteService _rollbackReplayService;

        private void Awake()
        {
            SetTickCallbacks(TickCallback.Tick);
        }

        protected override void TimeManager_OnTick()
        {
            if (!IsOwner)
                return;

            var tick = TimeManager.GetPreciseTick(TickType.LastPacketTick);
            SendTickServerRpc(tick);
        }

        [ServerRpc(RequireOwnership = true)]
        private void SendTickServerRpc(PreciseTick tick)
        {
            RollbackManager.Rollback(tick, RollbackPhysicsType.Physics2D, IsOwner);
            _rollbackReplayService.CaptureRollbackFrame(Owner.ClientId, (int)tick.Tick);
            RollbackManager.Return();
        }
    }
}
