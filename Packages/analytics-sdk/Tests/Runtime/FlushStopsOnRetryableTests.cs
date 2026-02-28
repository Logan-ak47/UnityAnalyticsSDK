using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;
using UnityEngine;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    public class FlushStopsOnRetryableTests
    {
        [UnityTest]
        public IEnumerator Flush_Stops_On_Retryable_DoesNotSendMoreBatches()
        {
            // Success for first batch, retryable for second, success would be third if it continued
            var mock = new MockTransport(
                TransportResult.Success(200),
                TransportResult.Retryable(0, "offline"),
                TransportResult.Success(200)
            );

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                maxEventsPerBatch: 2,
                maxBatchesPerFlush: 10,
                enableAutoFlush: false,
                enableDiskPersistence: false,
                enableRetry: true,
                maxRetryAttempts: 5,
                retryBaseDelaySeconds: 10f,   // long cooldown so it definitely blocks continuing
                retryMaxDelaySeconds: 10f
            );

            var client = new AnalyticsClient(cfg, mock, new FixedRandom(1.0));

            // 5 events => 3 batches (2,2,1)
            for (int i = 0; i < 5; i++)
                client.Track($"evt{i}", new Dictionary<string, object> { { "i", i } });

            var t = client.FlushUpToAsync(10);
            yield return new WaitUntil(() => t.IsCompleted);

            // Should have attempted only first two sends, then stopped on retryable
            Assert.AreEqual(2, mock.SendCount);

            // First batch dropped (2), second kept (2), third untouched (1) => 3 queued
            Assert.AreEqual(3, client.GetStats().QueuedEventCount);
        }
    }
}