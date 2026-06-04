# Modular Packaging Boundaries Across Core Http And Wolverine

* Status: accepted
* Date: 2026-06-04
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Repository architecture baseline

## Context and Problem Statement

The solution provides shared security domain logic, ASP.NET integration, and optional Wolverine integration. Consumers should be able to adopt only the layers they need without pulling in unnecessary runtime dependencies.

## Decision Drivers

* Keep core domain and data concerns reusable across hosting models.
* Isolate ASP.NET-specific concerns from domain services.
* Provide optional message-bus integration for event-driven consumers.
* Keep package adoption flexible and incremental.

## Considered Options

* **Option A**: Split into dedicated packages (`Cause.SecurityManagement.Core`, `Cause.SecurityManagement`, `Cause.SecurityManagement.Wolverine`) with clear extension-point boundaries.
* **Option B**: Ship a monolithic package containing all concerns.

## Decision Outcome

Chosen option: **Option A**, because package boundaries reduce coupling and let applications depend only on the required integration surface.

### Consequences

* Good: Consumers can use core security services without ASP.NET or Wolverine dependencies.
* Good: Wolverine handlers can evolve behind optional package boundaries.
* Bad: Cross-package contract changes require careful version coordination.
* Bad: Documentation and onboarding must clearly explain package responsibilities.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- Maintain explicit dependency directions from integration packages toward core abstractions.
- Validate package-level API compatibility before release.
- Keep usage documentation aligned with package responsibilities and extension method entry points.
