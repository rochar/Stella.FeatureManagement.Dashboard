---
id: ADR-20261014-optimistic-concurrency-writes
status: accepted
date: 2026-10-14
superseded-by:
scope: Stella.FeatureManagement.Dashboard/Services/FeatureFlagUpdater.cs, IFeatureFlagStore.cs, EndPoints/PutFeaturesExtension.cs, EndPoints/DeleteFeaturesExtension.cs, Data/FeatureFlagDbContext.cs
summary: Feature updates go through one FeatureFlagUpdater with UpdatedAt as the EF concurrency token; equal state wins over a stale token
---
## Context
Consumers need to change stored features from code (for example, add one id to a filter) without
overwriting a concurrent dashboard edit, and a dashboard tab open on an old copy could silently replace
newer state. Every write already sets `UpdatedAt`, so it can serve as the token.
- Rejected: a dedicated version column or Postgres `xmin` — needs a migration and a new DTO field.
- Rejected: public positional records for the update input — breaks binary compatibility
  (ADR-20261006-additive-public-dto-fields); the store takes the existing `FeatureFlagDto`.
- Rejected: requiring the token on the dashboard PUT now — breaks other callers; it stays optional.
- Accepted: a stale token whose requested state equals the stored state returns `Unchanged`, not
  `Conflict`. The caller's state is already stored, so nothing is lost, and concurrent writers that plan
  the same state do not need to retry.

## Decision
Mark `FeatureFlag.UpdatedAt` as an EF concurrency token (no migration). Route every update of an existing
feature — the dashboard PUT and the public `IFeatureFlagStore.UpdateFeatureAsync` — through the internal
`FeatureFlagUpdater`, which validates (as `Update`), compares the requested state field by field (filters
as an ordered sequence, ordinal) before applying the token, and saves once. A write sets `UpdatedAt` to
the later of now and the first microsecond after the expected value, so the token always moves forward.
`DbUpdateConcurrencyException` becomes `Conflict` (409 on PUT and DELETE), never a 500. Filters are read
in `Id` order wherever state is compared or returned.

Binds: writes to existing features go through `FeatureFlagUpdater`; `UpdatedAt` stays the concurrency
token and only ever moves forward; the state check precedes the token check.
