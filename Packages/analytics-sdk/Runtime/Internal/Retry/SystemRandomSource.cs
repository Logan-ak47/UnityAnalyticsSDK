using System;

namespace Ashutosh.AnalyticsSdk.Internal.Retry
{
    internal sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _rng = new Random();
        public double NextDouble() => _rng.NextDouble();
    }
}