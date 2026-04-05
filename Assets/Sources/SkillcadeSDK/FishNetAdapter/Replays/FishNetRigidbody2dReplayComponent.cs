using SkillcadeSDK.FishNetAdapter.ColliderRollback;
using SkillcadeSDK.Replays;
using SkillcadeSDK.Replays.Components;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    [ReplayDataObjectId(1)]
    public class FishNetRigidbody2dReplayComponent : MonoBehaviour, IReplayComponent
    {
        public int Size => sizeof(float) * 2;

        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private PlayerRollbackSource _playerRollbackSource;
        [SerializeField] private FishNet.Component.ColliderRollback.ColliderRollback _colliderRollback;

        public void Read(ReplayReader reader)
        {
            var position = reader.ReadVector2();
            _rigidbody.transform.position = position;
        }

        public void Write(ReplayWriter writer)
        {
            var position =
                _playerRollbackSource != null && _playerRollbackSource.PlayerOwnerPosition.HasValue
                ? _playerRollbackSource.PlayerOwnerPosition.Value
                : _colliderRollback != null
                    ? _colliderRollback.RollbackPosition
                    : _rigidbody.position;
            
            writer.WriteVector2(position);
        }
    }
}
