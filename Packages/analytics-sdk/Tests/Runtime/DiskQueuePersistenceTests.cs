using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;
using UnityEngine;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    public class DiskQueuePersistenceTests
    {
        [UnityTest]
        public IEnumerator DiskQueue_Persists_And_Reloads()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "analytics_sdk_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var cfg = new AnalyticsConfig(
                    endpointUrl: "https://example.com",
                    enableAutoFlush: false,
                    enableDiskPersistence: true,
                    storagePathOverride: tempDir
                );

                var c1 = new AnalyticsClient(cfg, new MockTransport(TransportResult.Retryable(0, "offline")));
                c1.Track("evt1", new Dictionary<string, object> { { "a", 1 } });
                c1.Track("evt2", new Dictionary<string, object> { { "b", "x" } });

                Assert.AreEqual(2, c1.GetStats().QueuedEventCount);

                // New client simulates app restart
                var c2 = new AnalyticsClient(cfg, new MockTransport(TransportResult.Retryable(0, "offline")));
                Assert.AreEqual(2, c2.GetStats().QueuedEventCount);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DiskQueue_FlushSuccess_Updates_File()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "analytics_sdk_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var cfg = new AnalyticsConfig(
                    endpointUrl: "https://example.com",
                    maxEventsPerBatch: 25,
                    maxBatchesPerFlush: 4,
                    enableAutoFlush: false,
                    enableDiskPersistence: true,
                    storagePathOverride: tempDir
                );

                var mock = new MockTransport(TransportResult.Success(200));
                var client = new AnalyticsClient(cfg, mock);

                client.Track("evt1", new Dictionary<string, object> { { "a", 1 } });

                var task = client.FlushUpToAsync(1);
                yield return new WaitUntil(() => task.IsCompleted);

                Assert.AreEqual(0, client.GetStats().QueuedEventCount);

                // Reload should also be empty
                var client2 = new AnalyticsClient(cfg, new MockTransport(TransportResult.Success(200)));
                Assert.AreEqual(0, client2.GetStats().QueuedEventCount);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
            }
        }
    }
}