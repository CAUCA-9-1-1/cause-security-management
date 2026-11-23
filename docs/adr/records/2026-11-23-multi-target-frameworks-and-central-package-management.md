# Multi-Target net10.0 And net11.0 With Central Package Management

* Status: accepted
* Date: 2026-11-23
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Issue #121 "Support DotNet 11"

## Context and Problem Statement

Every project in `Cause.SecurityManagement.sln` targeted a single framework, `net10.0`, and each `.csproj` carried its own package versions. Supporting .NET 11 raises two questions: how consumers on .NET 10 and on .NET 11 are served by one release set, and where package versions live now that some of them must differ per framework.

This ADR does not contradict an existing record. It relates to [Multi-Package Release Governance For SecurityManagement Libraries](2026-06-04-multi-package-release-governance.md), whose coordinated versioning still applies, and to [Modular Packaging Boundaries Across Core Http And Wolverine](2026-06-04-modular-packaging-boundaries.md), whose package boundaries are unchanged.

## Decision Drivers

* Support .NET 10 and .NET 11 consumers from one release set.
* One place for package versions.
* Per-framework versions for packages that track the .NET version.
* Preserve the existing floating versions.

## Considered Options

* **Option A**: Multi-target `net10.0;net11.0` in every solution project and adopt Central Package Management (`Directory.Packages.props`) with per-framework conditions.
* **Option B**: Move to `net11.0` only and drop .NET 10 consumers.
* **Option C**: Keep single-targeting and per-project package versions, with one branch or package set per .NET major.

## Decision Outcome

Chosen option: **Option A**, because it supports both runtimes from one release set and keeps all versions in one file.

* All eight projects use `<TargetFrameworks>net10.0;net11.0</TargetFrameworks>`, test projects included, so tests run on both frameworks.
* `Directory.Packages.props` sets `ManagePackageVersionsCentrally` and `CentralPackageFloatingVersionsEnabled`. `PackageReference` items carry no `Version`.
* `Microsoft.AspNetCore.Authentication.JwtBearer`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.TestHost` and `Microsoft.EntityFrameworkCore.InMemory` have one version per framework, selected with a `$(TargetFramework)` condition.
* Floating versions are preserved: a float to the minor stays a float to the minor, a float to the major (`WolverineFx.Http`) stays a float to the major, exact pins stay exact. `Cause.Core.DataLayerExtensions` is `11.0.*` for both frameworks.
* The published packages move to `11.0.0`. The MAJOR component now means the highest .NET major targeted; see `docs/RELEASING.md`.
* `NU1507` (several package sources configured while using Central Package Management) is suppressed in `Directory.Packages.props`. The repository has no `NuGet.config`, so package source mapping is not available here.

### Consequences

* Good: .NET 10 consumers can take `11.x` as-is, and .NET 11 consumers get the .NET 11 dependency builds.
* Good: Versions are edited in one file; framework-tracking packages are visible side by side.
* Bad: Build and test time roughly doubles, since every project builds and tests twice.
* Bad: A package that fails on one framework needs a per-framework version entry, which adds conditions to maintain.
* Bad: `Cause.Core.DataLayerExtensions` `11.0.*` must be available on the package feed before the release can restore.

## Implementation Plan

- [x] Task 1: Switch the eight `.csproj` files to `<TargetFrameworks>net10.0;net11.0</TargetFrameworks>` and remove `Version` from every `PackageReference`.
- [x] Task 2: Add `Directory.Packages.props` with the central and per-framework versions.
- [x] Task 3: Bump the four published projects to `11.0.0` and update `<PackageReleaseNotes>`.
- [x] Task 4: Update the "Version Scheme" and "Bump Rules" sections of `docs/RELEASING.md`.
- [x] Task 5: Verify build with zero warnings and all tests on both frameworks.
