# Default Authorization And Controller Conventions

* Status: accepted
* Date: 2026-06-04
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Repository architecture baseline

## Context and Problem Statement

Security defaults must be secure-by-default across host applications that consume library controllers. The package needs a predictable way to apply authorization rules without requiring every controller author to remember explicit attributes.

## Decision Drivers

* Enforce authentication by default.
* Support specific external-system entry points with dedicated policies.
* Keep opt-in exceptions explicit and discoverable.
* Minimize accidental anonymous exposure.

## Considered Options

* **Option A**: Apply default and special-case authorization via MVC filters and controller model conventions.
* **Option B**: Require all controllers to manually declare `[Authorize]` and explicit policy attributes.

## Decision Outcome

Chosen option: **Option A**, because conventions provide a stronger secure default and reduce the chance of missing protection on new controllers.

### Consequences

* Good: Centralized policy application through `UseDefaultAuthorizationWhenNotSpecifiedFilter` and `AddAuthorizeFiltersControllerConvention`.
* Good: External-system and certificate scenarios remain explicit through marker attributes.
* Bad: Convention-based behavior can be less obvious to new maintainers than local controller attributes.
* Bad: Policy names and attribute semantics must remain stable to avoid breaking consumers.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- Keep policy names and role requirements consistent between convention code and service registration.
- Maintain tests proving fallback authorization applies when no explicit authorize metadata is present.
- Maintain tests for `OpenToExternalSystemAttribute` and `OpenToExternalSystemWithCertificateAttribute` policy behavior.
