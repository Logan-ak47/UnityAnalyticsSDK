using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Ashutosh.AnalyticsSdk.Internal;
using Ashutosh.AnalyticsSdk.Internal.Logging;
using Ashutosh.AnalyticsSdk.Internal.Serialization;
using Ashutosh.AnalyticsSdk.Internal.Validation;
using Ashutosh.AnalyticsSdk.Internal.Retry;
using Ashutosh.AnalyticsSdk.Transports;

namespace Ashutosh.AnalyticsSdk
{
    /// <summary>
    /// Default analytics client implementation with:
    /// - validation/sanitization
    /// - queue (memory or disk)
    /// - deterministic JSON serialization
    /// - transport (UnityWebRequest by default)
    /// - batching + auto-flush
    /// - retry with exponential backoff + jitter (cooldown)
    /// </summary>
    public sealed class AnalyticsClient : IAnalyticsClient
    {
        private const string SdkVersion = "0.1.0";

        private readonly AnalyticsConfig _config;
        private readonly IEventQueue _queue;

        private readonly ITransport _transport;
        private readonly IEventSerializer _serializer;

        private readonly RetryPolicy _retryPolicy;

        private string _userId;
        private string _sessionId;

        private DateTimeOffset? _lastFlushTimeUtc;
        private string _lastError;

        private bool _isFlushing;

        // Auto flush
        private float _flushTimer;

        // Retry state
        private float _retryCooldownRemaining;
        private int _retryAttempt;

        /// <summary>
        /// Creates a client using the provided configuration and optional transport.
        /// </summary>
        public AnalyticsClient(AnalyticsConfig config, ITransport transport = null)
            : this(config, transport, null)
        {
        }

        /// <summary>
        /// Internal ctor used by tests (allows deterministic jitter via injected random).
        /// </summary>
        internal AnalyticsClient(AnalyticsConfig config, ITransport transport, IRandomSource random)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Logger
            SdkLogger.Enabled = _config.EnableLogging;

            // Queue: disk or memory
            var basePath = string.IsNullOrEmpty(_config.StoragePathOverride)
                ? Application.persistentDataPath
                : _config.StoragePathOverride;

            _queue = _config.EnableDiskPersistence
                ? new Ashutosh.AnalyticsSdk.Internal.Storage.DiskBackedEventQueue(basePath, _config.MaxDiskBytes)
                : new MemoryEventQueue();

            // Serializer + transport
            _serializer = new JsonEventSerializer();
            _transport = transport ?? new UnityWebRequestTransport(_config.EndpointUrl);

            // Retry policy
            var rng = random ?? new SystemRandomSource();
            _retryPolicy = new RetryPolicy(
                baseDelay: _config.RetryBaseDelaySeconds,
                maxDelay: _config.RetryMaxDelaySeconds,
                maxAttempts: _config.MaxRetryAttempts,
                random: rng
            );

            _retryAttempt = 0;
            _retryCooldownRemaining = 0f;

            // Auto flush runner registration
            if (_config.EnableAutoFlush && _config.FlushIntervalSeconds > 0f)
            {
                AnalyticsRuntime.Register(this);
            }
        }

        public void SetUserId(string userId) => _userId = userId;

        public void SetSessionId(string sessionId) => _sessionId = sessionId;

        public void Track(string eventName, IReadOnlyDictionary<string, object> properties = null)
        {
            if (!EventValidator.TrySanitize(eventName, properties, out var sanitizedName, out var sanitizedProps, out var error))
            {
                _lastError = $"Track dropped event: {error}";
                SdkLogger.Warn(_lastError);
                return;
            }

            // Timestamp is created inside AnalyticsEvent
            Track(new AnalyticsEvent(sanitizedName, sanitizedProps));
        }

        public void Track(AnalyticsEvent evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            if (!EventValidator.TrySanitize(evt.Name, evt.Properties, out var sanitizedName, out var sanitizedProps, out var error))
            {
                _lastError = $"Track dropped event: {error}";
                SdkLogger.Warn(_lastError);
                return;
            }

            // Preserve timestamp from provided event
            var sanitizedEvt = new AnalyticsEvent(sanitizedName, evt.Timestamp, sanitizedProps);
            _queue.Enqueue(sanitizedEvt);
        }

