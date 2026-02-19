using System;
using System.Collections.Generic;
using Ashutosh.AnalyticsSdk.Internal;
using Ashutosh.AnalyticsSdk.Internal.Validation;

namespace Ashutosh.AnalyticsSdk
{
    public sealed class AnalyticsClient : IAnalyticsClient
    {
        private readonly AnalyticsConfig _config;
        private readonly EventQueue _queue;

        private string _userId;
        private string _sessionId;

        private DateTimeOffset? _lastFlushTimeUtc;
        private string _lastError;

        public AnalyticsClient(AnalyticsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _queue = new EventQueue();
        }

        public void SetUserId(string userId) => _userId = userId;
        public void SetSessionId(string sessionId) => _sessionId = sessionId;

        public void Track(string eventName, IReadOnlyDictionary<string, object> properties = null)
        {
            if (!EventValidator.TrySanitize(eventName, properties, out var sanitizedName, out var sanitizedProps, out var error))
            {
                _lastError = $"Track dropped event: {error}";
                if (_config.EnableLogging) UnityEngine.Debug.LogWarning(_lastError);
                return;
            }

            var evt = new AnalyticsEvent(sanitizedName, sanitizedProps);
            Track(evt);
        }

        public void Track(AnalyticsEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            if (!EventValidator.TrySanitize(evt.Name, evt.Properties, out var sanitizedName, out var sanitizedProps, out var error))
            {
                _lastError = $"Track dropped event: {error}";
                if (_config.EnableLogging) UnityEngine.Debug.LogWarning(_lastError);
                return;
            }

            // Preserve original timestamp
            var sanitizedEvt = new AnalyticsEvent(sanitizedName, evt.Timestamp, sanitizedProps);
            _queue.Enqueue(sanitizedEvt);
        }

        public void Flush()
        {
            _lastFlushTimeUtc = DateTimeOffset.UtcNow;
            // Day 4+: send batches via transport
        }

        public AnalyticsStats GetStats()
        {
            return new AnalyticsStats(
                queuedEventCount: _queue.Count,
                lastFlushTimeUtc: _lastFlushTimeUtc,
                lastError: _lastError
            );
        }
    }
}
