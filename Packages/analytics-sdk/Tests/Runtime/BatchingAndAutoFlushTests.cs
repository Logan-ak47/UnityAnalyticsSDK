using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;
using UnityEngine;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    public class BatchingAndAutoFlushTests
    {
        [UnityTest]
        public IEnumerator Flush_Sends_Multiple_Batches_Until_Empty()
        {
            var mock = new MockTransport(
                TransportResult.Success(200),
                TransportResult.Success(200),
                TransportResult.Success(200)
            );

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                maxEventsPerBatch: 2,
                maxBatchesPerFlush: 10,
                flushIntervalSeconds: 999f,
                enableAutoFlush: false
            );

            var client = new AnalyticsClient(cfg, mock);

            // 5 events -> batches: 2 + 2 + 1 => 3 sends
            for (int i = 0; i < 5; i++)
                client.Track($"evt{i}", new Dictionary<string, object> { { "i", i } });

            var task = client.FlushUpToAsync(10);
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.AreEqual(3, mock.SendCount);
            Assert.AreEqual(0, client.GetStats().QueuedEventCount);
        }

        [UnityTest]
        public IEnumerator Flush_Respects_MaxBatchesPerFlush()
        {
            var mock = new MockTransport(
                TransportResult.Success(200),
                TransportResult.Success(200),
                TransportResult.Success(200)
            );

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                maxEventsPerBatch: 2,
                maxBatchesPerFlush: 1,
                flushIntervalSeconds: 999f,
                enableAutoFlush: false
            );

            var client = new AnalyticsClient(cfg, mock);

            for (int i = 0; i < 5; i++)
                client.Track($"evt{i}", new Dictionary<string, object> { { "i", i } });

            var task = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.AreEqual(1, mock.SendCount);
            // Only 1 batch of 2 should have been dropped
            Assert.AreEqual(3, client.GetStats().QueuedEventCount);
        }

        [UnityTest]
        public IEnumerator Tick_AutoFlush_Triggers_After_Interval()
        {
            var mock = new MockTransport(TransportResult.Success(200));

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                maxEventsPerBatch: 25,
                maxBatchesPerFlush: 1,
                flushIntervalSeconds: 1.0f,
                enableAutoFlush: true
            );

            var client = new AnalyticsClient(cfg, mock);

            client.Track("evt", new Dictionary<string, object> { { "a", 1 } });

            // Not enough time yet
            client.Tick(0.5f);
            Assert.AreEqual(0, mock.SendCount);

            // Cross the 1s interval
            client.Tick(0.6f);

            // Wait for async send completion
            yield return null;

            Assert.AreEqual(1, mock.SendCount);
        }
    }
}