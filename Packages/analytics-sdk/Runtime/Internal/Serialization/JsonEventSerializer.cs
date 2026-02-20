using System;
using System.Collections.Generic;
using System.Text;

namespace Ashutosh.AnalyticsSdk.Internal.Serialization
{
    internal sealed class JsonEventSerializer : IEventSerializer
    {
        public string ContentType => "application/json";

        public byte[] Serialize(AnalyticsPayload payload)
        {
            var w = new DeterministicJsonWriter();

            // Build JSON:
            // {
            //   "sdkVersion": "...",
            //   "userId": "...",
            //   "sessionId": "...",
            //   "events": [ { "name": "...", "ts": "...", "props": { ... } } ]
            // }

            var root = new Dictionary<string, object>(4)
            {
                { "sdkVersion", payload.Context.SdkVersion },
                { "userId", payload.Context.UserId ?? "" },
                { "sessionId", payload.Context.SessionId ?? "" },
            };

            var eventsArr = new List<object>(payload.Events.Count);
            for (int i = 0; i < payload.Events.Count; i++)
            {
                var e = payload.Events[i];
                var evtObj = new Dictionary<string, object>(3)
                {
                    { "name", e.Name },
                    { "ts", e.Timestamp.ToUniversalTime().ToString("o") },
                    { "props", e.Properties is Dictionary<string, object> d ? d : new Dictionary<string, object>(e.Properties) }
                };
                eventsArr.Add(evtObj);
            }

            root["events"] = eventsArr;

            w.WriteValue(root);

            return Encoding.UTF8.GetBytes(w.ToString());
        }
    }
}