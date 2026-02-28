# Case Study — Unity Analytics SDK (UPM)

**Author:** Ashutosh Kale  
**Role:** Senior Unity Developer / Team Lead (Generalist)  
**Unity:** 6000.3 (Unity 6.3 LTS)  
**Repo:** https://github.com/Logan-ak47/UnityAnalyticsSDK.git
**Demo scene:** `Assets/DemoAnalyticsSDK/Scenes/AnalyticsDemo.unity`

---

## Why I built this

In many Unity projects, analytics starts simple (“just send a JSON event”), but over time it becomes messy:

- event payloads become inconsistent (random keys/types)
- offline support is missing (events lost on bad network / app restart)
- failures cause request spam (or silent drops)
- debugging is hard (payload order changes, no clear semantics)
- testing is painful (logic scattered across gameplay code)

This project is a **small SDK-style package** that shows how I approach “platform-ish” Unity work: stable APIs, predictable behavior, clear failure semantics, and reviewability.

---

## Goals

**Engineering goals**
- Keep the public API **small and stable**
- Validate/sanitize event inputs so gameplay code can call `Track()` safely
- Ensure payloads are **deterministic** (diffable + testable)
- Support batching + timed flush to reduce request overhead
- Handle offline and transient failures with persistence + retries/backoff
- Keep it easy to evaluate (one-click demo + tests)

**Constraints**
- No fancy art; demo uses simple UI Toolkit UI
- No exaggerated claims; focus on correctness and maintainability
- Minimal dependencies (no external JSON packages)

---

## What it does (high level)

You call:

- `Track(eventName, properties)`
- optional `Flush()`

The SDK:
1. **Sanitizes** event name + properties (types/limits/nesting)
2. **Queues** events (memory or disk)
3. On flush or auto-flush tick:
   - batches events
   - serializes deterministic JSON
   - sends via transport
   - applies clear failure semantics:
     - success → drop batch
     - retryable fail → keep batch + backoff cooldown
     - fatal fail → drop batch (avoid permanent blockage)
     - give up after max attempts → drop batch

---

## Public API

The public surface is intentionally small:

- `AnalyticsClient / IAnalyticsClient`
  - `Track(string eventName, IReadOnlyDictionary<string, object> props = null)`
  - `Track(AnalyticsEvent evt)`
  - `Flush()`
  - `GetStats()`
  - `SetUserId()`, `SetSessionId()`

This keeps integration trivial for game teams and keeps implementation details internal.

---

## Architecture


Track()
-> EventValidator (sanitize types/limits)
-> Queue (MemoryEventQueue OR DiskBackedEventQueue)

Flush / AutoFlush Tick
-> Batch selection (MaxEventsPerBatch, MaxBatchesPerFlush)
-> JsonEventSerializer (deterministic JSON)
-> ITransport (UnityWebRequestTransport / MockTransport / simulated demo transport)
-> Retry policy (cooldown with exponential backoff + jitter)


---

## Key decisions (and tradeoffs)

### 1) Deterministic JSON (custom writer)
**Why:** Analytics properties are `Dictionary<string, object>`. Unity’s `JsonUtility` is great for typed objects but not for dictionary-heavy payloads. Also, deterministic output is hugely useful:
- stable tests
- easy diffs while debugging
- less “it changed but I don’t know why”

**Tradeoff:** maintaining a small JSON writer + formatting rules.

### 2) Strict sanitization rules for event properties
**Why:** Payloads should be safe to serialize and stable across teams. Unity objects, random custom classes, and deep graphs often leak into telemetry accidentally.

**Behavior:**
- unsupported property values are dropped
- strings/keys/nesting are capped
- numeric types are normalized

**Tradeoff:** You sometimes need to convert values manually (e.g., Vector3 → `{x,y,z}`) instead of sending objects directly.

### 3) Retry/backoff with jitter + cooldown
**Why:** During outages you don’t want to spam requests every frame/flush call.
- retryable failures set a cooldown
- cooldown blocks further send attempts until it expires
- exponential growth reduces pressure; jitter prevents synchronized retries

**Tradeoff:** delivery is delayed under bad network. After max attempts, the SDK drops the batch to avoid blocking forever (a deliberate “fail-safe” choice).

### 4) Disk queue snapshot approach (portfolio-friendly reliability)
**Why:** Persistence across restarts is the “real” offline story. The simplest robust approach here is to write queue snapshots on changes.

**Tradeoff:** snapshot saves more often than an append-only log. For this scope, clarity + testability wins.

---

## Demo (how to review in < 2 minutes)

1. Open scene: `Assets/DemoAnalyticsSDK/Scenes/AnalyticsDemo.unity`
2. Press Play
3. Click Track buttons → queue count increases
4. Click Flush → sends and drains queue
5. Toggle **Simulate Offline**:
   - flush fails retryably (queue stays)
   - repeated flush clicks won’t spam sends (cooldown blocks)
   - toggle back online → queue drains after cooldown

> The demo uses a mock/simulated transport so reviewers don’t need a real endpoint.

---

## Testing approach

I treated this like an SDK, so I locked down behavior with automated tests.

Covered areas:
- deterministic serialization (same input → same JSON)
- flush semantics (success drops / retryable keeps / fatal drops)
- batching limits (max events per batch; max batches per flush)
- disk persistence (persist → restart → still queued)
- retry cooldown blocking (no spam) + retry after cooldown
- edge cases (corrupt queue file recovery, max disk cap, stop-on-retryable)

**Test count:** 21
(You can grab this from Unity Test Runner.)

---

## What I’d change if this were production (optional, honest notes)

- Switch disk storage to an append-only log + compaction for high-throughput games
- Add payload compression (gzip) behind a feature flag
- Add event sampling and priority levels
- Add richer context fields (app version, platform, device, session start/end)

---

## Summary

This project demonstrates how I build Unity SDK/tools-style systems:
- stable public API
- strict input contracts
- deterministic outputs
- clear failure semantics
- real offline story (persistence + retry)
- tests + demo that make it easy to evaluate quickly