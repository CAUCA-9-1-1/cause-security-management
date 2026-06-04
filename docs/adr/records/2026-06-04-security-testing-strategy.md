# Security Testing Strategy Across Unit And Integration Layers

* Status: accepted
* Date: 2026-06-04
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Test architecture standardization

## Context and Problem Statement

The library includes multiple security-critical behaviors: authentication flows, authorization conventions, anti-forgery behavior, and version validation. Existing tests cover important slices (controller/unit tests and dedicated version tests), but architectural guidance is needed to keep future changes consistently validated at the right depth.

## Decision Drivers

* Prevent regressions in authentication and authorization behavior.
* Keep feedback loops fast for common changes.
* Ensure framework wiring is validated in realistic execution paths.
* Make expected test ownership clear per package and feature.

## Considered Options

* **Option A**: Adopt a layered strategy: unit tests for pure logic and policies, focused integration tests for pipeline/wiring behavior.
* **Option B**: Rely primarily on integration tests for all security behavior.

## Decision Outcome

Chosen option: **Option A**, because layered tests provide faster iteration while still validating runtime integration points where framework behavior matters.

### Consequences

* Good: Pure logic (for example semantic version comparisons and validators) remains cheap to test.
* Good: Authentication/authorization wiring is validated where endpoint behavior depends on middleware and conventions.
* Bad: Two test layers require discipline to avoid duplicate assertions.
* Bad: Integration fixtures can become costly if scope is not kept targeted.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- Keep version comparison and validator behavior covered by unit tests in `Cause.SecurityManagement.Tests`.
- Maintain focused integration tests in `Cause.SecurityManagement.Integration.Tests` for fallback policies, scheme routing, and marker-attribute conventions.
- Require that any change to authentication/authorization extensions is covered by tests in at least one layer, and both layers when middleware wiring changes.
