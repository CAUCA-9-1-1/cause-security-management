# Extensible Security Service Registration Through Options

* Status: accepted
* Date: 2026-06-04
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Repository architecture baseline

## Context and Problem Statement

Host applications frequently need to customize authentication behavior, user management, validation-code flows, and supporting services. The library requires a structured mechanism for replacing defaults while preserving sensible baseline registrations.

## Decision Drivers

* Provide stable defaults that work out of the box.
* Allow selective replacement of service implementations.
* Keep custom wiring discoverable and centralized.
* Avoid forcing consumers to re-register the entire dependency graph.

## Considered Options

* **Option A**: Expose customization through `InjectSecurityServices<TUser>(Action<SecurityManagementOptions>)` with helper methods for replacing specific services.
* **Option B**: Require consumers to override registrations manually after calling a minimal bootstrap method.

## Decision Outcome

Chosen option: **Option A**, because explicit options APIs provide controlled extensibility while keeping the base registration pipeline coherent.

### Consequences

* Good: Consumers can override targeted services without reproducing all base registrations.
* Good: Default implementations remain available for common scenarios.
* Bad: Static options flags must be used carefully in multi-tenant or complex hosting setups.
* Bad: Incorrect custom type pairing can fail at runtime if not validated by tests.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- Keep `SecurityManagementOptions` extension points synchronized with actual DI registration behavior.
- Maintain tests for custom service replacement and default fallback behavior.
- Document supported override combinations and known constraints for static option flags.
