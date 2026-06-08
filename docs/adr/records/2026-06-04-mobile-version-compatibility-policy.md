# Mobile Version Compatibility Policy Using Semantic Version Gates

* Status: accepted
* Date: 2026-06-04
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Client compatibility governance

## Context and Problem Statement

Authentication endpoints expose mobile version validation behavior and enforce compatibility thresholds based on configuration values (`MinimalVersion`, `LatestVersion`). The system needs a clear architectural policy so client compatibility behavior remains predictable across releases.

## Decision Drivers

* Enforce minimum supported client versions.
* Allow forward-compatible clients to continue authenticating.
* Keep compatibility logic deterministic and configuration-driven.
* Reuse a standard semantic-version comparison model.

## Considered Options

* **Option A**: Parse versions with semantic versioning and compare against configurable minimum/latest gates.
* **Option B**: Implement custom string or numeric comparison logic tied to client release formats.

## Decision Outcome

Chosen option: **Option A**, because semantic version parsing/comparison is explicit, testable, and less error-prone than bespoke comparators.

### Consequences

* Good: Compatibility decisions are centralized in version-management services.
* Good: Operators can adjust support windows via configuration without code changes.
* Bad: Invalid version payloads can fail parsing and require robust caller handling.
* Bad: Strict semver expectations require client teams to send compliant version strings.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- Keep `MobileVersionValidator` and semantic extension behavior aligned with SemVer precedence rules.
- Expand tests for malformed input and edge cases (pre-release/build metadata) where needed.
- Document operational guidance for updating `MinimalVersion` and `LatestVersion` during rollouts.
