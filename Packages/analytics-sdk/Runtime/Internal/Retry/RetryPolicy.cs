using System;

namespace Ashutosh.AnalyticsSdk.Internal.Retry
{
    internal sealed class RetryPolicy
    {
        private readonly float _baseDelay;
        private readonly float _maxDelay;
        private readonly int _maxAttempts;
        private readonly IRandomSource _random;

        public RetryPolicy(float baseDelay, float maxDelay, int maxAttempts, IRandomSource random)
        {
            _baseDelay = Math.Max(0.01f, baseDelay);
            _maxDelay = Math.Max(_baseDelay, maxDelay);
            _maxAttempts = Math.Max(0, maxAttempts);
            _random = random;
        }

        public int MaxAttempts => _maxAttempts;

        // attempt: 1..N
        public float GetDelaySeconds(int attempt)
        {
            if (attempt <= 0) return 0f;

            // exponential cap
            double exp = _baseDelay * Math.Pow(2, attempt - 1);
            double cap = Math.Min(exp, _maxDelay);

            // Full jitter: random between 0..cap
            return (float)(_random.NextDouble() * cap);
        }
    }
}