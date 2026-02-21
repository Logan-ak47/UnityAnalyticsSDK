using System.Collections.Generic;
using UnityEngine;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;

public class Test : MonoBehaviour
{
     private IAnalyticsClient _client;
    private MockTransport _mock;

    void Start()
    {
        _mock = new MockTransport(TransportResult.Success(200));
        _client = new AnalyticsClient(new AnalyticsConfig("https://example.com"), _mock);

        _client.Track("evt", new Dictionary<string, object>{{"a", 1}});
        _client.Flush();

        Debug.Log("SendCount=" + _mock.SendCount);
    }
}