        /// <summary>
        /// Starts an asynchronous flush of queued events (may send multiple batches).
        /// </summary>
        public void Flush()
        {
            _ = FlushUpToAsync(_config.MaxBatchesPerFlush);
        }

        /// <summary>
        /// Tick is called from the SDK runtime runner (unscaled time).
        /// Decrements retry cooldown always, and triggers auto flush when enabled.
        /// </summary>
        internal void Tick(float unscaledDeltaTime)
        {
            // Day 9: cooldown must tick down regardless of auto-flush setting.
            if (_config.EnableRetry && _retryCooldownRemaining > 0f)
            {
                _retryCooldownRemaining -= unscaledDeltaTime;
                if (_retryCooldownRemaining < 0f) _retryCooldownRemaining = 0f;
            }

            // Auto flush scheduling
            if (!_config.EnableAutoFlush) return;
            if (_config.FlushIntervalSeconds <= 0f) return;
            if (_queue.Count == 0) return;

            _flushTimer += unscaledDeltaTime;
            if (_flushTimer < _config.FlushIntervalSeconds) return;

            // Reset timer (simple reset is fine)
            _flushTimer = 0f;

            _ = FlushUpToAsync(_config.MaxBatchesPerFlush);
        }

        /// <summary>
        /// Flushes up to N batches. Uses retry cooldown to avoid spamming on transient failures.
        /// </summary>
        internal async Task FlushUpToAsync(int maxBatches)
        {
            // Day 9: block flush attempts while retry cooldown is active.
            if (_config.EnableRetry && _retryCooldownRemaining > 0f)
                return;

            if (_isFlushing) return;
            _isFlushing = true;

            try
            {
                if (maxBatches <= 0) maxBatches = 1;

                int batchesSent = 0;

                while (_queue.Count > 0 && batchesSent < maxBatches)
                {
                    var batch = _queue.PeekBatch(_config.MaxEventsPerBatch);
                    if (batch.Count == 0) break;

                    var payload = new AnalyticsPayload(
                        new AnalyticsContext(SdkVersion, _userId, _sessionId),
                        batch
                    );

                    byte[] bytes = _serializer.Serialize(payload);
                    var result = await _transport.SendAsync(bytes, _serializer.ContentType, default);

                    batchesSent++;
                    _lastFlushTimeUtc = DateTimeOffset.UtcNow;

                    if (result.IsSuccess)
                    {
                        _queue.DropBatch(batch.Count);
                        _lastError = null;

                        // Reset retry state on success
                        _retryAttempt = 0;
                        _retryCooldownRemaining = 0f;

                        continue;
                    }

                    _lastError = $"Flush failed ({result.StatusCode}) {result.Error}";

                    if (result.IsRetryable)
                    {
                        if (_config.EnableRetry)
                        {
                            _retryAttempt++;

                            if (_retryAttempt > _retryPolicy.MaxAttempts)
                            {
                                // Give up to avoid permanent blockage.
                                _queue.DropBatch(batch.Count);
                                _retryAttempt = 0;
                                _retryCooldownRemaining = 0f;
                                break;
                            }

                            _retryCooldownRemaining = _retryPolicy.GetDelaySeconds(_retryAttempt);
                        }

                        // Keep queue intact; stop now. Next tick/manual flush retries after cooldown.
                        break;
                    }

                    // Fatal: drop the batch to avoid blocking forever, then reset retry state.
                    _queue.DropBatch(batch.Count);
                    _retryAttempt = 0;
                    _retryCooldownRemaining = 0f;
                    break;
                }
            }
            finally
            {
                _isFlushing = false;
            }
        }

        /// <summary>
        /// Convenience wrapper (keeps only one flush implementation).
        /// </summary>
        internal Task FlushOnceAsync() => FlushUpToAsync(1);

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