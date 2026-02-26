// AnalyticsConfig.cs
namespace Ashutosh.AnalyticsSdk
{
    /// <summary>
    /// Immutable configuration for an <see cref="AnalyticsClient"/>.
    /// </summary>
    public sealed class AnalyticsConfig
    {
        /// <summary>
        /// HTTP endpoint used to send analytics batches.
        /// </summary>
        public string EndpointUrl { get; }

        /// <summary>
        /// Maximum number of events included in a single request.
        /// </summary>
        public int MaxEventsPerBatch { get; }

        /// <summary>
        /// Maximum number of batches sent during one flush call.
        /// </summary>
        public int MaxBatchesPerFlush { get; }

        /// <summary>
        /// Auto-flush interval in seconds.
        /// </summary>
        public float FlushIntervalSeconds { get; }

        /// <summary>
        /// Enables periodic background flushing via the runtime helper.
        /// </summary>
        public bool EnableAutoFlush { get; }

        /// <summary>
        /// Enables diagnostic logging for dropped events and failures.
        /// </summary>
        public bool EnableLogging { get; }


        public bool EnableDiskPersistence { get; }
        public int MaxDiskBytes { get; }
        public string StoragePathOverride { get; }




        public bool EnableRetry { get; }
        public int MaxRetryAttempts { get; }
        public float RetryBaseDelaySeconds { get; }
        public float RetryMaxDelaySeconds { get; }

        /// <summary>
        /// Creates analytics client configuration values.
        /// </summary>
        /// <param name="endpointUrl">Analytics ingestion endpoint URL.</param>
        /// <param name="maxEventsPerBatch">Maximum events per request batch.</param>
        /// <param name="maxBatchesPerFlush">Maximum batches processed per flush.</param>
        /// <param name="flushIntervalSeconds">Auto-flush interval in seconds.</param>
        /// <param name="enableAutoFlush">Whether auto-flush is enabled.</param>
        /// <param name="enableLogging">Whether SDK logging is enabled.</param>
        public AnalyticsConfig(
            string endpointUrl,
            int maxEventsPerBatch = 25,
            int maxBatchesPerFlush = 4,
            float flushIntervalSeconds = 5f,
            bool enableAutoFlush = true,
            bool enableLogging = true,
            bool enableDiskPersistence = true,
            int maxDiskBytes = 5_000_000,
            string storagePathOverride = null,
            bool enableRetry = true,
            int maxRetryAttempts = 5,
            float retryBaseDelaySeconds = 1f,
            float retryMaxDelaySeconds = 30f)
        {
            EndpointUrl = endpointUrl;
            MaxEventsPerBatch = maxEventsPerBatch;
            MaxBatchesPerFlush = maxBatchesPerFlush;
            FlushIntervalSeconds = flushIntervalSeconds;
            EnableAutoFlush = enableAutoFlush;
            EnableLogging = enableLogging;
            EnableDiskPersistence = enableDiskPersistence;
            MaxDiskBytes = maxDiskBytes;
            StoragePathOverride = storagePathOverride;
            EnableRetry = enableRetry;
            MaxRetryAttempts = maxRetryAttempts;
            RetryBaseDelaySeconds = retryBaseDelaySeconds;
            RetryMaxDelaySeconds = retryMaxDelaySeconds;
        }



    }
}
