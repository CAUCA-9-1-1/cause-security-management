# Multi-Scheme Authentication Routing For JWT And Certificate Flows

* Status: accepted
* Date: 2026-06-04
* Deciders: Cause.SecurityManagement maintainers
* Technical Story: Repository architecture baseline

## Context and Problem Statement

The library supports multiple identity sources (regular users, Keycloak users, and certificate-based console users). Consumers need a single authentication entry point that can route incoming bearer tokens to the correct scheme without forcing endpoint-level scheme selection everywhere.

## Decision Drivers

* Support multiple token issuers in the same API.
* Preserve backward compatibility for regular-user JWT flows.
* Keep endpoint authorization policies focused on roles and business intent.
* Allow certificate-based integration scenarios.

## Considered Options

* **Option A**: Register a policy authentication scheme that inspects token claims and forwards to regular-user, Keycloak, or console certificate schemes.
* **Option B**: Require each endpoint to specify one fixed authentication scheme and avoid runtime scheme selection.

## Decision Outcome

Chosen option: **Option A**, because centralized scheme forwarding enables mixed authentication sources while keeping endpoint code and policy usage simple.

### Consequences

* Good: Single composition point for authentication setup through extension methods.
* Good: Consumers can enable dual or triple authentication with explicit package APIs.
* Bad: Token claim shape and issuer values become critical; malformed token assumptions can route incorrectly.
* Bad: Debug behavior must be controlled carefully when exposing token diagnostics.

## Maintenance Invariants
<!-- Behaviors to preserve; this decision is implemented -->
- Keep `AddTokenAuthentication` and `AddTokenAuthenticationWithCertificates` behavior aligned with `GetSchemeToUse` claim/issuer checks.
- Maintain tests for regular-user, Keycloak, and console token routing, including unknown-issuer fallback behavior.
- Verify unauthorized responses include expected behavior for token expiry and challenge handling.
