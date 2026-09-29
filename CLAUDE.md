# Feature Management Dashboard

## How to Behave

1. When a step doesn't need my input, keep going. Put status notes in the same message as your next action. Stop and ask only when you can't continue without me, or before anything destructive: deleting data, force-pushing, or changing anything outside this repository.
2. Once you have answered something, treat it as done. Don't revisit an earlier answer unless I ask or point out a problem with it.

## Project Structure

- **`Stella.FeatureManagement.Dashboard`** — the NuGet package: EF Core/PostgreSQL persistence, a `Microsoft.FeatureManagement` definition provider, Minimal API endpoints (`EndPoints\*Extension.cs`), and the embedded web UI (`wwwroot`). Packs `README.md`, `LICENSE`, symbols, and `wwwroot`; packaging/publishing lives in `.github\workflows\ci.yml` and `publish.yml` (versioned by GitVersion).
- **`Stella.FeatureManagement.Dashboard.UI`** — standalone React/Vite app. Its build output goes straight into `Stella.FeatureManagement.Dashboard\wwwroot`, which the library embeds and serves via `StaticDashboardExtensions`. After any UI change, rebuild or the packaged assets go stale.
- **`Example.Api`** — sample/integration host and the reference for consumer setup.
- **`Example.AppHost`** — Aspire orchestrator: starts PostgreSQL, the API, and the Vite app, injecting `VITE_API_URL` into the frontend. (`Example.ServiceDefaults` holds its shared Aspire defaults.)
- **`Stella.FeatureManagement.Dashboard.Tests`** — end-to-end tests: boot `Example.Api` via `WebApplicationFactory<Program>`, swap the DB for a PostgreSQL Testcontainer, assert with Shouldly. Prefer extending these for changes touching endpoints, persistence, or feature evaluation.
- **`docs/`** — decisions, specs, plans, durable facts, and lessons (`adrs/`, `specs/`, `plans/`, `memories/`, `learnings/`). Start at `docs/README.md`; each folder's `README.md` is its index and defines how to write records.

## Commands

```powershell
dotnet restore .\Stella.FeatureManagement.Dashboard.slnx
dotnet build .\Stella.FeatureManagement.Dashboard.slnx

# Full test suite (needs a running container runtime — Testcontainers PostgreSQL)
dotnet test .\Stella.FeatureManagement.Dashboard.Tests\Stella.FeatureManagement.Dashboard.Tests.csproj

# Single test
dotnet test .\Stella.FeatureManagement.Dashboard.Tests\Stella.FeatureManagement.Dashboard.Tests.csproj --filter "FullyQualifiedName~Stella.FeatureManagement.Dashboard.Tests.GetFeaturesEndPointTests.WhenGetDashboardReturnsHtml"

# Rebuild the embedded dashboard (from Stella.FeatureManagement.Dashboard.UI)
npm run build:dev
npm run build        # CI/release; expects `dotnet gitversion`

# Local Aspire environment
dotnet run --project .\Example.AppHost\Example.AppHost.csproj
```

No separate lint step: analyzer/style checks run in `dotnet build` (`EnforceCodeStyleInBuild=true`, `AnalysisMode=Recommended`); TypeScript checking runs in `npm run build:dev`.

## Architecture

- **Registration**: `ServiceCollectionExtensions.AddFeaturesDashboard(...)` builds a `FeatureManagerDashboardBuilder` that wires `FeatureFlagDbContext`, `DatabaseFeatureDefinitionProvider`, `DashboardInitializer`, `FeatureChangeValidation`, `ManagedFeatureRegistration`, and the in-memory `FeatureFilterRepository`.
- **Routing**: `EndpointRouteBuilderExtensions.UseFeaturesDashboard(...)` is the runtime entry point; route groups are composed in `FeatureManagerDashboardAppBuilder.UseFeaturesDashboard`:
  - `/features/{featureName}` — feature state via `Microsoft.FeatureManagement` (optional CORS and rate-limiting policy)
  - `/features/dashboard` — the embedded React SPA
  - `/features/dashboardapi/features`, `/filters`, `/applications` — dashboard CRUD/support APIs
- **DB → FeatureManagement**: `DatabaseFeatureDefinitionProvider` maps disabled features to `EnabledFor = []`, enabled without filters to `AlwaysOn`, and enabled with filters to `FeatureFilterConfiguration` built from stored JSON.
- **Evaluation context**: `GetFeaturesFromFeatureManager` copies the request query string into `HttpContext.Items`, exposed to filters via the singleton `IFeatureEvaluationRequestContext` (case-insensitive). This is how custom filters get per-request parameters.
- **Data**: `FeatureFlagDbContext` uses PostgreSQL schema `features`. `FeatureFlag` owns `FeatureFilter` rows (cascade delete); `Application` defaults to `"Default"`. Migrations are in `Data/Migrations`.

## Constraints

- Use `IDbContextFactory<FeatureFlagDbContext>` in endpoints and services — never inject a scoped `FeatureFlagDbContext` into request handlers.
- New endpoints are Minimal API extension classes in `EndPoints\*Extension.cs`, wired in `FeatureManagerDashboardAppBuilder.UseFeaturesDashboard`.
- UI/API path contract is `/features/dashboardapi/...`. The React app uses relative paths (`../dashboardapi/features`) unless Aspire sets `VITE_API_URL`.
- Consumer startup order (see `Example.Api/Program.cs`): `AddFeaturesDashboard(...)` → `UseFeaturesDashboard(...)` → `MigrateFeaturesDatabaseAsync()` → `RegisterManagedFeaturesAsync(...)`.
- Add custom filters only via `.AddFeatureFilter<T>(defaultSettings)`: it registers the filter with `Microsoft.FeatureManagement` and adds its metadata/default JSON to `FeatureFilterRepository` (what the UI offers for editing). Display name comes from `FilterAliasAttribute`, else the CLR type name.
- Filter JSON validation is centralized in `FeatureChangeValidation` (validates against the registered settings type with `JsonUnmappedMemberHandling.Disallow`). Handle filter-shape changes there, not in the React app. Consumers can add validation via `OnFeatureChanging(...)`.
- `ManagedFeatureRegistration` tracks managed features in memory so startup registration is idempotent — preserve that when changing seeding.
