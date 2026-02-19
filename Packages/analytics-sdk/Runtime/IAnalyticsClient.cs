using System.Collections.Generic;

namespace Ashutosh.AnalyticsSdk
{
    public interface IAnalyticsClient
    {
        void SetUserId(string userId);
        void SetSessionId(string sessionId);

        void Track(string eventName, IReadOnlyDictionary<string, object> properties = null);
        void Track(AnalyticsEvent evt);

        void Flush();

        AnalyticsStats GetStats();
    }
}
