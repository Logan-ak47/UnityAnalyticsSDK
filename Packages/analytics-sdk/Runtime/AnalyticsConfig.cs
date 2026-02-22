// AnalyticsConfig.cs
namespace Ashutosh.AnalyticsSdk
{
    public sealed class AnalyticsConfig
    {
        public string EndpointUrl { get; }
        public int MaxEventsPerBatch { get; }
        public int MaxBatchesPerFlush { get; }
        public float FlushIntervalSeconds { get; }
        public bool EnableAutoFlush { get; }
        public bool EnableLogging { get; }

        public AnalyticsConfig(
            string endpointUrl,
            int maxEventsPerBatch = 25,
            int maxBatchesPerFlush = 4,
            float flushIntervalSeconds = 5f,
            bool enableAutoFlush = true,
            bool enableLogging = true)
        {
            EndpointUrl = endpointUrl;
            MaxEventsPerBatch = maxEventsPerBatch;
            MaxBatchesPerFlush = maxBatchesPerFlush;
            FlushIntervalSeconds = flushIntervalSeconds;
            EnableAutoFlush = enableAutoFlush;
            EnableLogging = enableLogging;
        }
    }
}