using System;
using System.Collections.Generic;
using FishNet.Component.ColliderRollback;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Utility.Template;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.ColliderRollback
{
    public class PlayerRollbackSource : TickNetworkBehaviour
    {
        public event Action<PreciseTick> OnRollback;

        public new RollbackManager RollbackManager => base.RollbackManager;

        public IReadOnlyList<(FishNet.Component.ColliderRollback.ColliderRollback colliderRollback, Vector3 position)> LastQueryResults => _queryResults;

        [SerializeField] private int _tickDivisor = 1;

        private List<(FishNet.Component.ColliderRollback.ColliderRollback, Vector3)> _queryResults;
        private int _tickCounter;

        private void Awake()
        {
            SetTickCallbacks(TickCallback.Tick);
        }

        protected override void TimeManager_OnTick()
        {
            if (!IsOwner)
                return;

            if (++_tickCounter % _tickDivisor != 0)
                return;

            var tick = TimeManager.GetPreciseTick(TickType.LastPacketTick);
            PerformRollbackServerRpc(tick);
        }

        [ServerRpc(RequireOwnership = true)]
        private void PerformRollbackServerRpc(PreciseTick tick)
        {
            _queryResults ??= new();
            _queryResults.Clear();

            RollbackManager.QueryRollbackPositions(tick, IsOwner, _queryResults);

            OnRollback?.Invoke(tick);
        }
    }
}
