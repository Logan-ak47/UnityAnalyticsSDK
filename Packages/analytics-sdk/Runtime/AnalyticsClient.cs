using System;
using System.Collections.Generic;
using Ashutosh.AnalyticsSdk.Internal;
using Ashutosh.AnalyticsSdk.Internal.Validation;

using System.Threading.Tasks;
using Ashutosh.AnalyticsSdk.Internal.Serialization;
using Ashutosh.AnalyticsSdk.Transports;
using Ashutosh.AnalyticsSdk.Internal.Logging;
using UnityEngine;

namespace Ashutosh.AnalyticsSdk
{
    /// <summary>
    /// Default analytics client implementation with in-memory batching and flushing.
    /// </summary>
    public sealed class AnalyticsClient : IAnalyticsClient
    {
        private readonly AnalyticsConfig _config;
        //private readonly EventQueue _queue;

        private string _userId;
        private string _sessionId;

        private readonly IEventQueue _queue;

        private DateTimeOffset? _lastFlushTimeUtc;
        private string _lastError;



        private readonly ITransport _transport;
        private readonly IEventSerializer _serializer;
        private bool _isFlushing;
        private const string SdkVersion = "0.1.0"; // later: derive from package version/tag

        private float _flushTimer;

        /// <summary>
        /// Creates a client using the provided configuration and optional transport.
        /// </summary>
        /// <param name="config">Client configuration.</param>
        /// <param name="transport">Optional custom transport. Uses UnityWebRequest by default.</param>
        public AnalyticsClient(AnalyticsConfig config, ITransport transport = null)
        {

            _config = config ?? throw new ArgumentNullException(nameof(config));
            var basePath = string.IsNullOrEmpty(_config.StoragePathOverride)
    ? Application.persistentDataPath
    : _config.StoragePathOverride;

            _queue = _config.EnableDiskPersistence
                ? new Ashutosh.AnalyticsSdk.Internal.Storage.DiskBackedEventQueue(basePath, _config.MaxDiskBytes)
                : new Ashutosh.AnalyticsSdk.Internal.MemoryEventQueue();
            _serializer = new JsonEventSerializer();
            _transport = transport ?? new UnityWebRequestTransport(_config.EndpointUrl);

            SdkLogger.Enabled = _config.EnableLogging;

            if (_config.EnableAutoFlush && _config.FlushIntervalSeconds > 0f)
            {
                Ashutosh.AnalyticsSdk.Internal.AnalyticsRuntime.Register(this);
            }
        }

        /// <summary>
        /// Sets the user ID attached to future flushed events.
        /// </summary>
        /// <param name="userId">Application-defined user ID.</param>
        public void SetUserId(string userId) => _userId = userId;

        /// <summary>
        /// Sets the session ID attached to future flushed events.
        /// </summary>
        /// <param name="sessionId">Application-defined session ID.</param>
        public void SetSessionId(string sessionId) => _sessionId = sessionId;

        /// <summary>
        /// Validates and queues an event by name.
        /// </summary>
        /// <param name="eventName">Event name to record.</param>
        /// <param name="properties">Optional event properties.</param>
        public void Track(string eventName, IReadOnlyDictionary<string, object> properties = null)
        {
            if (!EventValidator.TrySanitize(eventName, properties, out var sanitizedName, out var sanitizedProps, out var error))
            {
                _lastError = $"Track dropped event: {error}";
                SdkLogger.Warn(_lastError);
                return;
            }

            var evt = new AnalyticsEvent(sanitizedName, sanitizedProps);
            Track(evt);
        }

        /// <summary>
        /// Validates and queues a pre-built event.
        /// </summary>
        /// <param name="evt">Event instance to enqueue.</param>
        public void Track(AnalyticsEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            if (!EventValidator.TrySanitize(evt.Name, evt.Properties, out var sanitizedName, out var sanitizedProps, out var error))
            {
                _lastError = $"Track dropped event: {error}";
                SdkLogger.Warn(_lastError);
                return;
            }

            // Preserve original timestamp
            var sanitizedEvt = new AnalyticsEvent(sanitizedName, evt.Timestamp, sanitizedProps);
            _queue.Enqueue(sanitizedEvt);
        }

