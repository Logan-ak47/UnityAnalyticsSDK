using System;
using System.Collections.Generic;

namespace Ashutosh.AnalyticsSdk
{
    /// <summary>
    /// Represents a single analytics event queued for upload.
    /// </summary>
    public sealed class AnalyticsEvent
    {
        /// <summary>
        /// Event name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// UTC timestamp for when the event was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Event property bag.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; }

        /// <summary>
        /// Creates an event with the current UTC timestamp.
        /// </summary>
        /// <param name="name">Event name.</param>
        /// <param name="properties">Optional event properties.</param>
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







