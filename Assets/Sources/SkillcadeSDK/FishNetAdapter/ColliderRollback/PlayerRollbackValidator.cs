using System.Collections.Generic;
using FishNet.Component.ColliderRollback;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Utility.Template;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.ColliderRollback
{
    /// <summary>
    /// Uses FishNet Pro collider rollback to check if player enters any triggers on each network tick.
    /// Searches for RollbackTrigger objects in radius _checkRadius of player with offset _checkOffset.
    /// Passes its own NetworkObject to RollbackTrigger.HandleTriggerServer method.
    /// </summary>
    public class PlayerRollbackValidator : TickNetworkBehaviour
    {
        [SerializeField] private float _checkRadius;
        [SerializeField] private Vector2 _checkOffset;
        [SerializeField] private LayerMask _checkLayer;
        
        private List<Collider2D> _overlapColliders;

        private void Awake()
        {
            SetTickCallbacks(TickCallback.Tick);
        }

        protected override void TimeManager_OnTick()
        {
            if (!IsOwner)
                return;
            
            var tick = TimeManager.GetPreciseTick(TickType.LastPacketTick);
            ValidateTriggersServerRpc(tick);
        }

        [ServerRpc(RequireOwnership = true)]
        private void ValidateTriggersServerRpc(PreciseTick tick)
        {
            _overlapColliders ??= new List<Collider2D>();
            
            RollbackManager.Rollback(tick, RollbackPhysicsType.Physics2D, IsOwner);

            var filter = new ContactFilter2D().NoFilter();
            filter.SetLayerMask(_checkLayer);
            Physics2D.OverlapCircle((Vector2)transform.position + _checkOffset, _checkRadius, filter, _overlapColliders);
            
            foreach (var overlapCollider in _overlapColliders)
            {
                if (!overlapCollider.TryGetComponent(out RollbackTrigger obstacleController))
                    continue;

                obstacleController.HandleTriggerServer(NetworkObject);
            }
            
            _overlapColliders.Clear();
            RollbackManager.Return();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position + (Vector3)_checkOffset, _checkRadius);
        }
#endif
    }
}