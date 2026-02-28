using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;
using Ashutosh.AnalyticsSdk.Tests.Runtime.TestKit;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    public class DiskQueueMaxSizeTests
    {
        [UnityTest]
        public IEnumerator DiskQueue_Respects_MaxDiskBytes()
        {
            using var temp = new TempDirScope("analytics_sdk_maxsize_");

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                enableAutoFlush: false,
                enableDiskPersistence: true,
                maxDiskBytes: 2048, // small cap
                storagePathOverride: temp.Path
            );

            var client = new AnalyticsClient(cfg, new MockTransport(TransportResult.Retryable(0, "offline")));

            // Add many events so persistence has pressure
            for (int i = 0; i < 200; i++)
            {
                client.Track("evt", new Dictionary<string, object>
                {
                    { "i", i },
                    { "msg", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }
                });
            }

            // Find queue file (don’t assume exact name if you changed it)
            var files = Directory.GetFiles(temp.Path, "*", SearchOption.AllDirectories);
            long maxFound = 0;
            foreach (var f in files)
            {
                var fi = new FileInfo(f);
                if (fi.Length > maxFound) maxFound = fi.Length;
            }

            Assert.LessOrEqual(maxFound, cfg.MaxDiskBytes, "Expected persisted data to respect MaxDiskBytes cap.");

            yield return null;
        }
    }
}