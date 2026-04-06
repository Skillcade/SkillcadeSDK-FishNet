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
        [Tooltip("How many send intervals the buffer lags behind remote time. Higher = more latency but smoother.")]
        [SerializeField] public float BufferTimeMultiplier;

        [Tooltip("Maximum number of snapshots stored in each buffer. Older snapshots are discarded when exceeded.")]
        [SerializeField] public int BufferLimit;

        [Tooltip("Drift threshold (in send intervals) below which the timeline slows down to avoid running ahead.")]
        [SerializeField] public float CatchupNegativeThreshold;

        [Tooltip("Drift threshold (in send intervals) above which the timeline speeds up to catch up.")]
        [SerializeField] public float CatchupPositiveThreshold;

        [Tooltip("How much faster the timeline runs when catching up (added to timescale 1.0).")]
        [SerializeField] public float CatchupSpeed;

        [Tooltip("How much slower the timeline runs when slowing down (subtracted from timescale 1.0).")]
        [SerializeField] public float SlowDownSpeed;

        [Tooltip("Window size (in send intervals) for the drift EMA. Larger = smoother drift estimation.")]
        [SerializeField] public int DriftEmaDuration;

        [Tooltip("Whether to dynamically adjust buffer time based on delivery jitter.")]
        [SerializeField] public bool DynamicAdjustment;

        [Tooltip("Tolerance added to the dynamic buffer multiplier. Higher = more safety margin against jitter.")]
        [SerializeField] public float DynamicAdjustmentTolerance;

        [Tooltip("Window size (in send intervals) for the delivery time EMA. Used by dynamic adjustment.")]
        [SerializeField] public int DeliveryTimeEmaDuration;

        [Tooltip("Buffer size, above which player will be instantly teleported to current position, to avoid creating too big buffer")]
        [SerializeField] public int TeleportBufferThreshold;
    }
}
