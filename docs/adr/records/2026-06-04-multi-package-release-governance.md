# Multi-Package Release Governance For SecurityManagement Libraries

* Status: accepted
* Date: 2026-06-04
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Package release consistency

## Context and Problem Statement

The repository ships multiple NuGet packages (`Cause.SecurityManagement.Models`, `Cause.SecurityManagement.Core`, `Cause.SecurityManagement`, and `Cause.SecurityManagement.Wolverine`) with explicit architectural boundaries. Consumers often depend on more than one package, so releases require coordinated versioning and compatibility governance.

## Decision Drivers

* Preserve compatibility across related packages in the same ecosystem.
* Reduce consumer risk from partial or unsynchronized upgrades.
* Make breaking-change impact explicit before publishing.
* Keep release process predictable for maintainers and adopters.

## Considered Options

* **Option A**: Govern releases with coordinated semantic versioning and explicit compatibility checks across all produced packages.
* **Option B**: Version each package independently without repository-level release coordination.

## Decision Outcome

Chosen option: **Option A**, because these packages represent one security platform and are frequently consumed together.

### Consequences

* Good: Consumers get clearer upgrade paths and fewer runtime surprises.
* Good: Maintainers review cross-package impact before publishing.
* Bad: Release orchestration overhead increases for small changes.
* Bad: Packaging automation must enforce compatibility checks to avoid manual errors.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- Keep semver bump rules per package documented and current in `docs/RELEASING.md`.
- Keep `release.ps1` as the release gate: it verifies version coordination, then builds and tests all packable projects before any package is pushed.
- Maintain release notes that describe cross-package compatibility expectations for each published version set.
