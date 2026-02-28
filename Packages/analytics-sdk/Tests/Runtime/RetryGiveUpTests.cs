using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;
using UnityEngine;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    public class RetryGiveUpTests
    {
        [UnityTest]
        public IEnumerator Retry_GivesUpAfterMaxAttempts_DropsBatch()
        {
            // Always retryable -> should eventually drop batch after max attempts
            var mock = new MockTransport(
                TransportResult.Retryable(0, "offline"),
                TransportResult.Retryable(0, "offline"),
                TransportResult.Retryable(0, "offline"),
                TransportResult.Retryable(0, "offline")
            );

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                enableAutoFlush: false,
                enableDiskPersistence: false,
                enableRetry: true,
                maxRetryAttempts: 2,          // give up quickly
                retryBaseDelaySeconds: 0.1f,
                retryMaxDelaySeconds: 1f
            );

            // Use jitter=0 so cooldown becomes 0 and retries can be attempted immediately
            var client = new AnalyticsClient(cfg, mock, new FixedRandom(0.0));

            client.Track("evt", new Dictionary<string, object> { { "a", 1 } });

            // attempt 1
            var t1 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t1.IsCompleted);
            Assert.AreEqual(1, mock.SendCount);
            Assert.AreEqual(1, client.GetStats().QueuedEventCount);

            // attempt 2
            var t2 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t2.IsCompleted);
            Assert.AreEqual(2, mock.SendCount);
            Assert.AreEqual(1, client.GetStats().QueuedEventCount);

            // attempt 3 -> exceeds maxRetryAttempts => should drop
            var t3 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t3.IsCompleted);

            Assert.AreEqual(3, mock.SendCount);
            Assert.AreEqual(0, client.GetStats().QueuedEventCount);
        }
    }
}