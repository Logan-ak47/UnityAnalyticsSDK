// AnalyticsEvent.cs
using System;
using System.Collections.Generic;

namespace Ashutosh.AnalyticsSdk
{
    public sealed class AnalyticsEvent
    {
        public string Name { get; }
        public DateTimeOffset Timestamp { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }

        public AnalyticsEvent(string name, IReadOnlyDictionary<string, object> properties = null)
        {
            Name = name;
            Timestamp = DateTimeOffset.UtcNow;
            Properties = properties ?? new Dictionary<string, object>();
        }
    }
}
