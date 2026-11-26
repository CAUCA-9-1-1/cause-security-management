# Releasing Cause.SecurityManagement Packages

## Coordinated Versioning

All four published packable projects share a single, identical `<Version>` value:

| Project | Package |
|---|---|
| `Cause.SecurityManagement.Models` | `Cause.SecurityManagement.Models` |
| `Cause.SecurityManagement.Core` | `Cause.SecurityManagement.Core` |
| `Cause.SecurityManagement` | `Cause.SecurityManagement` |
| `Cause.SecurityManagement.Wolverine.ExternalSystem` | `Cause.SecurityManagement.Wolverine.ExternalSystem` |

> `Cause.SecurityManagement.Wolverine.ExternalSystem` contains only the
> external-system authentication Wolverine HTTP endpoints (logon/refresh),
> isolated in their own assembly so a consumer can register just those without
> pulling the rest of the (unpublished) `Cause.SecurityManagement.Wolverine`
> surface. It shares the coordinated version and is in `release.ps1`'s packable list.

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

## Version Scheme — MAJOR Tracks .NET, Not Breaking Changes

**This is not semver, and the difference matters.** The MAJOR component states
the highest .NET major version the packages target. A release may also keep the
previous .NET major as an additional target, so that consumers on the older
runtime are not forced to move. The `11.x` packages target `net10.0` and
`net11.0`: `11.x` means ".NET 11 is supported", and .NET 10 consumers can take
it too, subject to any dependency floor the release notes name. MAJOR moves only when the highest target framework moves — `12.0.0`
would advertise .NET 12 support, so it must not be used to signal a breaking
change while the highest target is still `net11.0`.

The practical consequence: **a breaking change cannot take MAJOR.** It takes
MINOR, and the break is communicated through `<PackageReleaseNotes>` rather than
through the version number. Consumers cannot rely on the version alone to tell
them a release is safe to take, so release notes are the compatibility contract
and must name any behavior change explicitly.

Package versions are managed centrally in `Directory.Packages.props` at the
repository root; `PackageReference` items in the `.csproj` files carry no
`Version`. Packages that track the .NET version carry one version per target
framework there, except `Cause.Core.DataLayerExtensions`, whose single `11.0.*`
release is itself multi-targeted. See the ADR
`2026-11-23-multi-target-frameworks-and-central-package-management.md`.

## Bump Rules

Because all four published packages share one version, apply the **highest
required bump across any package** to all of them.

| Change type | Version component | Examples |
|---|---|---|
| Highest target framework moves to a new .NET major | **MAJOR** | `net10.0` → `net11.0` (adding `net11.0` next to `net10.0` took `10.x` to `11.0.0`) |
| Breaking change in any public API or behavior contract | **MINOR**, and name it in the release notes | Remove a method, rename a DTO property, change an endpoint signature, change the HTTP status a condition returns |
| Additive change (backwards compatible) | **MINOR** | New method overload, new endpoint, new optional parameter |
| Bug fix, documentation, internal refactor | **PATCH** | Fix incorrect validation, correct a return type, update XML docs |

Update all four published `<Version>` elements to the same new value before releasing.

## Cross-Package Compatibility

A given version set (`11.0.0`, `11.1.0-preview1`, etc.) is fully compatible
across all four published packages. Consuming applications should pin all
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
11.1.0 — Adds group management API (Core services, HTTP controllers). All
published packages must be upgraded together. Backwards compatible with 11.0.x
consumers that do not use the group management feature.
</PackageReleaseNotes>
```

## Requirements

**The script is portable; the release is not** — not until Linux credentials
are provisioned separately. `release.ps1` is a PowerShell 7 (`pwsh`) script
and its logic runs unmodified on Windows or Linux (`pwsh` is cross-platform,
so there is a single script rather than separate Windows/Linux versions).
But a working Windows `CaucaNuget` registration cannot simply be carried over
to Linux — see the credentials note below before attempting a release from a
Linux machine.

Three things must be true on the machine running it:

- **The .NET 11 SDK and the .NET 10 runtime (ASP.NET Core included) are
  installed.** The SDK builds both `net10.0` and `net11.0`; without it the build
  fails with `NETSDK1045`. The test step runs the `net10.0` test assemblies,
  which need the .NET 10 runtime, and the .NET 11 SDK does not ship it.

- **PowerShell 7 (`pwsh`) is installed.** Windows PowerShell 5.1 is not
  sufficient — the script relies on `Join-Path` accepting multiple child-path
  segments, which is PowerShell 6+ only. The script declares
  `#Requires -Version 7.0` so a 5.1 invocation fails fast with a clear
  version-mismatch error instead of an obscure `Join-Path` parameter error.
