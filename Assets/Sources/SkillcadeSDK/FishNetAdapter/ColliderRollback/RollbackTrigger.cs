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
    public abstract class RollbackTrigger : NetworkBehaviour
    {
        public float TriggerRadius => _triggerRadius;

        [SerializeField] private float _triggerRadius;

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

        [Server]
        public void HandleTriggerServer(NetworkObject playerNetworkObject)
        {
            HandleTriggerServer_Internal(playerNetworkObject);
        }

        protected abstract void HandleTriggerServer_Internal(NetworkObject playerNetworkObject);
    }
}
