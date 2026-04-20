using System;
using System.Collections.Generic;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Utility.Template;
using SkillcadeSDK.FishNetAdapter.Replays;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.ColliderRollback
{
    public class PlayerRollbackSource : TickNetworkBehaviour
    {
        public Vector2? PlayerOwnerPosition;
        public event Action<PreciseTick> OnRollback;

        [SerializeField] private TickType _tickType;
        [SerializeField] private int _tickDivisor = 1;

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

            var tick = TimeManager.GetPreciseTick(TickType.Tick);
            var overridePositions = new Dictionary<int, Vector2>();
            overridePositions.Add(OwnerId, transform.position);

            PerformRollbackServerRpc(tick, overridePositions);
        }

        [ServerRpc(RequireOwnership = true)]
        private void PerformRollbackServerRpc(PreciseTick tick, Dictionary<int, Vector2> overrideObjectsPositions)
        {
            // 1) Apply client-supplied overrides by OwnerId. Track which components got one.
            var overriddenFromDict = new HashSet<FishNetRigidbody2dReplayComponent>();
            foreach (var component in FishNetRigidbody2dReplayComponent.ReplayComponents)
            {
                if (component == null) continue;

                if (overrideObjectsPositions.TryGetValue(component.OwnerId, out var pos))
                {
                    component.OverridePosition = pos;
                    overriddenFromDict.Add(component);
                }
                else
                {
                    component.OverridePosition = null;
                }
            }

            if (overrideObjectsPositions.TryGetValue(OwnerId, out var ownerPos))
                PlayerOwnerPosition = ownerPos;

            // 2) For each TickBasedMoveBehaviour: if it has a replay component on same
            //    GameObject and that component wasn't overridden from the dict, set its
            //    OverridePosition to the tick-based reconstruction.
            foreach (var tmb in TickBasedMoveBehaviour.All)
            {
                if (tmb == null) continue;
                if (!tmb.TryGetComponent<FishNetRigidbody2dReplayComponent>(out var replay)) continue;
                if (overriddenFromDict.Contains(replay)) continue;

                replay.OverridePosition = tmb.GetPositionAtTick(tick);
            }

            // 3) Fire rollback validation. RollbackTrigger/Validator read override positions.
            //    Replay write runs inside this invocation — OverridePosition values must be live.
            OnRollback?.Invoke(tick);

            // 4) Clear state.
            PlayerOwnerPosition = null;
            foreach (var component in FishNetRigidbody2dReplayComponent.ReplayComponents)
            {
                if (component == null) continue;
                component.OverridePosition = null;
            }
        }
    }
}
