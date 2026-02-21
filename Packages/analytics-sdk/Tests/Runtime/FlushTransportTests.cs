using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;
using UnityEngine;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    public class FlushTransportTests
    {
        [UnityTest]
        public IEnumerator Flush_Success_Drops_Batch()
        {
            var mock = new MockTransport(TransportResult.Success(200));
            var client = new AnalyticsClient(new AnalyticsConfig("https://example.com"), mock);

            client.Track("evt1", new Dictionary<string, object> { { "a", 1 } });
            client.Track("evt2", new Dictionary<string, object> { { "b", 2 } });

            var task = client.FlushOnceAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.AreEqual(0, client.GetStats().QueuedEventCount);
            Assert.AreEqual(1, mock.SendCount);
        }

        [UnityTest]
        public IEnumerator Flush_Retryable_Keeps_Batch()
        {
            var mock = new MockTransport(TransportResult.Retryable(0, "network"));
            var client = new AnalyticsClient(new AnalyticsConfig("https://example.com"), mock);

            client.Track("evt1", new Dictionary<string, object> { { "a", 1 } });

            var task = client.FlushOnceAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.AreEqual(1, client.GetStats().QueuedEventCount);
            Assert.AreEqual(1, mock.SendCount);
        }

        [UnityTest]
        public IEnumerator Flush_Fatal_Drops_Batch()
        {
            var mock = new MockTransport(TransportResult.Fatal(400, "bad request"));
            var client = new AnalyticsClient(new AnalyticsConfig("https://example.com"), mock);

            client.Track("evt1", new Dictionary<string, object> { { "a", 1 } });

            var task = client.FlushOnceAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.AreEqual(0, client.GetStats().QueuedEventCount);
            Assert.AreEqual(1, mock.SendCount);
        }
    }
}