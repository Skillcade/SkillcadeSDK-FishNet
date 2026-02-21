using UnityEngine;

namespace Game.RigidbodyInterpolation
{
    /// <summary>
    /// Exponential Moving Average (EMA) that tracks a smoothed value, variance,
    /// and standard deviation. Newer samples carry more weight than older ones,
    /// controlled by the smoothing factor alpha = 2 / (n + 1).
    /// </summary>
    public struct ExponentalMovingAverage
    {
	    public float Value;
        public float Variance;
        public float StandardDeviation;

        private readonly float _alpha;
        private bool _initialized;

        /// <summary>
        /// Creates a new EMA with window size <paramref name="n"/>.
        /// The smoothing factor is computed as alpha = 2 / (n + 1).
        /// Larger n means slower reaction to new values.
        /// </summary>
        public ExponentalMovingAverage(int n)
        {
            _alpha = 2f / (n + 1);
            _initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }

        /// <summary>
        /// Incorporates a new sample, updating the smoothed average,
        /// variance, and standard deviation.
        /// The first sample initializes the average directly.
        /// </summary>
        public void Add(float value)
        {
            if (!_initialized)
            {
                Value = value;
                _initialized = true;
            }

            float delta = value - Value;
            Value += _alpha * delta;
            Variance = (1 - _alpha) * (Variance + _alpha * delta * delta);
            StandardDeviation = Mathf.Sqrt(Variance);
        }

        /// <summary>
        /// Resets to an uninitialized state. The next <see cref="Add"/> call
        /// will treat its value as the first sample.
        /// </summary>
        public void Clear()
        {
            _initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }
    }
}
