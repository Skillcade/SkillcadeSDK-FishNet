using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.ColliderRollback
{
    /// <summary>
    /// Base class for all triggers that should be verified on server using ColliderRollback feature.
    /// Registers itself in a static registry on server start. Provides position via GetPosition() —
    /// uses ColliderRollback.RollbackPosition for moving objects, transform.position for static ones.
    /// </summary>
    public enum TriggerShape { Circle, Box }

    public abstract class RollbackTrigger : NetworkBehaviour
    {
        [SerializeField] private TriggerShape _shape;
        [SerializeField] private float _triggerRadius;
        [SerializeField] private Vector2 _triggerHalfExtents;

        private static readonly List<RollbackTrigger> _allTriggers = new();
        public static List<RollbackTrigger> AllTriggers => _allTriggers;

        private FishNet.Component.ColliderRollback.ColliderRollback _colliderRollback;
        private bool _isStatic;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _isStatic = !TryGetComponent(out _colliderRollback);
            _allTriggers.Add(this);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _allTriggers.Remove(this);
        }

        public Vector2 GetPosition()
        {
            return _isStatic
                ? (Vector2)transform.position
                : (Vector2)_colliderRollback.RollbackPosition;
        }

        public bool CheckOverlap(Vector2 point, float pointRadius)
        {
            Vector2 center = GetPosition();

            if (_shape == TriggerShape.Circle)
            {
                float combined = pointRadius + _triggerRadius;
                float dx = point.x - center.x;
                float dy = point.y - center.y;
                return dx * dx + dy * dy <= combined * combined;
            }
            else // Box
            {
                float closestX = point.x < center.x - _triggerHalfExtents.x
                    ? center.x - _triggerHalfExtents.x
                    : (point.x > center.x + _triggerHalfExtents.x
                        ? center.x + _triggerHalfExtents.x
                        : point.x);
                float closestY = point.y < center.y - _triggerHalfExtents.y
                    ? center.y - _triggerHalfExtents.y
                    : (point.y > center.y + _triggerHalfExtents.y
                        ? center.y + _triggerHalfExtents.y
                        : point.y);

                float dx = point.x - closestX;
                float dy = point.y - closestY;
                return dx * dx + dy * dy <= pointRadius * pointRadius;
            }
        }

        [Server]
        public void HandleTriggerServer(NetworkObject playerNetworkObject)
        {
            HandleTriggerServer_Internal(playerNetworkObject);
        }

        protected abstract void HandleTriggerServer_Internal(NetworkObject playerNetworkObject);

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_shape == TriggerShape.Circle)
                Gizmos.DrawWireSphere(transform.position, _triggerRadius);
            else
                Gizmos.DrawWireCube(transform.position, (Vector3)(_triggerHalfExtents * 2));
        }
#endif
    }
}
