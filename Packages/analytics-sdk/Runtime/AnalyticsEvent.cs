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
            : this(name, DateTimeOffset.UtcNow, properties)
        {
        }

        internal AnalyticsEvent(string name, DateTimeOffset timestamp, IReadOnlyDictionary<string, object> properties)
        {
            Name = name;
            Timestamp = timestamp;
            Properties = properties ?? new Dictionary<string, object>();
        }
    }
}







