---
id: LRN-20261006-ef-migrations-startup-project
date: 2026-10-06
obsolete:
summary: Add EF migrations with the library as its own startup project (DesignTimeDbContextFactory), not Example.Api
---
## Lesson
Generate migrations from the repo root with the library as both project and startup project:
`dotnet ef migrations add <Name> --project Stella.FeatureManagement.Dashboard --context FeatureFlagDbContext --output-dir Data/Migrations`.
`Data/DesignTimeDbContextFactory.cs` supplies the context at design time, so no database or host is needed.

## What happened
`--startup-project Example.Api` (the obvious host) failed with "Your startup project 'Example.Api' doesn't
reference Microsoft.EntityFrameworkCore.Design". Only the library references the Design package.
