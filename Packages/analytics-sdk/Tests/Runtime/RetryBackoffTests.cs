using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;
using UnityEngine;
using Ashutosh.AnalyticsSdk.Internal.Retry;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    public class RetryBackoffTests
    {
        [UnityTest]
        public IEnumerator Retryable_SetsCooldown_And_BlocksImmediateRetry()
        {
            var mock = new MockTransport(
                TransportResult.Retryable(0, "net down"),
                TransportResult.Success(200)
            );

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                enableAutoFlush: false,
                enableRetry: true,
                maxRetryAttempts: 5,
                retryBaseDelaySeconds: 2f,
                retryMaxDelaySeconds: 10f
            );

            // FixedRandom(1.0) => full jitter returns cap (max) delay.
            var client = new AnalyticsClient(cfg, mock, new FixedRandom(1.0));

            client.Track("evt", new Dictionary<string, object> { { "a", 1 } });

            var t1 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t1.IsCompleted);

            Assert.AreEqual(1, mock.SendCount);
            Assert.AreEqual(1, client.GetStats().QueuedEventCount); // kept

            // Immediate second flush should be blocked by cooldown
            var t2 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t2.IsCompleted);

            Assert.AreEqual(1, mock.SendCount); // still 1
        }

        [UnityTest]
        public IEnumerator AfterCooldown_Expires_Retry_Happens()
        {
            var mock = new MockTransport(
                TransportResult.Retryable(0, "net down"),
                TransportResult.Success(200)
            );

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                enableAutoFlush: false,
                enableRetry: true,
                maxRetryAttempts: 5,
                retryBaseDelaySeconds: 1f,
                retryMaxDelaySeconds: 10f
            );

            // FixedRandom(0.0) => delay becomes 0 (full jitter min)
            // To actually test waiting, use something like 1.0 and tick enough.
            var client = new AnalyticsClient(cfg, mock, new FixedRandom(1.0));

            client.Track("evt", new Dictionary<string, object> { { "a", 1 } });

            var t1 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t1.IsCompleted);
            Assert.AreEqual(1, mock.SendCount);

            // Tick past cooldown (base=1, attempt1 cap=1, jitter=1 => 1s)
            client.Tick(1.1f);

            var t2 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t2.IsCompleted);

            Assert.AreEqual(2, mock.SendCount);
            Assert.AreEqual(0, client.GetStats().QueuedEventCount);
        }
    }

    internal class FixedRandom : IRandomSource
    {
        private readonly double _value;
        public FixedRandom(double value) { _value = value; }
        public double NextDouble() => _value;
    }
}




