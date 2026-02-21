using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ashutosh.AnalyticsSdk.Transports
{
    public sealed class MockTransport : ITransport
    {
        private readonly Queue<TransportResult> _results = new Queue<TransportResult>();

        public int SendCount { get; private set; }

        public MockTransport(params TransportResult[] results)
        {
            if (results != null)
            {
                foreach (var r in results) _results.Enqueue(r);
            }
        }

        public Task<TransportResult> SendAsync(byte[] payload, string contentType, CancellationToken ct)
        {
            SendCount++;

            if (_results.Count > 0)
                return Task.FromResult(_results.Dequeue());

            // Default to success if no scripted results provided
            return Task.FromResult(TransportResult.Success(200));
        }

        public void EnqueueResult(TransportResult result) => _results.Enqueue(result);
    }
}