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
        [SerializeField] private bool _debug;
        [SerializeField] private TriggerShape _shape;
        [SerializeField] private float _triggerRadius;
        [SerializeField] private Vector2 _triggerHalfExtents;

        private static readonly List<RollbackTrigger> _allTriggers = new();
        public static List<RollbackTrigger> AllTriggers => _allTriggers;

        private FishNet.Component.ColliderRollback.ColliderRollback _colliderRollback;

        private bool _drawHitPosition;
        private Vector2 _playerTransformPosition;
        private Vector2 _playerCheckPosition;
        private Vector2 _selfTransformPosition;
        private Vector2 _selfCheckPosition;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _colliderRollback = GetComponentInParent<FishNet.Component.ColliderRollback.ColliderRollback>();
            _allTriggers.Add(this);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            _allTriggers.Remove(this);
        }

        public Vector2 GetPosition()
        {
            return _colliderRollback == null
                ? (Vector2)transform.position
                : (Vector2)_colliderRollback.RollbackPosition;
        }

        public bool CheckOverlap(Vector2 playerTransformPosition, Vector2 point, float pointRadius)
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
        public void HandleTriggerServer(NetworkObject playerNetworkObject, Vector2 playerTransformPosition, Vector2 point)
        {
            HandleTriggerServer_Internal(playerNetworkObject);
            
            if (!_debug) return;
            
            Vector2 center = GetPosition();
            ShowHitPositionClientRpc(playerTransformPosition, point, transform.position, center);
        }

        [ObserversRpc]
        private void ShowHitPositionClientRpc(Vector2 playerTransformPosition, Vector2 playerCheckPosition, Vector2 selfTransformPosition, Vector2 selfCheckPosition)
        {
            _playerTransformPosition = playerTransformPosition;
            _playerCheckPosition = playerCheckPosition;
            _selfTransformPosition = selfTransformPosition;
            _selfCheckPosition = selfCheckPosition;
            _drawHitPosition = true;
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
        
        private void OnDrawGizmos()
        {
            if (!_drawHitPosition)
                return;
            
            // Client-recorded positions: blue
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_playerTransformPosition, 0.15f);

            // Server rollback positions: red
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_playerCheckPosition, 0.15f);

            // Draw connecting lines between paired positions (same index)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_playerTransformPosition, _playerCheckPosition);
            
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_selfTransformPosition, 0.15f);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_selfCheckPosition, 0.15f);
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(_selfTransformPosition, _selfCheckPosition);
        }
#endif
    }
}
