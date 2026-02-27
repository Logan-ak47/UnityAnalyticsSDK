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
    public class OfflineRecoveryIntegrationTests
    {
        [UnityTest]
        public IEnumerator DiskQueue_OfflineThenRestart_OnlineFlushes()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "analytics_sdk_recover_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var cfg = new AnalyticsConfig(
                    endpointUrl: "https://example.com",
                    enableAutoFlush: false,
                    enableDiskPersistence: true,
                    storagePathOverride: tempDir,
                    enableRetry: true,
                    maxRetryAttempts: 5,
                    retryBaseDelaySeconds: 1f,
                    retryMaxDelaySeconds: 10f
                );

                // First run: offline (retryable)
                var offlineTransport = new MockTransport(TransportResult.Retryable(0, "offline"));
                var c1 = new AnalyticsClient(cfg, offlineTransport);

                c1.Track("evt1", new Dictionary<string, object> { { "a", 1 } });

                var t1 = c1.FlushUpToAsync(1);
                yield return new WaitUntil(() => t1.IsCompleted);

                // Should still be queued (kept on retryable)
                Assert.AreEqual(1, c1.GetStats().QueuedEventCount);

                // "Restart app": new client reads persisted queue
                var onlineTransport = new MockTransport(TransportResult.Success(200));
                var c2 = new AnalyticsClient(cfg, onlineTransport);

                Assert.AreEqual(1, c2.GetStats().QueuedEventCount);

                var t2 = c2.FlushUpToAsync(1);
                yield return new WaitUntil(() => t2.IsCompleted);

                Assert.AreEqual(0, c2.GetStats().QueuedEventCount);
                Assert.AreEqual(1, onlineTransport.SendCount);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
            }
        }
    }
}