---
id: ADR-20261006-additive-public-dto-fields
status: accepted
date: 2026-10-06
superseded-by:
scope: Stella.FeatureManagement.Dashboard/FeatureFlagDto.cs, public DTOs of the NuGet package
summary: Add new fields to public DTO records as init-only properties, not positional parameters, to stay binary-compatible
---
## Context
`FeatureFlagDto` is a public positional record shipped in the NuGet package and handed to consumer
code via `OnFeatureChanging(...)` validators, so its constructor signature is part of the public API.
Exposing `CreatedAt`/`UpdatedAt` required new fields.
- Rejected: new positional (even optional) parameters — changes the constructor signature, breaking
  binary compatibility for compiled consumers.
- Rejected: a separate response-only DTO — duplicates the shape and splits the API/validator contract.
- Accepted cost: record value equality (`Equals`/`GetHashCode`) now includes the added properties.

## Decision
Add new fields to public DTO records as nullable `{ get; init; }` properties in the record body, set
via object initializers (EF Core translates these inside `Select`), in the one shared mapping
(`FeatureFlagMapping`). Keep the positional parameter list unchanged.

Binds: future additions to `FeatureFlagDto` and other public DTOs consumed by package users.
