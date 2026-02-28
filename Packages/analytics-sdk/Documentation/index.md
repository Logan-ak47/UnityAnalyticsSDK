# Ashutosh Analytics SDK

Lightweight analytics SDK for Unity with event tracking, batching, deterministic JSON serialization, offline persistence, and retry/backoff.

## Package Info

- Package name: `com.ashutosh.analytics-sdk`
- Display name: `Ashutosh Analytics SDK`
- Version: `0.1.0`
- Unity: `6000.3+`
- Namespace: `Ashutosh.AnalyticsSdk`
- License: MIT

## Features

- Track analytics events with typed properties
- Built-in validation and sanitization (name/keys/values)
- Queue + batching (`MaxEventsPerBatch`, `MaxBatchesPerFlush`)
- Deterministic JSON payload serialization
- Pluggable transports (`UnityWebRequestTransport`, `MockTransport`, or custom `ITransport`)
- Manual flush and optional timed auto-flush
- Retry with exponential backoff + full jitter cooldown
- Optional disk-backed queue for offline recovery across restarts
- Runtime stats (`QueuedEventCount`, `LastFlushTimeUtc`, `LastError`)

## Installation

Install as a Unity Package (UPM):

1. Put this package under your project `Packages/` folder as `analytics-sdk`.
2. Ensure your `manifest.json` references it (local or git-based reference).
3. Open Unity and let package import complete.

## Quick Start

```csharp
using System.Collections.Generic;
using Ashutosh.AnalyticsSdk;

public class AnalyticsBootstrap
{
    private IAnalyticsClient _client;

    public void Init()
    {
        var config = new AnalyticsConfig(
            endpointUrl: "https://your-ingest-endpoint.example",
            maxEventsPerBatch: 25,
            maxBatchesPerFlush: 4,
            flushIntervalSeconds: 5f,
            enableAutoFlush: true,
            enableLogging: true,
            enableDiskPersistence: true,
            maxDiskBytes: 5_000_000,
            storagePathOverride: null,
            enableRetry: true,
            maxRetryAttempts: 5,
            retryBaseDelaySeconds: 1f,
            retryMaxDelaySeconds: 30f
        );

        _client = new AnalyticsClient(config);

        _client.SetUserId("user_123");
        _client.SetSessionId("session_abc");
    }

    public void TrackExample()
    {
        _client.Track("level_start", new Dictionary<string, object>
        {
            { "level", 1 },
            { "mode", "normal" },
            { "is_tutorial", true }
        });
    }

    public void FlushNow()
    {
        _client.Flush();
    }
}
```

## Public API

### `IAnalyticsClient`

- `SetUserId(string userId)`
- `SetSessionId(string sessionId)`
- `Track(string eventName, IReadOnlyDictionary<string, object> properties = null)`
- `Track(AnalyticsEvent evt)`
- `Flush()`
- `GetStats()`

### `AnalyticsEvent`

Represents one queued event:

- `Name`
- `Timestamp` (UTC)
- `Properties`

### `AnalyticsStats`

Snapshot of client state:

- `QueuedEventCount`
- `LastFlushTimeUtc`
- `LastError`

## Configuration Reference (`AnalyticsConfig`)

| Field | Default | Description |
|---|---:|---|
| `EndpointUrl` | required | Analytics ingestion endpoint URL |
| `MaxEventsPerBatch` | `25` | Max events per HTTP payload |
| `MaxBatchesPerFlush` | `4` | Max batches processed in one flush cycle |
| `FlushIntervalSeconds` | `5` | Auto-flush interval |
| `EnableAutoFlush` | `true` | Enables runtime tick-based periodic flush |
| `EnableLogging` | `true` | Logs dropped events / failures |
| `EnableDiskPersistence` | `true` | Uses disk-backed queue instead of memory-only queue |
| `MaxDiskBytes` | `5_000_000` | Disk queue file size cap |
| `StoragePathOverride` | `null` | Optional base path override for disk queue |
| `EnableRetry` | `true` | Enables retry cooldown/backoff on retryable failures |
| `MaxRetryAttempts` | `5` | Max retry attempts before giving up and dropping batch |
| `RetryBaseDelaySeconds` | `1` | Base delay for exponential backoff |
| `RetryMaxDelaySeconds` | `30` | Upper delay cap |

## Event Validation & Sanitization

All events are sanitized before enqueue.

Limits:

