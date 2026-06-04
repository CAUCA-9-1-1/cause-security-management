# Multi-Package Release Governance For SecurityManagement Libraries

* Status: proposed
* Date: 2026-06-04
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Package release consistency

## Context and Problem Statement

The repository ships multiple NuGet packages (`Cause.SecurityManagement.Core`, `Cause.SecurityManagement`, and `Cause.SecurityManagement.Wolverine`) with explicit architectural boundaries. Consumers often depend on more than one package, so releases require coordinated versioning and compatibility governance.

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

## Implementation Plan
<!-- Crucial section so Claude Code knows how to execute it -->
- [ ] Task 1: Define and document semver bump rules for each package based on API and behavior changes.
- [ ] Task 2: Add release verification that builds/tests all package projects before publishing.
- [ ] Task 3: Include release notes indicating cross-package compatibility expectations for each published version set.
