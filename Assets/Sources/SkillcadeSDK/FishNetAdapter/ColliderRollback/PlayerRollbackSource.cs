using System;
using System.Collections.Generic;
using FishNet.Component.ColliderRollback;
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
            var overridePositions = new Dictionary<int, Vector2>();
            // foreach (var component in FishNetRigidbody2dReplayComponent.ReplayComponents)
            // {
            //     if (component == null)
            //         continue;
            //     
            //     if (component.UseOverridePosition)
            //         overridePositions.Add(component.OwnerId, component.transform.position);
            // }
            overridePositions.Add(OwnerId, transform.position);

            // Debug.Log($"[PlayerRollbackSource] Collected {overridePositions.Count} override positions");
            PerformRollbackServerRpc(tick, overridePositions);
        }

        [ServerRpc(RequireOwnership = true)]
        private void PerformRollbackServerRpc(PreciseTick tick, Dictionary<int, Vector2> overrideObjectsPositions)
        {
            // Debug.Log($"[PlayerRollbackSource] Perform rollback for {OwnerId} at tick {tick.Tick}, current: {TimeManager.Tick}");
            foreach (var component in FishNetRigidbody2dReplayComponent.ReplayComponents)
            {
                if (overrideObjectsPositions.TryGetValue(component.OwnerId, out Vector2 position))
                {
                    component.OverridePosition = position;
                    // Debug.Log($"[PlayerRollbackSource] Use override position {position} for object {component.OwnerId}, transform: {component.transform.position}");
                }
                else
                {
                    component.OverridePosition = null;
                }
            }
            
            if (overrideObjectsPositions.TryGetValue(OwnerId, out var currentPosition))
                PlayerOwnerPosition = currentPosition;
            
            _queryResults ??= new();
            _queryResults.Clear();

            RollbackManager.QueryRollbackPositions(tick, IsOwner, _queryResults);

            OnRollback?.Invoke(tick);
            RollbackManager.RevertRollbackPositions();
            PlayerOwnerPosition = null;
            
            foreach (var component in FishNetRigidbody2dReplayComponent.ReplayComponents)
            {
                component.OverridePosition = null;
            }
        }
    }
}
