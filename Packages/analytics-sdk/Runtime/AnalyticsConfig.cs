// AnalyticsConfig.cs
namespace Ashutosh.AnalyticsSdk
{
    public sealed class AnalyticsConfig
    {
        public string EndpointUrl { get; }
        public int MaxEventsPerBatch { get; }
        public float FlushIntervalSeconds { get; }
        public bool EnableLogging { get; }

        public AnalyticsConfig(
            string endpointUrl,
            int maxEventsPerBatch = 25,
            float flushIntervalSeconds = 5f,
            bool enableLogging = true)
        {
            EndpointUrl = endpointUrl;
            MaxEventsPerBatch = maxEventsPerBatch;
            FlushIntervalSeconds = flushIntervalSeconds;
            EnableLogging = enableLogging;
        }
    }
}
