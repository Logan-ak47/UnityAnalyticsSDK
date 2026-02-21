using System.Collections.Generic;

namespace Ashutosh.AnalyticsSdk.Internal
{
    internal sealed class EventQueue
    {
        private readonly Queue<AnalyticsEvent> _queue = new Queue<AnalyticsEvent>();

        public int Count => _queue.Count;

        public void Enqueue(AnalyticsEvent evt) => _queue.Enqueue(evt);

        public List<AnalyticsEvent> PeekBatch(int maxCount)
        {
            if (maxCount <= 0 || _queue.Count == 0) return new List<AnalyticsEvent>(0);

            int take = _queue.Count < maxCount ? _queue.Count : maxCount;
            var batch = new List<AnalyticsEvent>(take);

            // Enumerating a Queue does not dequeue; it’s safe for peeking.
            int i = 0;
            foreach (var e in _queue)
            {
                batch.Add(e);
                i++;
                if (i >= take) break;
            }

            return batch;
        }

        public void DropBatch(int count)
        {
            int drop = count < _queue.Count ? count : _queue.Count;
            for (int i = 0; i < drop; i++)
                _queue.Dequeue();
        }
    }
}