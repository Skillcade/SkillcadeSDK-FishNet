using SkillcadeSDK.Replays;
using SkillcadeSDK.Replays.Components;
using UnityEngine; 

namespace SkillcadeSDK.FishNetAdapter.Replays
{
    [ReplayDataObjectId(1)]
    public class FishNetRigidbody2dReplayComponent : MonoBehaviour, IReplayComponent
    {
        public int Size => sizeof(float) * 4;

        [SerializeField] private Rigidbody2D _rigidbody;

        public void Read(ReplayReader reader)
        {
            var position = reader.ReadVector2();
            // var velocity = reader.ReadVector2();
            _rigidbody.transform.position = position;
            // _rigidbody.linearVelocity = velocity;
        }

        public void Write(ReplayWriter writer)
        {
            var position = _rigidbody.position;
            // var velocity = _rigidbody.linearVelocity;
            writer.WriteVector2(position);
            // writer.WriteVector2(velocity);
        }
    }
}