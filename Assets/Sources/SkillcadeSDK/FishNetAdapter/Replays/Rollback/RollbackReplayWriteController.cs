using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using VContainer;

namespace SkillcadeSDK.FishNetAdapter.Replays.Rollback
{
    public class RollbackReplayWriteController : NetworkBehaviour
    {
        [Inject] private readonly RollbackReplayWriteService _rollbackReplayService;

        private readonly List<(FishNet.Component.ColliderRollback.ColliderRollback colliderRollback, Vector3 position)> _queryResults = new();

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

            var tick = (int)TimeManager.Tick;
            _rollbackReplayService.SetCurrentTick(tick);

            // Capture server frame (present state, no rollback)
            _rollbackReplayService.CaptureServerFrame(tick);
        }
    }
}
