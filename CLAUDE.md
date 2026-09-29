# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```powershell
dotnet restore .\Stella.FeatureManagement.Dashboard.slnx
dotnet build .\Stella.FeatureManagement.Dashboard.slnx

# Full test suite (requires a running container runtime — tests use Testcontainers PostgreSQL)
dotnet test .\Stella.FeatureManagement.Dashboard.Tests\Stella.FeatureManagement.Dashboard.Tests.csproj

# Single test
dotnet test .\Stella.FeatureManagement.Dashboard.Tests\Stella.FeatureManagement.Dashboard.Tests.csproj --filter "FullyQualifiedName~Stella.FeatureManagement.Dashboard.Tests.GetFeaturesEndPointTests.WhenGetDashboardReturnsHtml"

# Rebuild the embedded React dashboard after any UI change (from Stella.FeatureManagement.Dashboard.UI)
npm run build:dev
# CI/release UI build; expects `dotnet gitversion` to be available
npm run build

# Local Aspire environment (PostgreSQL + Example.Api + Vite app)
dotnet run --project .\Example.AppHost\Example.AppHost.csproj
```

There is no separate lint step. Analyzer/code-style checks run during `dotnet build` (`EnforceCodeStyleInBuild=true`, `AnalysisMode=Recommended`); TypeScript checking runs as part of `npm run build:dev`.

## Architecture

- **`Stella.FeatureManagement.Dashboard`** — the NuGet package. Provides EF Core/PostgreSQL persistence, a `Microsoft.FeatureManagement` definition provider, Minimal API endpoints, and the embedded static web UI.
- **`ServiceCollectionExtensions.AddFeaturesDashboard(...)`** is the registration entry point. It builds a `FeatureManagerDashboardBuilder` that wires `FeatureFlagDbContext`, `DatabaseFeatureDefinitionProvider`, `DashboardInitializer`, `FeatureChangeValidation`, `ManagedFeatureRegistration`, and the in-memory `FeatureFilterRepository`.
- **`EndpointRouteBuilderExtensions.UseFeaturesDashboard(...)`** is the runtime entry point; route groups are composed in `FeatureManagerDashboardAppBuilder.UseFeaturesDashboard`:
  - `/features/{featureName}` — feature state via `Microsoft.FeatureManagement` (optional CORS and rate-limiting policy apply here)
  - `/features/dashboard` — the embedded React SPA
  - `/features/dashboardapi/features`, `/filters`, `/applications` — dashboard CRUD/support APIs
- **`DatabaseFeatureDefinitionProvider`** bridges DB → `Microsoft.FeatureManagement`: disabled features become `EnabledFor = []`; enabled with no filters become `AlwaysOn`; enabled with filters are converted from stored JSON into `FeatureFilterConfiguration`.
- **Feature evaluation context**: `GetFeaturesFromFeatureManager` copies the request's query string into `HttpContext.Items`, exposed to filters through the singleton `IFeatureEvaluationRequestContext` (case-insensitive dictionary). This is how custom filters receive per-request parameters.
- **`FeatureFlagDbContext`** uses PostgreSQL schema `features`. `FeatureFlag` owns `FeatureFilter` rows (cascade delete); `Application` defaults to `"Default"`. Migrations live in `Data/Migrations`.
- **`Stella.FeatureManagement.Dashboard.UI`** is a standalone React/Vite app whose build output goes directly into `Stella.FeatureManagement.Dashboard\wwwroot`, which the library embeds as resources and serves via `StaticDashboardExtensions`. If you change the UI, rebuild it or the packaged assets go stale.
- **`Example.Api`** is the sample/integration host; **`Example.AppHost`** is the Aspire orchestrator that starts PostgreSQL, the API, and the Vite app, injecting `VITE_API_URL` into the frontend.
- **`Stella.FeatureManagement.Dashboard.Tests`** are end-to-end tests that boot `Example.Api` via `WebApplicationFactory<Program>` and swap the DB for a PostgreSQL Testcontainer, asserting with Shouldly. Prefer extending these for changes touching endpoints, persistence, or feature evaluation.

## Conventions

- Use `IDbContextFactory<FeatureFlagDbContext>` in endpoints and services — never inject a scoped `FeatureFlagDbContext` directly into request handlers.
- Dashboard endpoints are Minimal API extension classes in `EndPoints\*Extension.cs`; new routes follow that pattern and are wired in `FeatureManagerDashboardAppBuilder.UseFeaturesDashboard`.
- UI/API path contract is `/features/dashboardapi/...`. The React app uses relative paths like `../dashboardapi/features` unless `VITE_API_URL` is set by Aspire.
- Consumer startup order (see `Example.Api/Program.cs`): `AddFeaturesDashboard(...)` → `UseFeaturesDashboard(...)` → `MigrateFeaturesDatabaseAsync()` → `RegisterManagedFeaturesAsync(...)`.
- Custom filters must be added via `.AddFeatureFilter<T>(defaultSettings)`, which both registers the filter with `Microsoft.FeatureManagement` and adds its metadata/default JSON to `FeatureFilterRepository` (what the UI uses to offer editable filters). Display names come from `FilterAliasAttribute`, else the CLR type name.
- Filter JSON validation is centralized in `FeatureChangeValidation`, which validates stored parameters against the registered settings type with `JsonUnmappedMemberHandling.Disallow`. Handle filter-shape changes there, not in the React app. Consumers can add extra validation via `OnFeatureChanging(...)`.
- Managed features are tracked in-memory by `ManagedFeatureRegistration` so startup registration is idempotent — preserve that when changing seeding behavior.
- The library packs `README.md`, `LICENSE`, symbols, and embedded `wwwroot`. Packaging/publishing is defined in `.github\workflows\ci.yml` and `.github\workflows\publish.yml` (versioning via GitVersion).
