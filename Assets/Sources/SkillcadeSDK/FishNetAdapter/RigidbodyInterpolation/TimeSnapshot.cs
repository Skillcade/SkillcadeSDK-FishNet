namespace Game.RigidbodyInterpolation
{
    /// <summary>
    /// Lightweight snapshot carrying only timing data.
    /// Used in a dedicated time buffer to drive timeline management
    /// (drift estimation, timescale adjustment) independently from position data.
    /// </summary>
    public struct TimeSnapshot : IInterpolateSnapshot
    {
        public float RemoteTime { get; set; }
        public float LocalTime { get; set; }

        /// <summary>
        /// Creates a time snapshot with the given source and local timestamps.
        /// </summary>
        public TimeSnapshot(float remoteTime, float localTime)
        {
            RemoteTime = remoteTime;
            LocalTime = localTime;
        }
    }
}
