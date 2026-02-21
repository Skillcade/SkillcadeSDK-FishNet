using UnityEngine;

namespace Game.RigidbodyInterpolation
{
    /// <summary>
    /// ScriptableObject holding tuning parameters for snapshot interpolation.
    /// Controls buffering, timeline catchup/slowdown, and dynamic adjustment behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "SnapshotInterpolationSettings", menuName = "Configs/Snapshot Interpolation Settings")]
    public class SnapshotInterpolationSettings : ScriptableObject
    {
        /// <summary>How many send intervals the buffer lags behind remote time. Higher = more latency but smoother.</summary>
        [SerializeField] public float BufferTimeMultiplier;

        /// <summary>Maximum number of snapshots stored in each buffer. Older snapshots are discarded when exceeded.</summary>
        [SerializeField] public int BufferLimit;

        /// <summary>Drift threshold (in send intervals) below which the timeline slows down to avoid running ahead.</summary>
        [SerializeField] public float CatchupNegativeThreshold;

        /// <summary>Drift threshold (in send intervals) above which the timeline speeds up to catch up.</summary>
        [SerializeField] public float CatchupPositiveThreshold;

        /// <summary>How much faster the timeline runs when catching up (added to timescale 1.0).</summary>
        [SerializeField] public float CatchupSpeed;

        /// <summary>How much slower the timeline runs when slowing down (subtracted from timescale 1.0).</summary>
        [SerializeField] public float SlowDownSpeed;

        /// <summary>Window size (in send intervals) for the drift EMA. Larger = smoother drift estimation.</summary>
        [SerializeField] public int DriftEmaDuration;

        /// <summary>Whether to dynamically adjust buffer time based on delivery jitter.</summary>
        [SerializeField] public bool DynamicAdjustment;

        /// <summary>Tolerance added to the dynamic buffer multiplier. Higher = more safety margin against jitter.</summary>
        [SerializeField] public float DynamicAdjustmentTolerance;

        /// <summary>Window size (in send intervals) for the delivery time EMA. Used by dynamic adjustment.</summary>
        [SerializeField] public int DeliveryTimeEmaDuration;

        /// <summary>Additional offset applied to the remote buffer. Reserved for future use.</summary>
        [SerializeField] public int RemoteBufferOffset;
    }
}
