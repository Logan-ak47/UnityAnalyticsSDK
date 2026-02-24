using System;

namespace Ashutosh.AnalyticsSdk
{
    /// <summary>
    /// Snapshot of client queue and flush state.
    /// </summary>
    public readonly struct AnalyticsStats
    {
        /// <summary>
        /// Number of events currently queued.
        /// </summary>
        public int QueuedEventCount { get; }

        /// <summary>
        /// UTC time of the most recent flush attempt, if any.
        /// </summary>
        public DateTimeOffset? LastFlushTimeUtc { get; }

        /// <summary>
        /// Last error message recorded by the client, if any.
        /// </summary>
        public string LastError { get; }

        /// <summary>
        /// Creates a stats snapshot.
        /// </summary>
        /// <param name="queuedEventCount">Queued event count.</param>
        /// <param name="lastFlushTimeUtc">Last flush attempt time in UTC.</param>
        /// <param name="lastError">Last recorded error message.</param>
        public AnalyticsStats(int queuedEventCount, DateTimeOffset? lastFlushTimeUtc, string lastError)
        {
            QueuedEventCount = queuedEventCount;
            LastFlushTimeUtc = lastFlushTimeUtc;
            LastError = lastError;
        }
    }
}
