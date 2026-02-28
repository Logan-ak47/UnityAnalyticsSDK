using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;
using Ashutosh.AnalyticsSdk.Tests.Runtime.TestKit;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    public class DiskQueueCorruptionTests
    {
        [UnityTest]
        public IEnumerator DiskQueue_CorruptFile_DoesNotCrash_AndStartsEmpty()
        {
            using var temp = new TempDirScope("analytics_sdk_corrupt_");

            // IMPORTANT:
            // This path assumes your DiskBackedEventQueue uses:
            // basePath/<DirectoryName>/<FileName>
            // where DirectoryName="AshutoshAnalyticsSdk" and FileName="event_queue.dat".
            // If you used different names, update these two strings.
            var dir = Path.Combine(temp.Path, "AshutoshAnalyticsSdk");
            Directory.CreateDirectory(dir);

            var file = Path.Combine(dir, "event_queue.dat");

            // Write garbage
            File.WriteAllBytes(file, new byte[] { 1, 2, 3, 4, 5, 6, 7 });

            var cfg = new AnalyticsConfig(
                endpointUrl: "https://example.com",
                enableAutoFlush: false,
                enableDiskPersistence: true,
                storagePathOverride: temp.Path,
                enableRetry: true
            );

            // Creating client should not throw even if file is corrupted
            var client = new AnalyticsClient(cfg, new MockTransport(TransportResult.Success(200)));

            Assert.AreEqual(0, client.GetStats().QueuedEventCount);

            yield return null;
        }
    }
}