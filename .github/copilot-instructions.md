# Copilot instructions for this repository

## Build, test, and local run commands

- Restore/build the solution with:
  - `dotnet restore .\Stella.FeatureManagement.Dashboard.slnx`
  - `dotnet build .\Stella.FeatureManagement.Dashboard.slnx`
- Run the full test suite with:
  - `dotnet test .\Stella.FeatureManagement.Dashboard.Tests\Stella.FeatureManagement.Dashboard.Tests.csproj`
- Run a single test with a filter, for example:
  - `dotnet test .\Stella.FeatureManagement.Dashboard.Tests\Stella.FeatureManagement.Dashboard.Tests.csproj --filter "FullyQualifiedName~Stella.FeatureManagement.Dashboard.Tests.GetFeaturesEndPointTests.WhenGetDashboardReturnsHtml"`
- Rebuild the React dashboard assets after UI changes:
  - `Set-Location .\Stella.FeatureManagement.Dashboard.UI`
  - `npm run build:dev`
- The CI/release UI build uses:
  - `npm run build`
  - This script expects `dotnet gitversion` to be available; GitHub Actions installs GitVersion separately.
- Run the local Aspire sample environment with:
  - `dotnet run --project .\Example.AppHost\Example.AppHost.csproj`

There is no dedicated lint script in this repo. .NET analyzer/code-style checks run during `dotnet build` because the C# projects set `EnforceCodeStyleInBuild=true` and `AnalysisMode=Recommended`. The UI gets TypeScript checking as part of `npm run build:dev`.

The integration tests use Testcontainers PostgreSQL, so a working container runtime is required for `dotnet test`.

## High-level architecture

- `Stella.FeatureManagement.Dashboard` is the reusable NuGet package. It provides the EF Core/PostgreSQL persistence layer, the `Microsoft.FeatureManagement` provider, the dashboard Minimal APIs, and the embedded static web UI.
- `ServiceCollectionExtensions.AddFeaturesDashboard(...)` is the main registration entry point. It builds a `FeatureManagerDashboardBuilder`, which wires `FeatureFlagDbContext`, `DatabaseFeatureDefinitionProvider`, `DashboardInitializer`, `FeatureChangeValidation`, `ManagedFeatureRegistration`, and the in-memory `FeatureFilterRepository`.
- `EndpointRouteBuilderExtensions.UseFeaturesDashboard(...)` is the runtime entry point. It maps:
  - `/features` for feature-state access through `Microsoft.FeatureManagement`
  - `/features/dashboard` for the embedded React SPA
  - `/features/dashboardapi/features`, `/filters`, and `/applications` for dashboard CRUD/support APIs
- `DatabaseFeatureDefinitionProvider` is the bridge between the database and `Microsoft.FeatureManagement`:
  - disabled features become `EnabledFor = []`
  - enabled features with no filters become `AlwaysOn`
  - enabled features with filters are converted from stored JSON into `FeatureFilterConfiguration`
- `FeatureFlagDbContext` uses PostgreSQL schema `features`. `FeatureFlag` owns `FeatureFilter` rows with cascade delete and defaults `Application` to `"Default"`.
- `Stella.FeatureManagement.Dashboard.UI` is a standalone React/Vite app, but its build output goes directly to `Stella.FeatureManagement.Dashboard\wwwroot`. The library project embeds that folder as resources and serves it through `StaticDashboardExtensions`.
- `Example.Api` is the sample/integration host for the library. `Example.AppHost` is the Aspire orchestration layer that starts PostgreSQL, the sample API, and the Vite app and injects `VITE_API_URL` into the frontend.
- `Stella.FeatureManagement.Dashboard.Tests` are integration tests that boot `Example.Api` via `WebApplicationFactory<Program>` and replace the database factory with a PostgreSQL Testcontainer.

## Key repository conventions

- Use `IDbContextFactory<FeatureFlagDbContext>` in endpoints and services. This library does not use a scoped `FeatureFlagDbContext` directly in request handlers.
- Dashboard endpoints are organized as Minimal API extension classes in `Stella.FeatureManagement.Dashboard\EndPoints\*Extension.cs`. New route behavior should follow that pattern and be composed through `FeatureManagerDashboardAppBuilder.UseFeaturesDashboard`.
- The dashboard UI/API path contract is `/features/dashboardapi/...`. The React app expects relative paths such as `../dashboardapi/features` unless `VITE_API_URL` is provided by Aspire.
- If you change the React app, rebuild `Stella.FeatureManagement.Dashboard.UI`; otherwise the embedded `wwwroot` assets in the package will be stale.
- Consumer startup order matters and the sample app shows the intended sequence:
  - register services with `AddFeaturesDashboard(...)`
  - map endpoints with `UseFeaturesDashboard(...)`
  - run `MigrateFeaturesDatabaseAsync()`
  - register initial managed features with `RegisterManagedFeaturesAsync(...)`
- Custom feature filters must be added through `.AddFeatureFilter<T>(defaultSettings)`. That call both registers the filter with `Microsoft.FeatureManagement` and adds its metadata/default JSON to `FeatureFilterRepository`, which is what the dashboard UI uses to offer editable filters.
- Filter display names come from `FilterAliasAttribute` when present; otherwise the CLR type name is used.
- Filter JSON validation is centralized in `FeatureChangeValidation`. It validates stored filter parameters against the registered settings type with `JsonUnmappedMemberHandling.Disallow`, so filter-shape changes should be handled there instead of in the React app.
- Managed features are tracked in-memory through `ManagedFeatureRegistration` to avoid duplicate startup registration. If you change startup seeding behavior, keep that idempotency story intact.
- Tests in `Stella.FeatureManagement.Dashboard.Tests` are end-to-end tests against the sample API with `Shouldly` assertions and real PostgreSQL behavior. Prefer extending those integration tests for behavior changes that touch endpoints, persistence, or feature-evaluation flow.
- The library project packs `README.md`, `LICENSE`, symbols, and embedded `wwwroot` assets. Packaging and publishing behavior is defined in `.github\workflows\ci.yml` and `.github\workflows\publish.yml`.
