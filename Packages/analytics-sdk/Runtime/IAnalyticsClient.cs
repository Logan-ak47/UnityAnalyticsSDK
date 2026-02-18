// IAnalyticsClient.cs
namespace Ashutosh.AnalyticsSdk
{
    public interface IAnalyticsClient
    {
        void SetUserId(string userId);
        void SetSessionId(string sessionId);
        void Track(AnalyticsEvent evt);
        void Flush();
    }
}