        /// <summary>
        /// Starts an asynchronous flush of queued events.
        /// </summary>
        public void Flush()
        {
            _ = FlushUpToAsync(_config.MaxBatchesPerFlush);
        }


        internal async Task FlushUpToAsync(int maxBatches)
        {
            if (_isFlushing) return;
            _isFlushing = true;

            try
            {
                int batchesSent = 0;
                if (maxBatches <= 0) maxBatches = 1;

                while (_queue.Count > 0 && batchesSent < maxBatches)
                {
                    var batch = _queue.PeekBatch(_config.MaxEventsPerBatch);
                    if (batch.Count == 0) break;

                    var payload = new AnalyticsPayload(
                        new AnalyticsContext(SdkVersion, _userId, _sessionId),
                        batch
                    );

                    var bytes = _serializer.Serialize(payload);
                    var result = await _transport.SendAsync(bytes, _serializer.ContentType, default);

                    batchesSent++;

                    if (result.IsSuccess)
                    {
                        _queue.DropBatch(batch.Count);
                        _lastError = null;
                        _lastFlushTimeUtc = System.DateTimeOffset.UtcNow;
                        continue;
                    }

                    _lastError = $"Flush failed ({result.StatusCode}) {result.Error}";
                    _lastFlushTimeUtc = System.DateTimeOffset.UtcNow;

                    if (result.IsRetryable)
                    {
                        // Keep queue intact; stop here. Next tick/manual flush will retry.
                        break;
                    }

                    // Fatal: drop the batch to avoid blocking forever, then stop to avoid spamming.
                    _queue.DropBatch(batch.Count);
                    break;
                }
            }
            finally
            {
                _isFlushing = false;
            }
        }

        internal async Task FlushOnceAsync()
        {
            if (_isFlushing) return;
            _isFlushing = true;

            try
            {
                if (_queue.Count == 0) return;

                var batch = _queue.PeekBatch(_config.MaxEventsPerBatch);
                if (batch.Count == 0) return;

                var payload = new AnalyticsPayload(
                    new AnalyticsContext(SdkVersion, _userId, _sessionId),
                    batch
                );

                var bytes = _serializer.Serialize(payload);
                var result = await _transport.SendAsync(bytes, _serializer.ContentType, default);

                if (result.IsSuccess)
                {
                    _queue.DropBatch(batch.Count);
                    _lastError = null;
                    _lastFlushTimeUtc = DateTimeOffset.UtcNow;
                    return;
                }

                _lastError = $"Flush failed ({result.StatusCode}) {result.Error}";

                if (result.IsRetryable)
                {
                    // Keep queue intact; caller can retry later (Day 8+: auto retry/backoff).
                    return;
                }

                // Fatal: drop the batch so the queue doesn’t get blocked forever.
                _queue.DropBatch(batch.Count);
            }
            finally
            {
                _isFlushing = false;
            }
        }

        /// <summary>
        /// Returns a snapshot of queue size and last flush status.
        /// </summary>
        /// <returns>Current analytics client stats.</returns>
        public AnalyticsStats GetStats()
        {
            return new AnalyticsStats(
                queuedEventCount: _queue.Count,
                lastFlushTimeUtc: _lastFlushTimeUtc,
                lastError: _lastError
            );
        }


        internal void Tick(float unscaledDeltaTime)
        {
            if (!_config.EnableAutoFlush) return;
            if (_config.FlushIntervalSeconds <= 0f) return;
            if (_queue.Count == 0) return;

            _flushTimer += unscaledDeltaTime;
            if (_flushTimer < _config.FlushIntervalSeconds) return;

            // reset timer (simple reset is fine; you can also subtract interval to reduce drift)
            _flushTimer = 0f;

            _ = FlushUpToAsync(_config.MaxBatchesPerFlush);
        }
    }
}
