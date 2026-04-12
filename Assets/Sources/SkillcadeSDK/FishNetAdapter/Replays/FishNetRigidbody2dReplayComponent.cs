using System.Collections.Generic;
using SkillcadeSDK.FishNetAdapter.ColliderRollback;
using SkillcadeSDK.Replays;
using SkillcadeSDK.Replays.Components;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    [ReplayDataObjectId(1)]
    public class FishNetRigidbody2dReplayComponent : MonoBehaviour, IReplayComponent
    {
        public static List<FishNetRigidbody2dReplayComponent> ReplayComponents = new List<FishNetRigidbody2dReplayComponent>();
        
        public int OwnerId => _playerRollbackSource != null ? _playerRollbackSource.OwnerId : -1;
        public int Size => sizeof(float) * 2;
        public bool UseOverridePosition => _useOverridePosition;

        public Vector2? OverridePosition;

        [SerializeField] private bool _debug;
        [SerializeField] private bool _useOverridePosition;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private PlayerRollbackSource _playerRollbackSource;
        [SerializeField] private FishNet.Component.ColliderRollback.ColliderRollback _colliderRollback;

        private void OnEnable()
        {
            ReplayComponents.Add(this);
        }

        private void OnDisable()
        {
            ReplayComponents.Remove(this);
        }

        public void Read(ReplayReader reader)
        {
            var position = reader.ReadVector2();
            _rigidbody.transform.position = position;
        }

        public void Write(ReplayWriter writer)
        {
            var position = OverridePosition ?? (
                _colliderRollback != null
                    ? _colliderRollback.RollbackPosition
                    : _rigidbody.position);

            if (_debug)
            {
                if (OverridePosition.HasValue)
                {
                    if (_colliderRollback != null)
                    {
                        Debug.Log(
                            $"[FishNetRigidbody2dReplayComponent] Object {OwnerId} overrided position at rollback, write: {position}, rollback: {_colliderRollback.RollbackPosition}, actual: {_rigidbody.position}");
                    }
                    else
                    {
                        Debug.Log(
                            $"[FishNetRigidbody2dReplayComponent] Object {OwnerId} overrided position without rollback, write: {position}, actual: {_rigidbody.position}");
                    }
                }
                else
                {
                    if (_colliderRollback != null)
                    {
                        Debug.Log($"[FishNetRigidbody2dReplayComponent] Object {OwnerId} position at rollback, write: {position}, actual: {_rigidbody.position}");
                    }
                    else
                    {
                        Debug.Log(
                            $"[FishNetRigidbody2dReplayComponent] Object {OwnerId} position without rollback, write: {position}");
                    }
                }
            }
            
            writer.WriteVector2(position);
        }
    }
}
