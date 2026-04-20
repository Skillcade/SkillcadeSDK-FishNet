using FishNet.Managing.Timing;
using FishNet.Object;
using SkillcadeSDK.FishNetAdapter.ColliderRollback;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays.Rollback
{
    public class PlayerRollbackReplaySource : NetworkBehaviour
    {
        [SerializeField] private PlayerRollbackSource _rollbackSource;

        [Inject] private readonly IObjectResolver _objectResolver;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (IsServerInitialized)
                _rollbackSource.OnRollback += OnRollback;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (IsServerInitialized)
                _rollbackSource.OnRollback -= OnRollback;
        }

        private void OnRollback(PreciseTick tick, PreciseTick writeTick)
        {
            if (_objectResolver == null)
                this.InjectToMe();
            
            if (!_objectResolver.TryResolve(out RollbackReplayWriteService writeService))
                return;

            writeService.CaptureClientFrame(OwnerId, (int)writeTick.Tick);
        }
    }
}