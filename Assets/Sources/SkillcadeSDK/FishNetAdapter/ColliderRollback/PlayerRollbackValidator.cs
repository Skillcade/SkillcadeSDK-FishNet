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
    public class PlayerRollbackValidator : MonoBehaviour
    {
        [SerializeField] private float _checkRadius;
        [SerializeField] private Vector2 _checkOffset;
        [SerializeField] private LayerMask _checkLayer;
        [SerializeField] private NetworkObject _networkObject;
        [SerializeField] private PlayerRollbackSource _rollbackSource;
        
        private List<Collider2D> _overlapColliders;

        private void OnEnable()
        {
            _rollbackSource.OnRollback += ValidateTriggers;
        }

        private void OnDisable()
        {
            _rollbackSource.OnRollback -= ValidateTriggers;
        }

        private void ValidateTriggers(PreciseTick tick)
        {
            _overlapColliders ??= new List<Collider2D>();

            var filter = new ContactFilter2D().NoFilter();
            filter.SetLayerMask(_checkLayer);
            Physics2D.OverlapCircle((Vector2)transform.position + _checkOffset, _checkRadius, filter, _overlapColliders);
            
            foreach (var overlapCollider in _overlapColliders)
            {
                if (!overlapCollider.TryGetComponent(out RollbackTrigger obstacleController))
                    continue;

                obstacleController.HandleTriggerServer(_networkObject);
            }
            
            _overlapColliders.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position + (Vector3)_checkOffset, _checkRadius);
        }
#endif
    }
}