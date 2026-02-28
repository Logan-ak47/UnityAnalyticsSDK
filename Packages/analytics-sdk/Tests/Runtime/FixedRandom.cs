using Ashutosh.AnalyticsSdk.Internal.Retry;

internal class FixedRandom : IRandomSource
    {
        private readonly double _value;
        public FixedRandom(double value) { _value = value; }
        public double NextDouble() => _value;
    }