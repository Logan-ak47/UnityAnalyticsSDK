using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ashutosh.AnalyticsSdk.Transports
{
    /// <summary>
    /// Test transport that returns scripted results in sequence.
    /// </summary>
    public sealed class MockTransport : ITransport
    {
        private readonly Queue<TransportResult> _results = new Queue<TransportResult>();

        /// <summary>
        /// Number of times <see cref="SendAsync"/> has been called.
        /// </summary>
        public int SendCount { get; private set; }

        /// <summary>
        /// Creates a mock transport with optional scripted responses.
        /// </summary>
        /// <param name="results">Results returned in FIFO order.</param>
        public MockTransport(params TransportResult[] results)
        {
            if (results != null)
            {
                foreach (var r in results) _results.Enqueue(r);
            }
        }

        /// <summary>
        /// Returns the next scripted result, or success when none are queued.
        /// </summary>
        /// <param name="payload">Ignored payload bytes.</param>
        /// <param name="contentType">Ignored content type.</param>
        /// <param name="ct">Cancellation token (unused).</param>
        public Task<TransportResult> SendAsync(byte[] payload, string contentType, CancellationToken ct)
        {
            SendCount++;

            if (_results.Count > 0)
                return Task.FromResult(_results.Dequeue());

            // Default to success if no scripted results provided
            return Task.FromResult(TransportResult.Success(200));
        }

        /// <summary>
        /// Adds a scripted result to the end of the queue.
        /// </summary>
        /// <param name="result">Result to return on a future send.</param>
        public void EnqueueResult(TransportResult result) => _results.Enqueue(result);
    }
}
