using UnityEngine;

namespace Ashutosh.AnalyticsSdk.Internal.Logging
{
    internal static class SdkLogger
    {
        internal static bool Enabled;

        internal static void Warn(string msg)
        {
            if (Enabled) Debug.LogWarning($"[AnalyticsSdk] {msg}");
        }

        internal static void Info(string msg)
        {
            if (Enabled) Debug.Log($"[AnalyticsSdk] {msg}");
        }
    }
}