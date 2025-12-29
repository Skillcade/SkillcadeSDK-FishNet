using SkillcadeSDK.Replays;
using SkillcadeSDK.Replays.Components;
using UnityEngine;

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    public class FishNetRigidbody2dReplayComponent : MonoBehaviour, IReplayComponent
    {
        public int Size => sizeof(float) * 4;

        [SerializeField] private Rigidbody2D _rigidbody;

        public void Read(ReplayReader reader)
        {
            var position = new Vector2(reader.ReadFloat(), reader.ReadFloat());
            var velocity = new Vector2(reader.ReadFloat(), reader.ReadFloat());
            _rigidbody.transform.position = position;
            _rigidbody.linearVelocity = velocity;
        }

        public void Write(ReplayWriter writer)
        {
            var position = _rigidbody.position;
            var velocity = _rigidbody.linearVelocity;
            writer.WriteFloat(position.x);
            writer.WriteFloat(position.y);
            writer.WriteFloat(velocity.x);
            writer.WriteFloat(velocity.y);
        }
    }
}