using System.Collections.Generic;

namespace Ashutosh.AnalyticsSdk
{
    /// <summary>
    /// Public API for tracking analytics events and flushing queued data.
    /// </summary>
    public interface IAnalyticsClient
    {
        /// <summary>
        /// Sets the user identifier attached to future events.
        /// </summary>
        /// <param name="userId">Application-defined user ID.</param>
        void SetUserId(string userId);

        /// <summary>
        /// Sets the session identifier attached to future events.
        /// </summary>
        /// <param name="sessionId">Application-defined session ID.</param>
        void SetSessionId(string sessionId);

        /// <summary>
        /// Queues an event by name with optional properties.
        /// </summary>
        /// <param name="eventName">Event name to record.</param>
        /// <param name="properties">Optional event properties.</param>
        void Track(string eventName, IReadOnlyDictionary<string, object> properties = null);

        /// <summary>
        /// Queues a pre-built analytics event.
        /// </summary>
        /// <param name="evt">Event instance to validate and enqueue.</param>
        void Track(AnalyticsEvent evt);

        /// <summary>
        /// Starts an asynchronous flush of queued events.
        /// </summary>
        void Flush();

        /// <summary>
        /// Returns current queue and flush status information.
        /// </summary>
        AnalyticsStats GetStats();
    }
}
