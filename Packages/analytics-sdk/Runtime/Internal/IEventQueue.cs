using System.Collections.Generic;

namespace Ashutosh.AnalyticsSdk.Internal
{
    internal interface IEventQueue
    {
        int Count { get; }
        void Enqueue(AnalyticsEvent evt);
        List<AnalyticsEvent> PeekBatch(int maxCount);
        void DropBatch(int count);
    }
}