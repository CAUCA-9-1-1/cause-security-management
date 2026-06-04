# Exclude Cause.SecurityManagement.Wolverine From The Published Release Set

* Status: accepted
* Date: 2026-06-04
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Package release scope

## Context and Problem Statement

The repository ships a coordinated set of NuGet packages governed by
[Multi-Package Release Governance For SecurityManagement Libraries](2026-06-04-multi-package-release-governance.md),
which originally enumerated four packages, including `Cause.SecurityManagement.Wolverine`.
The Wolverine integration described in
[Modular Packaging Boundaries Across Core Http And Wolverine](2026-06-04-modular-packaging-boundaries.md)
is incomplete, unfinished, and not validated for production use. Publishing it as
part of every coordinated release exposes consumers to an unfinished package and
implies a stability guarantee it does not yet meet.

## Decision Drivers

* Avoid publishing an incomplete, untested integration to the shared feed.
* Keep the published release set limited to packages that meet the platform's
  compatibility and quality expectations.
* Preserve the Wolverine project in the solution so it keeps building and
  testing as it matures.
* Keep the path back to publishing it explicit and low-friction.

## Considered Options

* **Option A**: Keep the Wolverine project in the solution (built and tested)
  but exclude it from the packed-and-pushed release set until it is complete.
* **Option B**: Continue publishing all four packages on every release.
* **Option C**: Remove the Wolverine project from the repository entirely.

## Decision Outcome

Chosen option: **Option A**, because it stops shipping an unfinished package to
consumers while keeping the code under continuous build and test, and leaves a
clear, reversible route to publishing once the integration is complete.

### Consequences

* Good: Consumers no longer receive an incomplete Wolverine package.
* Good: The published set (`Models`, `Core`, `Cause.SecurityManagement`) remains
  mutually compatible and quality-gated.
* Good: The Wolverine project still compiles and runs its tests with the solution.
* Bad: The coordinated version set is now three packages, so documentation and
  the release script must stay in sync with that scope.
* Bad: A reader of the original governance ADR must follow the cross-reference to
  learn the current published scope.

## Implementation Plan
<!-- Crucial section so Claude Code knows how to execute it -->
- [x] Task 1: Set `<IsPackable>false</IsPackable>` on
      `Cause.SecurityManagement.Wolverine.csproj` and remove its
      `<GeneratePackageOnBuild>` so no package is produced.
- [x] Task 2: Remove the Wolverine project from `$PackableProjects` in
      `release.ps1` and note the exclusion in its header.
- [x] Task 3: Update `docs/RELEASING.md` to describe a three-package published
      set and the conditions for re-adding Wolverine.
- [x] Task 4: Add this ADR and update `docs/adr/records/overview.md`.
- [ ] Task 5 (future): When Wolverine is complete — restore `<IsPackable>true</IsPackable>`,
      re-add it to `$PackableProjects`, align its `<Version>` with the others, and
      supersede this ADR.
