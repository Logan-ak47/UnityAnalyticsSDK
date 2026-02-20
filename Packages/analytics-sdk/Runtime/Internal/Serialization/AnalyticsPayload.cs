using System.Collections.Generic;

namespace Ashutosh.AnalyticsSdk.Internal.Serialization
{
    internal readonly struct AnalyticsPayload
    {
        public AnalyticsContext Context { get; }
        public IReadOnlyList<AnalyticsEvent> Events { get; }

        public AnalyticsPayload(AnalyticsContext context, IReadOnlyList<AnalyticsEvent> eventsList)
        {
            Context = context;
            Events = eventsList;
        }
    }

    internal readonly struct AnalyticsContext
    {
        public string SdkVersion { get; }
        public string UserId { get; }
        public string SessionId { get; }

        public AnalyticsContext(string sdkVersion, string userId, string sessionId)
        {
            SdkVersion = sdkVersion;
            UserId = userId;
            SessionId = sessionId;
        }
    }
}