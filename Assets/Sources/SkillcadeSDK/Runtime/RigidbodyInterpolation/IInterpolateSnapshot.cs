namespace Game.RigidbodyInterpolation
{
    /// <summary>
    /// Contract for snapshot types used with <see cref="InterpolationUtils"/>.
    /// Each snapshot carries two timestamps that allow the interpolation system
    /// to manage its local timeline relative to the source clock.
    /// </summary>
    public interface IInterpolateSnapshot
    {
        /// <summary>
        /// Timestamp from the source clock (e.g. server tick time converted to seconds).
        /// Used as the sorting key in snapshot buffers and for timeline calculations.
        /// </summary>
        public float RemoteTime { get; set; }

        /// <summary>
        /// Timestamp from the receiver's local clock at the moment the snapshot was recorded.
        /// Used to measure delivery intervals for dynamic buffer adjustment.
        /// </summary>
        public float LocalTime { get; set; }
    }
}
