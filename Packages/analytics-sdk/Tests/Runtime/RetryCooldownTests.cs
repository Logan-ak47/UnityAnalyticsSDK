using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;
using Ashutosh.AnalyticsSdk.Internal.Retry;
using UnityEngine;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    internal sealed class FixedRandom : IRandomSource
    {
        private readonly double _value;
        public FixedRandom(double value) { _value = value; }
        public double NextDouble() => _value;
    }

    public class RetryCooldownTests
    {
        [UnityTest]
        public IEnumerator Retryable_SetsCooldown_And_BlocksImmediateRetry()
        {
            // First attempt fails retryably, second would succeed if allowed
            var mock = new MockTransport(
                TransportResult.Retryable(0, "offline"),
                TransportResult.Success(200)
            );

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                enableAutoFlush: false,
                enableDiskPersistence: false,
                enableRetry: true,
                maxRetryAttempts: 5,
                retryBaseDelaySeconds: 1f,
                retryMaxDelaySeconds: 10f
            );

            // jitter=1 => delay becomes cap (>= baseDelay), so cooldown > 0
            var client = new AnalyticsClient(cfg, mock, new FixedRandom(1.0));

            client.Track("evt", new Dictionary<string, object> { { "a", 1 } });

            var t1 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t1.IsCompleted);

            Assert.AreEqual(1, mock.SendCount);
            Assert.AreEqual(1, client.GetStats().QueuedEventCount); // kept on retryable

            // Immediate retry should be blocked by cooldown: no additional send
            var t2 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t2.IsCompleted);

            Assert.AreEqual(1, mock.SendCount);
        }

        [UnityTest]
        public IEnumerator AfterCooldownExpires_RetryHappens()
        {
            var mock = new MockTransport(
                TransportResult.Retryable(0, "offline"),
                TransportResult.Success(200)
            );

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                enableAutoFlush: false,
                enableDiskPersistence: false,
                enableRetry: true,
                maxRetryAttempts: 5,
                retryBaseDelaySeconds: 1f,
                retryMaxDelaySeconds: 10f
            );

            var client = new AnalyticsClient(cfg, mock, new FixedRandom(1.0));

            client.Track("evt", new Dictionary<string, object> { { "a", 1 } });

            var t1 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t1.IsCompleted);
            Assert.AreEqual(1, mock.SendCount);

            // attempt1 cap is baseDelay=1s, jitter=1 => 1s cooldown
            client.Tick(1.1f);

            var t2 = client.FlushUpToAsync(1);
            yield return new WaitUntil(() => t2.IsCompleted);

            Assert.AreEqual(2, mock.SendCount);
            Assert.AreEqual(0, client.GetStats().QueuedEventCount);
        }
    }
}