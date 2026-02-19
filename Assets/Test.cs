using System.Collections.Generic;
using UnityEngine;
using Ashutosh.AnalyticsSdk;

public class Test : MonoBehaviour
{
    void Start()
    {
        var client = new AnalyticsClient(new AnalyticsConfig("https://example.com"));

        client.Track("level_start", new Dictionary<string, object>
        {
            { "level", 3 },
            { "mode", "hard" },
            { "bad_value", this } // should be dropped by validator
        });

        var stats = client.GetStats();
        Debug.Log($"Queued: {stats.QueuedEventCount}, LastError: {stats.LastError}");
    }
}
