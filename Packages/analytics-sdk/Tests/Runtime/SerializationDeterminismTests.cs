using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Ashutosh.AnalyticsSdk.Internal.Serialization;

namespace Ashutosh.AnalyticsSdk.Tests.Runtime
{
    public class SerializationDeterminismTests
    {
        [Test]
        public void Json_Is_Deterministic_For_Same_Input()
        {
            var ts = new DateTimeOffset(2026, 02, 19, 12, 00, 00, TimeSpan.Zero);

            var props = new Dictionary<string, object>
            {
                { "b", (long)2 },
                { "a", "hello" }
            };

            // Use internal ctor (enabled via InternalsVisibleTo)
            var e = new AnalyticsEvent("test_event", ts, props);

            var payload = new AnalyticsPayload(
                new AnalyticsContext("0.1.0", "u1", "s1"),
                new[] { e }
            );

            var ser = new JsonEventSerializer();

            var j1 = Encoding.UTF8.GetString(ser.Serialize(payload));
            var j2 = Encoding.UTF8.GetString(ser.Serialize(payload));

            Assert.AreEqual(j1, j2);
        }

        [Test]
        public void Json_Sorts_Property_Keys()
        {
            var ts = new DateTimeOffset(2026, 02, 19, 12, 00, 00, TimeSpan.Zero);

            var props = new Dictionary<string, object>
            {
                { "z", (long)1 },
                { "a", (long)2 }
            };

            var e = new AnalyticsEvent("evt", ts, props);
            var payload = new AnalyticsPayload(new AnalyticsContext("0.1.0", "u", "s"), new[] { e });

            var ser = new JsonEventSerializer();
            var json = Encoding.UTF8.GetString(ser.Serialize(payload));

            // Ensure "a" appears before "z" inside props
            var idxA = json.IndexOf("\"a\":", StringComparison.Ordinal);
            var idxZ = json.IndexOf("\"z\":", StringComparison.Ordinal);

            Assert.IsTrue(idxA >= 0 && idxZ >= 0, "Keys not found in JSON.");
            Assert.Less(idxA, idxZ, "Expected key 'a' before 'z' for determinism.");
        }

        [Test]
        public void Json_Escapes_Strings()
        {
            var ts = new DateTimeOffset(2026, 02, 19, 12, 00, 00, TimeSpan.Zero);

            var props = new Dictionary<string, object>
            {
                { "msg", "hi \"unity\"\nline2" }
            };

            var e = new AnalyticsEvent("evt", ts, props);
            var payload = new AnalyticsPayload(new AnalyticsContext("0.1.0", "u", "s"), new[] { e });

            var ser = new JsonEventSerializer();
            var json = Encoding.UTF8.GetString(ser.Serialize(payload));

            StringAssert.Contains("\\\"", json);
            StringAssert.Contains("\\n", json);
        }
    }
}