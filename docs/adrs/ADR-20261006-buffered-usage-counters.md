---
id: ADR-20261006-buffered-usage-counters
status: accepted
date: 2026-10-06
superseded-by:
scope: Stella.FeatureManagement.Dashboard/Services/FeatureUsageRecorder.cs, FeatureUsageFlushService.cs, features."FeatureFlagUsages"
summary: Count feature-state calls in memory per (feature, UTC day) and flush deltas periodically with an atomic additive upsert
---
## Context
`GET /features/{featureName}` must record daily enabled/disabled counts per feature (to chart usage)
without adding latency. It is hit concurrently, and the API runs as several instances on one database.
Losing unflushed counts on shutdown or crash is acceptable.
- Rejected: a DB write per request — adds a round-trip to the hot path and contends on one row per feature/day.
- Rejected: a `Channel` queue drained by a worker — more moving parts with the same loss semantics.
- Rejected: hooking every `IFeatureManager` evaluation — only the HTTP endpoint is in scope.

## Decision
On the request path, only increment a `ConcurrentDictionary<(name, day), counter>` with `Interlocked`.
A `BackgroundService` detaches the counters every `FeatureUsageOptions.FlushInterval` (default 30 s; the
first tick is jittered). It writes them in one statement:
`INSERT … SELECT … FROM unnest(...) JOIN FeatureFlags … ORDER BY Id, Date ON CONFLICT DO UPDATE SET count = count + EXCLUDED.count`.
- The additive upsert keeps instances independent: each writes only its own deltas.
- The `JOIN` drops unknown or deleted names.
- `ORDER BY` gives all instances the same lock order, which prevents deadlocks.
- A deadlock, serialization failure, or FK violation (feature deleted mid-flush) is retried once. Any other failure drops the batch with a warning.

Binds: never write usage on the request path; keep usage writes additive (never overwrite totals); keep
`FeatureFlagUsages` cascade-deleted with its feature.
