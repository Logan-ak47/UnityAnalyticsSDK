using System;
using System.IO;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime.TestKit
{
    internal sealed class TempDirScope : IDisposable
    {
        public string Path { get; }

        public TempDirScope(string prefix = "analytics_sdk_test_")
        {
            var dir = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                prefix + Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(dir);
            Path = dir;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, true);
            }
            catch
            {
                // ignore cleanup errors in tests
            }
        }
    }
}