---
id: ADR-20261006-usage-read-api-and-index
status: accepted
date: 2026-10-06
superseded-by:
scope: Stella.FeatureManagement.Dashboard/EndPoints/GetUsageExtension.cs, features."FeatureFlagUsages", dashboard UI usage charts
summary: Serve usage from a separate /dashboardapi/usage endpoint loaded alongside the feature list, backed by a plain index on Date
---
## Context
The dashboard shows a 7-day usage sparkline per feature and a 30-day chart on demand. The feature
list must stay fast, and `FeatureFlagUsages` grows by features × days with no retention. Its PK
`(FeatureFlagId, Date)` cannot serve the all-features query `WHERE "Date" >= @from` as a range scan
(PostgreSQL 18 skip scan helps, but consumers may run older versions).
- Rejected: usage fields on `FeatureFlagDto` — couples the list's latency to usage and grows a public,
  consumer-visible DTO (ADR-20261006-additive-public-dto-fields) with response-only data.
- Rejected: one request per feature row — N round-trips for a list.
- Rejected: a covering index `INCLUDE (EnabledCount, DisabledCount)` — the flush updates counts every
  interval, and indexing them would make those updates non-HOT.

## Decision
Expose `GET /dashboardapi/usage?days=` (all features with usage in the window, one query) and
`GET /dashboardapi/usage/{featureName}?days=` (one feature, 404 if unknown), on their own route group
so they never shadow a feature name. `days` is clamped to 1..90, the window ends today (UTC), and
series are zero-filled, oldest first. The UI fetches usage in parallel with the feature list and
hides the sparklines if that fetch fails. Back the all-features query with a plain b-tree index on
`Date`; the single-feature query uses the PK.

Binds: keep usage out of `FeatureFlagDto` and the feature list query; keep indexes on
`FeatureFlagUsages` free of the counter columns.
