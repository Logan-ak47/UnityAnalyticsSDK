using System;

namespace Ashutosh.AnalyticsSdk
{
    public readonly struct AnalyticsStats
    {
        public int QueuedEventCount { get; }
        public DateTimeOffset? LastFlushTimeUtc { get; }
        public string LastError { get; }

        public AnalyticsStats(int queuedEventCount, DateTimeOffset? lastFlushTimeUtc, string lastError)
        {
            QueuedEventCount = queuedEventCount;
            LastFlushTimeUtc = lastFlushTimeUtc;
            LastError = lastError;
        }
    }
}
