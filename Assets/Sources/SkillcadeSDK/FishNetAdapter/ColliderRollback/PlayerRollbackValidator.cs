using FishNet.Component.ColliderRollback;
using FishNet.Managing.Timing;
using FishNet.Object;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.ColliderRollback
{
    /// <summary>
    /// Uses math-based distance checks against rolled-back positions to validate triggers.
    /// Searches for RollbackTrigger objects within radius _checkRadius of player with offset _checkOffset.
    /// Passes its own NetworkObject to RollbackTrigger.HandleTriggerServer method.
    /// </summary>
    public class PlayerRollbackValidator : MonoBehaviour
    {
        [SerializeField] private float _checkRadius;
        [SerializeField] private Vector2 _checkOffset;
        [SerializeField] private NetworkObject _networkObject;
        [SerializeField] private PlayerRollbackSource _rollbackSource;

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
            var rollbackPositions = _rollbackSource.LastQueryResults;
            if (rollbackPositions == null || rollbackPositions.Count == 0)
                return;

            Vector2 playerPos = (Vector2)transform.position + _checkOffset;
            float sqrRadius = _checkRadius * _checkRadius;

            foreach (var (cr, pos) in rollbackPositions)
            {
                if ((playerPos - (Vector2)pos).sqrMagnitude > sqrRadius)
                    continue;

                if (cr.TryGetComponent(out RollbackTrigger trigger))
                    trigger.HandleTriggerServer(_networkObject);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position + (Vector3)_checkOffset, _checkRadius);
        }
#endif
    }
}
