# Releasing Cause.SecurityManagement Packages

## Coordinated Versioning

All three published packable projects share a single, identical `<Version>` value:

| Project | Package |
|---|---|
| `Cause.SecurityManagement.Models` | `Cause.SecurityManagement.Models` |
| `Cause.SecurityManagement.Core` | `Cause.SecurityManagement.Core` |
| `Cause.SecurityManagement` | `Cause.SecurityManagement` |

These packages form one security platform. Consumers depend on more than one of
them simultaneously, so a given version number always means a mutually compatible
set. The `release.ps1` script enforces this: it reads the `<Version>` element
from every packable `.csproj` before the build starts and aborts immediately if
any version differs.

> **`Cause.SecurityManagement.Wolverine` is not published.** It is incomplete,
> unfinished, and excluded from the release set. The project sets
> `<IsPackable>false</IsPackable>` and is omitted from `release.ps1`'s packable
> list, so it is built and tested with the solution but never packed or pushed.
> When it is ready, re-add it to `$PackableProjects`, restore `<IsPackable>true</IsPackable>`,
> and bring its `<Version>` in line with the others. See the ADR
> `2026-06-04-exclude-wolverine-from-published-release-set.md`.

## Semver Bump Rules

Because all three published packages share one version, apply the **highest
required bump across any package** to all of them.

| Change type | Version component | Examples |
|---|---|---|
| Breaking change in any public API or behavior contract | **MAJOR** | Remove a method, rename a DTO property, change an endpoint signature |
| Additive change (backwards compatible) | **MINOR** | New method overload, new endpoint, new optional parameter |
| Bug fix, documentation, internal refactor | **PATCH** | Fix incorrect validation, correct a return type, update XML docs |

Update all three published `<Version>` elements to the same new value before releasing.

## Cross-Package Compatibility

A given version set (`10.2.0`, `10.3.0-preview1`, etc.) is fully compatible
across all three published packages. Consuming applications should pin all
`Cause.SecurityManagement.*` packages to the same version. Mixing versions from
different release sets is unsupported and may cause runtime or compile-time
failures.

## Release Notes

Each package carries a `<PackageReleaseNotes>` element in its `.csproj`. When
releasing, describe the changes from the perspective of the whole platform, not
just the individual project. Mention any cross-package behavior changes and
compatibility expectations.

Example:

```xml
<PackageReleaseNotes>
10.3.0 — Adds group management API (Core services, HTTP controllers). All
published packages must be upgraded together. Backwards compatible with 10.2.x
consumers that do not use the group management feature.
</PackageReleaseNotes>
```

## How to Release

1. Bump `<Version>` in all three published packable `.csproj` files to the same new value.
2. Update `<PackageReleaseNotes>` in each `.csproj` to describe the release set.
3. Commit, push, and merge to the main branch.
4. Run a dry run to preview what will be pushed — nothing is published:

   ```powershell
   .\release.ps1 -WhatIf
   ```

5. Run the full release when ready:

   ```powershell
   .\release.ps1
   ```

6. Tag the released commit and push the tag. Use the `v<MAJOR>.<MINOR>.<PATCH>`
   convention (e.g. `v10.2.0`), matching the coordinated `<Version>`. Tag the
   exact commit on the main branch that was published:

   ```powershell
   git tag -a v10.2.0 -m "Release 10.2.0 (final, non-experimental)"
   git push origin v10.2.0
   ```

   Pre-release sets (`10.3.0-preview1`, `-experimental*`, etc.) are not tagged;
   only finalized version sets get a `v*` tag.

`release.ps1` is the enforcement mechanism. It will:
- Abort immediately if any packable project version differs from the others.
- Build the solution in Release configuration.
- Run all tests (use `-SkipTests` only as a documented escape hatch when tests
  were already run locally in this session).
- Pack only the three published packable projects into `./artifacts/nupkg`.
- Push each `.nupkg` to the `CaucaNuget` feed.

It does **not** create the git tag — tagging is a manual step (6 above) so the
tag is only applied once the push has actually succeeded.
