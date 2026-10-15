---
id: ADR-20261015-filters-without-defaults
status: accepted
date: 2026-10-15
superseded-by:
scope: Stella.FeatureManagement.Dashboard/ServiceCollectionExtensions.cs, Stella.FeatureManagement.Dashboard/FeatureManagerDashboardBuilder.cs, Stella.FeatureManagement.Dashboard/Services/FeatureFilterRepository.cs, the UI's filter editors
summary: Register a filter without defaults via AddFeatureFilter<TFilter, TSettings>(), a default interface member
---
## Context
`AddFeatureFilter<T>(object defaultSettings)` needs an instance: its JSON pre-fills the dashboard editor and
its runtime type is the settings type parameters are validated against. Consumers whose filters list ids or
names had to register a placeholder (e.g. `{"UserIds":[0]}`), which every new filter then saved unless an
operator removed it. `IFeatureManagerDashboardBuilder` and `IFeatureFilterRepository` are public, so any
change to them must stay additive (net8.0 supports default interface members).
- Rejected: accept `null` in `AddFeatureFilter<T>(object? defaultSettings)` — with no instance there is no
  settings type, so that filter's parameters would silently skip validation.
- Rejected: an extension method on `IFeatureManagerDashboardBuilder` — it works only by casting to the
  concrete builder, hiding an unsupported implementer behind a runtime cast instead of stating the contract
  on the interface.
- Rejected: an abstract interface member, or a new member on `IFeatureFilterRepository` — breaks every
  third-party implementer of those public interfaces.
- Rejected: normalising a blank `parameters` string to null on the server — filter-shape validation stays
  unchanged; the dashboard sends null for a blank editor instead.

## Decision
Add `IFeatureManagerDashboardBuilder.AddFeatureFilter<TFilter, TSettings>()` (`TFilter : IFeatureFilterMetadata`,
`TSettings : class`) as a default interface member that throws `NotSupportedException`;
`FeatureManagerDashboardBuilder` implements it by registering the filter with `Microsoft.FeatureManagement`
and calling the concrete `FeatureFilterRepository.AddFilter<TFilter, TSettings>()`, which records
`DefaultSettingsJson = null` and `SettingsType = typeof(TSettings)`, and throws `ArgumentException` before
anything is registered when `TSettings` is abstract or an interface (System.Text.Json cannot deserialize it, so
every save would otherwise be a 500). `GET dashboardapi/filters` returns
`defaultSettings: null` for it. The first registration of a filter type wins, whichever overload made it.
Validation is unchanged: null parameters validate as `{}` against the settings type; blank strings are refused.

Binds: new builder or repository capabilities on public interfaces (default members only); consumers and the
UI must treat a null `defaultSettings` as "no default", never as an error.
