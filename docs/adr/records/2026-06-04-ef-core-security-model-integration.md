# EF Core Security Model Integration Through Base Context And Mappings

* Status: accepted
* Date: 2026-06-04
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Repository architecture baseline

## Context and Problem Statement

Consumers need consistent persistence for users, groups, permissions, tokens, and external-system authentication entities. The library must offer a repeatable integration path for EF Core contexts without requiring each application to duplicate mapping logic.

## Decision Drivers

* Standardize security entity mapping across consuming applications.
* Keep user type extensible via generic user model support.
* Ensure repositories and services can depend on predictable DbSet and mapping contracts.
* Reduce drift between security features and database model shape.

## Considered Options

* **Option A**: Provide `BaseSecurityContext<TUser>` and shared mapping extensions to register all required security entities.
* **Option B**: Publish only entity classes and let each consuming application define all EF mappings and context shape independently.

## Decision Outcome

Chosen option: **Option A**, because a shared base context and mapping model lowers integration risk and keeps security behavior consistent across implementations.

### Consequences

* Good: Faster and safer adoption of the security model in new applications.
* Good: Repository implementations can assume known entity sets and conventions.
* Bad: Mapping changes have broad impact and require careful migration strategy.
* Bad: Consumer-specific context patterns may require explicit extension hooks.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- Keep mapping coverage synchronized with the full security entity set.
- Maintain migration guidance and integration tests for context upgrades.
- Validate that generic `TUser` mapping remains compatible with repository and service expectations.
