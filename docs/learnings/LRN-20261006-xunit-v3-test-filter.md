---
id: LRN-20261006-xunit-v3-test-filter
date: 2026-10-06
obsolete:
summary: dotnet test --filter is ignored by the xunit v3 test project; pass -- --filter-class/--filter-method instead
---
## Lesson
To run a subset of `Stella.FeatureManagement.Dashboard.Tests`, pass xunit v3 options after `--`:
`dotnet test <tests.csproj> -- --filter-class "Stella.FeatureManagement.Dashboard.Tests.FeatureUsageTests"`
(or `--filter-method "<FullyQualifiedName>"`). Check that the reported `Total` matches the subset you expect.

## What happened
`dotnet test … --filter "FullyQualifiedName~FeatureUsageTests"` (the VSTest syntax, also shown in CLAUDE.md)
reported `Passed: 30, Total: 30` without any error: it ran the whole suite. A failure in the targeted
tests could have been missed or misattributed.
