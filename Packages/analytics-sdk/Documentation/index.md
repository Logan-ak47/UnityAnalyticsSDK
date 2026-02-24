# Ashutosh Analytics SDK

This package provides a small, SDK-style analytics client for Unity:

- `Track()` events with properties (validated & sanitized)
- Queue + batching
- Deterministic JSON serialization
- Send via pluggable transports (Mock + UnityWebRequest)
- Manual flush + timed auto-flush
- Lightweight stats for debugging and demo UI

---

## Key Concepts

### Public API
- `AnalyticsClient` / `IAnalyticsClient`: main entry point
- `AnalyticsConfig`: configuration (endpoint, batching, auto-flush)
- `AnalyticsEvent`: event model
- `AnalyticsStats`: queue count / last flush / last error (debug-friendly)

Typical flow:

1. Create `AnalyticsClient(config)`
2. Set `UserId` / `SessionId` (optional)
3. Call `Track(...)`
4. Let auto-flush run, or call `Flush()`

---

## Architecture Overview

```
Track()
  -> EventValidator (sanitize types/limits)
  -> EventQueue (in-memory)
Flush / AutoFlush Tick
  -> Batch (MaxEventsPerBatch, MaxBatchesPerFlush)
  -> JsonEventSerializer (deterministic JSON)
  -> ITransport (MockTransport / UnityWebRequestTransport)
```

### Failure semantics (send results)
- **Success**: drop sent batch from queue
- **Retryable failure**: keep batch in queue (will send again on later flush/tick)
- **Fatal failure**: drop batch to avoid blocking the queue forever

---

## Event property sanitization

Event properties support primitives and structured data that can be represented safely in JSON:

**Supported**
- `string`, `bool`
- numbers (`int/long` → long, `float/double/decimal` → double)
- `DateTime` / `DateTimeOffset` → ISO-8601 string
- nested dictionaries and arrays/lists (up to a small max depth)

**Dropped**
- Unity objects (`UnityEngine.Object`)
- custom classes/structs (unless converted to primitives)

Current limits (defaults):
- event name length: 64
- key length: 64
- properties per event: 50
- string length: 256
- nesting depth: 3

---

## Payload format (example)

```json
{
  "sdkVersion": "0.1.0",
  "userId": "user_123",
  "sessionId": "session_abc",
  "events": [
    {
      "name": "level_start",
      "ts": "2026-02-19T12:00:00.0000000+00:00",
      "props": { "level": 1, "mode": "normal" }
    }
  ]
}
```

---

## Demo

If your repo includes the demo scene, open:

- `Assets/DemoAnalyticsSDK/Scenes/AnalyticsDemo.unity`

The demo uses `MockTransport` so it runs offline and still demonstrates tracking, queueing, batching, and flushing.
