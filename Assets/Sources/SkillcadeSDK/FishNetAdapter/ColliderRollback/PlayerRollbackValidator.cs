using FishNet.Managing.Timing;
using FishNet.Object;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.ColliderRollback
{
    /// <summary>
    /// Uses math-based distance checks to validate triggers on each rollback tick.
    /// Iterates all registered RollbackTriggers (both moving and static).
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

        private void ValidateTriggers(PreciseTick tick, int writeTick)
        {
            if (!_rollbackSource.PlayerOwnerPosition.HasValue)
                Debug.LogError("[PlayerRollbackValidator] no player position for rollback validation!");
            
            Vector2 playerPos = _rollbackSource.PlayerOwnerPosition.GetValueOrDefault(transform.position) + _checkOffset;

            var allTriggers = RollbackTrigger.AllTriggers;
            for (int i = 0; i < allTriggers.Count; i++)
            {
                var trigger = allTriggers[i];
                if (!trigger.CheckOverlap(transform.position, playerPos, _checkRadius))
                    continue;

                trigger.HandleTriggerServer(_networkObject, transform.position, playerPos);
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
