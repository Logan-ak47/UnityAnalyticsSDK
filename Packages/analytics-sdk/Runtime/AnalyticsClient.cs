// AnalyticsClient.cs
using System;

namespace Ashutosh.AnalyticsSdk
{
    public sealed class AnalyticsClient : IAnalyticsClient
    {
        private readonly AnalyticsConfig _config;
        private string _userId;
        private string _sessionId;

        public AnalyticsClient(AnalyticsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void SetUserId(string userId) => _userId = userId;
        public void SetSessionId(string sessionId) => _sessionId = sessionId;

        public void Track(AnalyticsEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));
            // Day 2+: enqueue event
        }

        public void Flush()
        {
            // Day 4+: send batch via transport
        }
    }
}