- **`CaucaNuget` is registered as a NuGet source on that machine.** The
  registration lives in the local NuGet config (a user-level `NuGet.Config` on
  Windows, `~/.nuget/NuGet/NuGet.Config` or equivalent on Linux), not in this
  repository — there is no repo-level `NuGet.config`.

  > **Windows and Linux credentials do not interchange.** NuGet encrypts
  > stored `<packageSourceCredentials>` passwords and `<apikeys>` entries
  > using Windows DPAPI, which is Windows-only and decryptable only by the
  > same user account on the same machine that wrote it. Copying a Windows
  > `NuGet.Config` to a Linux machine, or any other means of reusing the
  > Windows CaucaNuget registration, will not work — there is no flag that
  > makes DPAPI-encrypted credentials portable. Each platform (and each
  > machine) needs its own credential registration.
  >
  > On Linux, register the source with `--store-password-in-clear-text` —
  > the only option available there, since DPAPI encryption is unsupported
  > outside Windows:
  >
  > ```
  > dotnet nuget add source <feed-url> --name CaucaNuget --username <user> --password <password> --store-password-in-clear-text
  > ```
  >
  > Prefer supplying credentials via the `NuGetPackageSourceCredentials_CaucaNuget`
  > environment variable instead of a persisted clear-text password,
  > especially on a shared or non-personal Linux machine.
  >
  > Get the feed URL and credentials from the team; do not guess or hardcode
  > them here. **The Linux path is unproven** — treat it as such until
  > someone has actually completed a release from a Linux machine.

Invocation differs slightly by platform. `pwsh` parses `.ps1` files in-process
rather than executing them as a native binary, so `release.ps1` does not need
an execute bit to run under `pwsh` — but running it as `./release.ps1` from
*inside a bash shell* relies on the shebang/exec-bit mechanism that `.ps1`
files on this repo don't have (mode `100644`), so invoke it through `pwsh`
explicitly:

```powershell
# Windows
.\release.ps1 -WhatIf

# Linux
pwsh -File ./release.ps1 -WhatIf
```

> The push step itself uses `dotnet nuget push`, not `nuget.exe push` — the
> `nuget` CLI is a Windows-only binary and is not available on Linux.
> `dotnet nuget push` is bundled with the .NET SDK on both platforms. The one
> functional difference that matters here: `nuget.exe push` prompts
> interactively for credentials by default, while `dotnet nuget push` does
> not unless `--interactive` is passed (which `release.ps1` does). Without
> it, a credential problem on the release machine would surface as a bare
> 401 at the push step instead of a prompt.

## How to Release

1. Bump `<Version>` in all four published packable `.csproj` files to the same new value.
2. Update `<PackageReleaseNotes>` in each `.csproj` to describe the release set.
3. Update the Claude Code skill in
   `claude-plugins/cause-security-management/skills/cause-security-management/`
   when the release changes a public API, a default, a route, a role, a policy or
   a published package. Bump the version and the "checked against" date at the
   top of `SKILL.md`. The `cauca-ai-tools` marketplace serves the skill from the
   main branch of this repository, so consumers get it once it is merged.
4. Commit, push, and merge to the main branch.
5. Run a dry run to preview what will be pushed — nothing is published:

   ```powershell
   .\release.ps1 -WhatIf
   ```

   > **Clean `artifacts/nupkg` first.** The script's cleanup step honours
   > `ShouldProcess`, so under `-WhatIf` it is skipped and the directory keeps
   > whatever a previous run left behind. The push preview then lists those
   > stale packages as if they were about to be published — including
   > pre-release versions from an earlier set. A real run cleans correctly and
   > is unaffected, but the dry run overstates the push list unless you run
   > `Remove-Item .\artifacts\nupkg -Recurse -Force` beforehand. Confirm the
   > preview lists exactly four packages, all at the version you intend.

6. Run the full release when ready:

   ```powershell
   .\release.ps1
   ```

7. Tag the released commit and push the tag. Use the `v<MAJOR>.<MINOR>.<PATCH>`
   convention (e.g. `v11.0.0`), matching the coordinated `<Version>`. Tag the
   exact commit on the main branch that was published:

   ```powershell
   git tag -a v11.0.0 -m "Release 11.0.0 (final, non-experimental)"
   git push origin v11.0.0
   ```

   Pre-release sets (`11.1.0-preview1`, `-experimental*`, etc.) are not tagged;
   only finalized version sets get a `v*` tag.

`release.ps1` is the enforcement mechanism. It will:
- Abort immediately if any packable project version differs from the others.
- Build the solution in Release configuration.
- Run all tests (use `-SkipTests` only as a documented escape hatch when tests
  were already run locally in this session).
- Pack only the four published packable projects into `./artifacts/nupkg`.
- Push each `.nupkg` to the `CaucaNuget` feed.

It does **not** create the git tag — tagging is a manual step (7 above) so the
tag is only applied once the push has actually succeeded.
