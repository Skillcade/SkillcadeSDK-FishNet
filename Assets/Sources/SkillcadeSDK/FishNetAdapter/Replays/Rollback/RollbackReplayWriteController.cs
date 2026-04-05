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

            // // For each connected client, query rollback positions and capture
            // var rollbackManager = NetworkManager.RollbackManager;
            // foreach (var conn in NetworkManager.ServerManager.Clients.Values)
            // {
            //     uint estimatedClientTick = conn.PacketTick.Value(TimeManager);
            //     var preciseTick = TimeManager.GetPreciseTick(estimatedClientTick);
            //
            //     // QueryRollbackPositions updates ColliderRollback.RollbackPosition
            //     // without moving transforms — the replay component reads RollbackPosition
            //     _queryResults.Clear();
            //     Debug.Log($"[RollbackReplayWriteController] Query rollback positions for client {conn.ClientId} tick {preciseTick.Tick}");
            //     rollbackManager.QueryRollbackPositions(preciseTick, false, _queryResults);
            //
            //     _rollbackReplayService.CaptureClientFrame(conn.ClientId, (int)preciseTick.Tick);
            //     rollbackManager.RevertRollbackPositions();
            // }
        }
    }
}