- Event name max length: `64`
- Property key max length: `64`
- Max properties per event: `50`
- String value max length: `256`
- Max nested depth: `3`

Supported property value types:

- `string`, `bool`
- `int`, `long` (normalized to `long`)
- `float`, `double`, `decimal` (normalized to `double`)
- `DateTime`, `DateTimeOffset` (ISO-8601 string)
- Nested dictionaries/lists/arrays (within depth limit)

Dropped/unsupported values:

- `null`
- Custom unsupported objects (for example `UnityEngine.Object`, custom classes/structs)

If event name is empty/invalid, the event is dropped and `LastError` is updated.

## Flush and Delivery Semantics

`Flush()` starts async sending in batches.

For each batch result:

- Success: batch is dropped from queue
- Retryable failure: batch is kept, flush stops, retry cooldown starts (if retry enabled)
- Fatal failure: batch is dropped to prevent queue blockage

Additional behavior:

- If cooldown is active, flush attempts are skipped
- Retry state resets after success or fatal drop
- Flush is guarded to avoid concurrent overlapping send loops

## Retry/Backoff Model

When retry is enabled and a retryable send fails:

- Attempt counter increases
- Delay is computed as exponential backoff with full jitter:
  - `cap = min(baseDelay * 2^(attempt-1), maxDelay)`
  - `delay = random(0..cap)`
- Client waits for cooldown expiry before next retry attempt
- If attempts exceed `MaxRetryAttempts`, the blocked batch is dropped

## Auto Flush Runtime

When `EnableAutoFlush = true` and interval is positive:

- SDK registers client into an internal runtime runner
- Runner ticks every frame with `Time.unscaledDeltaTime`
- Flush triggers when accumulated interval is reached and queue is non-empty

## Disk Persistence

When `EnableDiskPersistence = true`:

- Queue is persisted under:
  - `<basePath>/AshutoshAnalyticsSdk/event_queue.dat`
- Base path is:
  - `StoragePathOverride` if provided
  - otherwise `Application.persistentDataPath`
- Queue state is updated on enqueue/drop
- Corrupt queue files are handled by safe reset (no crash)
- If serialized snapshot exceeds `MaxDiskBytes`, temp snapshot is discarded

## Transport Layer

### Built-in

- `UnityWebRequestTransport` (default)
  - Sends `POST` with `application/json`
  - Maps results:
    - Success: HTTP success
    - Retryable: connection/data processing errors, HTTP `429`, HTTP `5xx`
    - Fatal: other protocol errors (typically non-retryable `4xx`)

- `MockTransport`
  - Useful for tests and offline demos
  - Returns scripted `TransportResult` sequence

### Custom Transport

Implement `ITransport`:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Ashutosh.AnalyticsSdk.Transports;

public sealed class MyTransport : ITransport
{
    public Task<TransportResult> SendAsync(byte[] payload, string contentType, CancellationToken ct)
    {
        // Send payload to your backend here
        return Task.FromResult(TransportResult.Success(200));
    }
}
```

## Payload Shape Example

```json
{
  "sdkVersion": "0.1.0",
  "userId": "user_123",
  "sessionId": "session_abc",
  "events": [
    {
      "name": "level_start",
      "ts": "2026-02-19T12:00:00.0000000+00:00",
      "props": {
        "level": 1,
        "mode": "normal"
      }
    }
  ]
}
```

## Demo Sample

![Analytics SDK demo](documentation/Media/UnityAnalyticsDemo.gif)

Sample included:

- `Samples~/Demo`

After importing samples in Unity Package Manager, open:

- `Packages/com.ashutosh.analytics-sdk/Samples~/Demo/Scenes/AnalyticsDemo.unity`

The demo uses `MockTransport` so behavior can be verified offline.

## Troubleshooting

- Events not sending:
  - Verify `EndpointUrl`
  - Call `GetStats()` and check `LastError`
  - If retry cooldown is active, next send waits for cooldown

- Queue keeps growing:
  - Backend may be returning retryable errors repeatedly
  - Inspect transport mapping and server responses
  - Tune retry and batch config

- Disk queue not persisting as expected:
  - Ensure `EnableDiskPersistence = true`
  - Check writable storage path and `MaxDiskBytes`

## Notes

- `Flush()` is fire-and-forget; it starts async work and returns immediately.
- Payload serialization is deterministic (stable key ordering), useful for testing and reproducibility.
