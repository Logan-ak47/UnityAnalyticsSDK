using System.Collections.Generic;

namespace Ashutosh.AnalyticsSdk.Internal
{
    internal sealed class EventQueue
    {
        private readonly Queue<AnalyticsEvent> _queue = new Queue<AnalyticsEvent>();

        public int Count => _queue.Count;

        public void Enqueue(AnalyticsEvent evt)
        {
            _queue.Enqueue(evt);
        }
    }
}
